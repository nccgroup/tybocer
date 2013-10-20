/*
 * Released as open source by NCC Group Plc - http://www.nccgroup.com/
 * 
 * Developed by Felix Ingram, (felix.ingram@nccgroup.com)
 * 
 * http://www.github.com/nccgroup/tybocer
 * 
 * Released under AGPL. See LICENSE for more information
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using CodeNaviWPF.Models;
using CodeNaviWPF.ViewModels;
using ICSharpCode.AvalonEdit.Editing;
using GraphX;
using GraphX.Xceed.Wpf.Toolkit.Zoombox;
using CodeNaviWPF.Utils;
using System.Threading;
using GraphX.GraphSharp.Algorithms.Layout.Simple.Tree;

namespace CodeNaviWPF
{
    public partial class MainWindow : Window
    {
        private GraphProvider graph_provider;
        private VertexControl root_control;
        private VertexControl centre_on_me;
        private bool recentre = true;
        private int directory_count = 0;
        private object directory_count_lock = new object();
        private bool still_counting;
        private List<string> ctags_tags_files = new List<string>();
        private object ctags_info_lock = new object();
        private bool loading = false;
        private bool use_ctags = true;
        private CancellationTokenSource dir_count_token_source = new CancellationTokenSource();
        private CancellationToken dir_count_cancellation_token;
        private Task<int> dir_count_task;
        private CancellationTokenSource ctags_token_source = new CancellationTokenSource();
        private CancellationToken ctags_cancellation_token;
        private Task ctags_task;

        public MainWindow()
        {
            InitializeComponent();
            Zoombox.SetViewFinderVisibility(zoom_control, System.Windows.Visibility.Visible);

            CreateNewGraph();
            zoom_control.CenterContent();
            zoom_control.DragModifiers.Clear();
            zoom_control.DragModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Ctrl);
            zoom_control.DragModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Shift);
            zoom_control.DragModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Exact);
            zoom_control.ZoomModifiers.Clear();
            zoom_control.ZoomModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Ctrl);
            zoom_control.ZoomModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Alt);
            zoom_control.ZoomModifiers.Add(GraphX.Xceed.Wpf.Toolkit.Core.Input.KeyModifier.Exact);

            PreferencesExpander.Collapsed += delegate
            {
                PrefTopRow.Height = new GridLength(1, GridUnitType.Auto);
            };

            PreferencesExpander.Expanded += delegate
            {
                PrefTopRow.Height = new GridLength(1, GridUnitType.Star);
            };

            dir_count_cancellation_token = dir_count_token_source.Token;
            ctags_cancellation_token = ctags_token_source.Token;

            if (File.Exists(Properties.Settings.Default.PreviousFile)) load_project(Properties.Settings.Default.PreviousFile);

        }

        private void CreateNewGraph()
        {
            graph_area.Children.Clear();
            graph_provider = new GraphProvider();
            graph_area.AsyncAlgorithmCompute = true;
            graph_area.RelayoutFinished += OnRelayoutFinished;
            graph_area.UseNativeObjectArrange = true;
            graph_area.Graph = graph_provider.Graph;
            SetGraphLayoutParameters();
            graph_area.GenerateGraph(graph_provider.Graph);
            graph_area.RelayoutGraph(true);

            root_control = graph_area.VertexList.Values.First();
            graph_provider.root_vertex = (FileBrowser)graph_area.VertexList.Keys.First();
            centre_on_me = root_control;
        }

        #region Events
        private void OnRelayoutFinished(object sender, EventArgs e)
        {
            SetGraphLayoutParameters();
            if (recentre)
            {
                CenterOnVertex(centre_on_me);
            }
            else
            {
                recentre = true;
            }
        }

        private void SetGraphLayoutParameters()
        {
            graph_area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.Tree;
            graph_area.DefaultLayoutAlgorithmParams = graph_area.AlgorithmFactory.CreateLayoutParameters(GraphX.LayoutAlgorithmTypeEnum.Tree);
            ((SimpleTreeLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).VertexGap = 100; // int.Parse(vertdist.Text);
            ((SimpleTreeLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).LayerGap = 100; // int.Parse(vertdist.Text);
            ((SimpleTreeLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).WidthPerHeight = 1000;

            //graph_area.DefaultLayoutAlgorithmParams = graph_area.AlgorithmFactory.CreateLayoutParameters(GraphX.LayoutAlgorithmTypeEnum.EfficientSugiyama);
            //((EfficientSugiyamaLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).LayerDistance = int.Parse(layerdist.Text);
            //((EfficientSugiyamaLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).MinimizeEdgeLength = (bool)mini.IsChecked;
            //((EfficientSugiyamaLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).PositionMode = 3;
            //((EfficientSugiyamaLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).VertexDistance = int.Parse(vertdist.Text);
            //((EfficientSugiyamaLayoutParameters)graph_area.DefaultLayoutAlgorithmParams).WidthPerHeight = 1000;
        }

        async private void DirPicker_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                graph_provider.root_dir = dialog.SelectedPath;
                graph_provider.UpdateRoot(dialog.SelectedPath);
                still_counting = true;
                directory_count = await CountDirs(dialog.SelectedPath);
                still_counting = false;
                graph_provider.root_vertex.CtagsRun = false;
                await UpdateCtags();
                UpdateCtagsHighlights();
                graph_provider.root_vertex.CtagsRun = true;
                graph_provider.SaveGraph();
            }
        }

        async private void CheckForCtags(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ctagsLocation.Text) && graph_provider != null && graph_provider.root_dir != "")
            {
                await UpdateCtags();
                UpdateCtagsHighlights();
            }
        }

        private void UpdateCtagsHighlights()
        {
            foreach (TextArea ta in Utils.TreeHelpers.FindVisualChildren<TextArea>(this))
            {
                List<ICSharpCode.AvalonEdit.Rendering.IVisualLineTransformer> old_list = (from transformer in ta.TextView.LineTransformers
                                                                                         where transformer.GetType() != typeof(UnderlineCtagsMatches)
                                                                                         select transformer).ToList();
                ta.TextView.LineTransformers.Clear();
                foreach (var a in old_list)
                {
                    ta.TextView.LineTransformers.Add(a);
                }
                //ta.TextView.LineTransformers
                if (use_ctags)
                {
                    ta.TextView.LineTransformers.Add(new UnderlineCtagsMatches(graph_provider.root_vertex.CtagsMatches.Keys.ToList()));
                }
            }
        }

        private Task UpdateCtags()
        {
            if (!use_ctags) return Task.Run(() => { });
            if (ctags_task != null && ctags_task.Status.Equals(TaskStatus.Running))
            {
                ctags_token_source.Cancel();
            }
            ctags_task = Task.Run(() =>
            {
                try
                {
                    if (graph_provider.root_dir != "")
                    {
                        lock (ctags_info_lock)
                        {
                            ctags_tags_files = RunCtags(graph_provider.root_dir);
                            //ctags_matches = new Dictionary<string, List<List<string>>>();
                            foreach (string file in ctags_tags_files)
                            {
                                foreach (string line in File.ReadLines(file)) // ctags_info.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (ctags_cancellation_token.IsCancellationRequested) break;
                                    if (line.StartsWith("!")) continue; // Skip comments lines

                                    List<string> fields = line.Split(new string[] { "\t" }, StringSplitOptions.None).ToList();
                                    try
                                    {
                                        graph_provider.root_vertex.CtagsMatches[fields[0]].Add(fields);
                                    }
                                    catch (KeyNotFoundException)
                                    {
                                        graph_provider.root_vertex.CtagsMatches.Add(fields[0], new List<List<string>>());
                                        graph_provider.root_vertex.CtagsMatches[fields[0]].Add(fields);
                                    }
                                }
                                File.Delete(file);
                            }
                        }
                    }
                }
                catch (OutOfMemoryException)
                {
                    System.Windows.Forms.MessageBox.Show("Ran out of memory while processing ctags. Tags will not be available.",
                        "Ctags fail",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Exclamation);
                }
            }, ctags_cancellation_token);
            return ctags_task;
        }

        private List<string> RunCtags(string path)
        {
            if (!File.Exists(Properties.Settings.Default.CtagsLocation)) return new List<string>();
            //List<string> temp_files = new List<string>();
            //Parallel.ForEach(Utils.PathEnumerators.EnumerateAccessibleDirectories(path, true), dir_info =>
            //    {
                    string results_file = Path.GetTempFileName();
                    string args = string.Format(@"-f""{0}"" --fields=afmikKlnsStz --recurse=yes ""{1}""", results_file, path);
              //      string args = string.Format(@"-f""{0}"" --fields=afmikKlnsStz ""{1}""", results_file, dir_info.FullName);
                    //string args = string.Format(@"-f""c:\temp\test.tmp{0}"" --fields=afmikKlnsStz *", Utils.IDCounter.Counter);


                    Process process = new Process
                    {
                        StartInfo =
                        {
                            FileName = Properties.Settings.Default.CtagsLocation,
                            Arguments = args,
                            WorkingDirectory = path,
                            RedirectStandardOutput = false,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    //string output = "";
                    try
                    {
                        //output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                    }
                    catch (OutOfMemoryException)
                    {
                        System.Windows.Forms.MessageBox.Show("Ctags ran out of memory.", "Ctags fail", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    }
             //       temp_files.Add(results_file);
              //  });
            //return output
            //temp_files.RemoveAll(string.IsNullOrEmpty);
            return new List<String>() { results_file };
        }

        private Task<int> CountDirs(string path)
        {
            if (dir_count_task != null && dir_count_task.Status.Equals(TaskStatus.Running))
            {
                dir_count_token_source.Cancel();
            }

            var count_progress = new Progress<int>(CountDirProgress);

            dir_count_cancellation_token = dir_count_token_source.Token;

            lock (directory_count_lock)
            {
                dir_count_task = Task.Run(() =>
                {
                    return Utils.PathEnumerators.EnumerateAccessibleDirectories(path, count_progress, dir_count_cancellation_token, true).Count();
                }, dir_count_cancellation_token);
                return dir_count_task;
            }
        }

        private void OnTreeNodeDoubleClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                FileItem file_item = item.Header as FileItem;
                if (file_item != null)
                {
                    AddFileView(file_item, root_control, graph_provider.root_vertex);
                    graph_provider.SaveGraph();
                }
            }
        }

        private void OnTreeItemExpand(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                DirectoryItem di = item.Header as DirectoryItem;
                if (di != null)
                {
                    graph_provider.ExpandDirectory(di);
                }
            }
        }

        private void OnCloseVertex(object sender, RoutedEventArgs e)
        {
            VertexControl vertex_control_to_close = e.Source as VertexControl;
            PocVertex v = vertex_control_to_close.DataContext as PocVertex;
            PocEdge in_edge = graph_area.Graph.InEdge(v, 0); // Will only ever have one in edge
            if (graph_area.Graph.OutEdges(v).Count() > 0)
            {
                System.Windows.Forms.DialogResult d = System.Windows.Forms.MessageBox.Show("Vertex has child nodes. All nodes on this branch will be deleted. Continue?", "Delete Child Nodes?", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                if (d == System.Windows.Forms.DialogResult.No) return;
            }
            CloseVertex(v);
            RemoveEdge(in_edge);
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
            graph_provider.SaveGraph();
            recentre = true;
            centre_on_me = graph_area.VertexList.Where(x => x.Key == in_edge.Source).First().Value;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SearchResult result = (SearchResult)((System.Windows.Controls.DataGridRow)sender).Item;
            result.Checked = true;
            FileItem fi = new FileItem { FileName = result.FileName, FullPath = result.FullPath, Extension = result.Extension, RelPath = result.RelPath };

            VertexControl sv = TreeHelpers.FindVisualParent<VertexControl>(sender as DataGridRow);
            AddFileView(fi, sv, (PocVertex)sv.Vertex, result.LineNumber);
            graph_provider.SaveGraph();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Properties.Settings.Default.Save();
        }

        private void TestEditor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualParent<ICSharpCode.AvalonEdit.TextEditor>((DependencyObject)sender);
                var file_path = ((FileVertex)TreeHelpers.FindVisualParent<VertexControl>((DependencyObject)editor).Vertex).FilePath;
                
                // Ctrl+Click Go to definition
                var position = editor.GetPositionFromPoint(e.GetPosition(editor));
                if (position != null)
                {
                    var clicked_line_no = position.Value.Line;
                    var offset = editor.Document.GetOffset(position.Value.Location);
                    var start = ICSharpCode.AvalonEdit.Document.TextUtilities.GetNextCaretPosition(editor.Document, offset, System.Windows.Documents.LogicalDirection.Backward, ICSharpCode.AvalonEdit.Document.CaretPositioningMode.WordBorder);
                    if (start < 0) return;
                    var end = ICSharpCode.AvalonEdit.Document.TextUtilities.GetNextCaretPosition(editor.Document, offset, System.Windows.Documents.LogicalDirection.Forward, ICSharpCode.AvalonEdit.Document.CaretPositioningMode.WordBorder);
                    var word = editor.Document.GetText(start, end - start);
                    //System.Diagnostics.Debug.Print(word);

                    if (graph_provider.root_vertex.CtagsRun && graph_provider.root_vertex.CtagsMatches.ContainsKey(word))
                    {
                        Dictionary<string, List<int>> files_and_lines = new Dictionary<string, List<int>>();
                        //foreach (string line in ctags_info.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        //{
                        //System.Diagnostics.Debug.Print(line);
                        foreach (List<string> match in graph_provider.root_vertex.CtagsMatches[word])
                        {
                            int line_no = 1;
                            foreach (string field in match)
                            {
                                if (field.StartsWith("line:"))
                                {
                                    line_no = int.Parse(field.Split(new char[] { ':' })[1]);
                                }
                            }
                            var file_path_to_add = match[1];
                            if (file_path_to_add != file_path)
                            {
                                if (files_and_lines.ContainsKey(file_path_to_add))
                                {
                                    files_and_lines[match[1]].Add(line_no);
                                }
                                else
                                {
                                    files_and_lines[file_path_to_add] = new List<int>();
                                    files_and_lines[file_path_to_add].Add(line_no);
                                }
                            }
                        }
                        if (files_and_lines.Count() > 0)
                        {
                            VertexControl editor_vertex = TreeHelpers.FindVisualParent<VertexControl>(editor);
                            VertexControl ctags_vertex = AddCtagsAnchor(word, editor_vertex, (PocVertex)editor_vertex.Vertex);
                            foreach (string file in files_and_lines.Keys)
                            {
                                FileItem fi = new FileItem
                                {
                                    FileName = Path.GetFileName(file),
                                    FullPath = Path.Combine(graph_provider.root_dir, file),
                                    Extension = Path.GetExtension(file),
                                    RelPath = file,
                                };
                                if (!(Path.GetFullPath(((FileVertex)editor_vertex.Vertex).FilePath) == Path.GetFullPath(fi.FullPath) && !files_and_lines[file].Contains(position.Value.Line)))
                                {
                                    AddFileView(fi, ctags_vertex, (CtagsVertex)ctags_vertex.Vertex, files_and_lines[file]);
                                }
                            }
                            graph_provider.SaveGraph();
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
        }

        async private void TestEditor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.S && !((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)) // Not doing ctrl-s to save
            {
                string selected_text = "";
                TextArea textarea = e.OriginalSource as TextArea;
                VertexControl sv = TreeHelpers.FindVisualParent<VertexControl>(textarea);
                PocVertex source_vertex = (PocVertex)sv.Vertex;
                selected_text = textarea.Selection.GetText();
                await SearchForString(selected_text, sv);
            }
        }

        #endregion

        private VertexControl AddCtagsAnchor(String tag, VertexControl source, PocVertex source_vertex)
        {
            CtagsVertex new_vertex = graph_provider.AddCtagsAnchor(tag, source_vertex);
            VertexControl new_vertex_control = new VertexControl(new_vertex) { DataContext = new_vertex };
            graph_area.AddVertex(new_vertex, new_vertex_control);
            PocEdge new_edge = new PocEdge(source_vertex, new_vertex);
            graph_area.InsertEdge(new_edge, new EdgeControl(source, new_vertex_control, new_edge));
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
            centre_on_me = new_vertex_control;

            return new_vertex_control;
        }

        private void AddFileView(FileItem file_item, VertexControl source, PocVertex source_vertex, int line)
        {
            AddFileView(file_item, source, source_vertex, new List<int> { line });
        }

        private void AddFileView(FileItem file_item, VertexControl source, PocVertex source_vertex, List<int> lines = null)
        {
            if (!graph_provider.root_vertex.CtagsRun)
            {
                System.Windows.Forms.MessageBox.Show("Ctags is still running, so tags are not available.", "Ctags running", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            FileVertex new_vertex = graph_provider.AddFileView(file_item, source_vertex);
            VertexControl new_vertex_control = new VertexControl(new_vertex) { DataContext = new_vertex };
            try
            {
                graph_area.AddVertex(new_vertex, new_vertex_control);
            }
            catch (GraphX.GX_InvalidDataException)
            {
                new_vertex_control = graph_area.GetAllVertexControls().Where(c => c.Vertex == new_vertex).First();
            }

            PocEdge new_edge = new PocEdge(source_vertex, new_vertex);
            graph_area.InsertEdge(new_edge, new EdgeControl(source, new_vertex_control, new_edge));
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
            centre_on_me = new_vertex_control;
            ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(new_vertex_control);
            if (editor != null)
            {
                editor.TextArea.TextView.MouseDown += TestEditor_MouseDown;
                editor.TextArea.SelectionChanged += TestEditor_SelectionChanged;
                if (graph_provider.root_vertex.CtagsRun) editor.TextArea.TextView.LineTransformers.Add(new UnderlineCtagsMatches(graph_provider.root_vertex.CtagsMatches.Keys.ToList()));
                if (lines != null)
                {
                    editor.TextArea.TextView.BackgroundRenderers.Add(new HighlightSearchLineBackgroundRenderer(editor, lines));
                    editor.ScrollToLine(lines.Min());
                }
                editor.Width = editor.ActualWidth;
            }
        }

        private void TestEditor_SelectionChanged(object sender, EventArgs e)
        {
            TextArea ta = sender as TextArea;
            List<ICSharpCode.AvalonEdit.Rendering.IVisualLineTransformer> old_list = (from transformer in ta.TextView.LineTransformers
                                                                                      where transformer.GetType() != typeof(HighlightSelection)
                                                                                      select transformer).ToList();
            ta.TextView.LineTransformers.Clear();
            foreach (var a in old_list)
            {
                ta.TextView.LineTransformers.Add(a);
            }
            ta.TextView.LineTransformers.Add(new HighlightSelection(ta.Selection.GetText()));
        }

        private void ExpanderRelayout(object sender, RoutedEventArgs e)
        {
            Expander expander = e.Source as Expander;
            recentre = expander.IsExpanded;
            VertexControl parent_vertex_control = TreeHelpers.FindVisualParent<VertexControl>(e.Source as Expander);
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                foreach (PocEdge edge in graph_area.Graph.OutEdges((PocVertex)parent_vertex_control.Vertex))
                {
                    VertexControl vc = graph_area.GetAllVertexControls().Where(x => x.Vertex == edge.Target).FirstOrDefault();
                    var new_expander = TreeHelpers.FindVisualChild<Expander>(vc);
                    if (new_expander != null) new_expander.IsExpanded = expander.IsExpanded;
                }
            }
            RelayoutGraph(parent_vertex_control);
        }

        private void CenterOnVertex(VertexControl vc)
        {
            var x = zoom_control.Position.X + vc.GetPosition().X;
            var y = zoom_control.Position.Y + vc.GetPosition().Y;
            var of = System.Windows.Media.VisualTreeHelper.GetOffset(vc);
            var new_point = new Point(
                (of.X
                * zoom_control.Scale
                + vc.ActualWidth / 2
                * zoom_control.Scale
                - zoom_control.ActualWidth / 2
                )
                ,
                (of.Y
                * zoom_control.Scale
                + vc.ActualHeight / 2
                * zoom_control.Scale
                - zoom_control.ActualHeight / 2
                )
                );
            zoom_control.ZoomTo(new_point);
        }

        private void RelayoutGraph(VertexControl vertex_control)
        {
            centre_on_me = vertex_control;
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
        }

        #region Searching
        async private void SearchString(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO - this needs sorting out. At the moment we're hacking up a solution
            // where we test for whether they've selected some text in the box, or
            // entered something in the root node.
            string selected_text = "";
            List<string> extensions = new List<string>();
            TextArea textarea = e.OriginalSource as TextArea;
            PocVertex source_vertex = (PocVertex)((VertexControl)e.Source).Vertex;
            if (textarea == null)
            {
                VertexControl source_vertex_control = e.Source as VertexControl;
                if (source_vertex_control == null) return;
                selected_text = ((FileBrowser)source_vertex_control.DataContext).SearchTerm;
                //System.Windows.Controls.TextBox textbox = Utils.TreeHelpers.FindVisualChildren<System.Windows.Controls.TextBox>(source_vertex_control).Last();
                //System.Windows.Controls.TextBox textbox = Utils.TreeHelpers.FindVisualChildByName<System.Windows.Controls.TextBox>(typeof(MainWindow), extensionList);
                extensions = extensionList.Text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                extensions.ForEach(extension => extension.ToLower());
            }
            else
            {
                selected_text = textarea.Selection.GetText();
                string parameter = e.Parameter as string;
                if (parameter == "same_type") // Should use an enum or similar
                {
                    extensions.Add(Path.GetExtension(((FileVertex)source_vertex).FilePath));
                }
                else if (parameter == "restricted")
                {
                    System.Windows.Controls.TextBox textbox = Utils.TreeHelpers.FindVisualChildren<System.Windows.Controls.TextBox>(root_control).LastOrDefault();
                    if (textbox != null)
                    {
                        extensions = textbox.Text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        extensions.ForEach(extension => extension.ToLower());
                    }
                }
            }

            await SearchForString(selected_text, (VertexControl)e.Source, extensions);
            graph_provider.SaveGraph();
        }

        private Task SearchForString(string selected_text, VertexControl from_vertex_control, List<string> extensions_to_search = null)
        {
            if (selected_text != null && selected_text != "")
            {
                PocVertex from_vertex = (PocVertex)from_vertex_control.Vertex;

                SearchResultsVertex new_search_results_vertex = graph_provider.PerformSearch(selected_text, from_vertex, extensions_to_search);
                VertexControl new_search_results_vertex_control = new VertexControl(new_search_results_vertex) { DataContext = new_search_results_vertex };
                graph_area.AddVertex(new_search_results_vertex, new_search_results_vertex_control);

                PocEdge new_edge = new PocEdge((PocVertex)from_vertex_control.Vertex, new_search_results_vertex);
                graph_area.InsertEdge(new_edge, new EdgeControl(from_vertex_control, new_search_results_vertex_control, new_edge));
                
                graph_area.RelayoutGraph(true);
                graph_area.UpdateLayout();
                
                centre_on_me = new_search_results_vertex_control;

                System.Windows.Controls.DataGrid grid = TreeHelpers.FindVisualChild<System.Windows.Controls.DataGrid>(new_search_results_vertex_control);
                System.Windows.Controls.ProgressBar bar = TreeHelpers.FindVisualChild<System.Windows.Controls.ProgressBar>(new_search_results_vertex_control);
                if (still_counting)
                {
                    bar.Maximum = 100;
                }
                else
                {
                    bar.Maximum = directory_count;
                }
                var search_progress = new Progress<int>((int some_int) => ReportProgress(bar));

                return graph_provider.PopulateResultsAsync(selected_text, new_search_results_vertex, search_progress);
                
                //bar.Visibility = System.Windows.Visibility.Collapsed;
                //grid.Visibility = System.Windows.Visibility.Visible;
            }
            return new Task(() => { });
        }

        private void CountDirProgress(int no_to_count)
        {
            directory_count += no_to_count;
        }

        private void ReportProgress(System.Windows.Controls.ProgressBar bar)
        {
            bar.Value++;
        }
        #endregion

        private void CloseVertex(PocVertex vertex_to_remove)
        {
            if (graph_area.Graph.OutEdges(vertex_to_remove).Count() > 0)
            {
                var edges = graph_area.Graph.OutEdges(vertex_to_remove).ToList();
                foreach (PocEdge edge in edges)
                {
                    CloseVertex(edge.Target);
                    RemoveEdge(edge);
                }
            }
            graph_area.Graph.RemoveVertex(vertex_to_remove);
            graph_area.RemoveVertex(vertex_to_remove);
        }

        private void RemoveEdge(PocEdge edge)
        {
            graph_area.Graph.RemoveEdge(edge);
            //tg_Area.Graph.RemoveEdge(e); // TODO - get this working.
            foreach (PocEdge f in graph_area.EdgesList.Keys.ToList())
            {
                if (edge.Target == f.Target && edge.Source == f.Source)
                {
                    graph_area.RemoveEdge(f);
                }
            }
        }

        private void SearchResultCheckedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!loading) graph_provider.SaveGraph();
        }

        private void root_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCtagsHighlights();
            foreach (ICSharpCode.AvalonEdit.TextEditor editor in Utils.TreeHelpers.FindVisualChildren<ICSharpCode.AvalonEdit.TextEditor>(this))
            {
                editor.TextArea.TextView.MouseDown += TestEditor_MouseDown;
                editor.Width = editor.ActualWidth;
            }
            NotesEditor.DataContext = graph_provider.Graph;
        }

        async private void enableCtags_Checked(object sender, RoutedEventArgs e)
        {
            if (((System.Windows.Controls.CheckBox)e.Source).IsChecked == null) use_ctags = false;
            if (((System.Windows.Controls.CheckBox)e.Source).IsChecked == false) use_ctags = false;
            if (((System.Windows.Controls.CheckBox)e.Source).IsChecked == true) use_ctags = true;
            if (use_ctags) await UpdateCtags();
            UpdateCtagsHighlights();
        }

        private void OpenProject(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "Vizzy files|*.vizzy";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                load_project(dialog.FileName);
            }
        }

        private void load_project(String filename)
        {
            graph_provider.LoadProject(filename);
            graph_area.Graph = graph_provider.Graph;
            SetGraphLayoutParameters();
            graph_area.GenerateGraph(graph_provider.Graph);
            graph_area.RelayoutGraph(true);
            root_control = graph_area.VertexList.Where(x => x.Key.GetType() == typeof(FileBrowser)).First().Value;
            root_control.Vertex = graph_provider.root_vertex;
            RelayoutGraph(root_control);
        }

        private void SaveProject(object sender, ExecutedRoutedEventArgs e)
        {
            save_project();
        }

        private void SaveProjectAs(object sender, ExecutedRoutedEventArgs e)
        {
            save_project(true);
        }

        private void save_project(bool saveas=false)
        {
            if (graph_provider.UsingTempFile || saveas)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.AddExtension = true;
                dialog.Filter = "Vizzy files|*.vizzy";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var file_name = dialog.FileName;
                    if (!file_name.EndsWith(".vizzy")) file_name += ".vizzy";
                    graph_provider.SaveGraph(file_name);
                }
            }
            else
            {
                graph_provider.SaveGraph();
            }
}

        private void NewProject(object sender, ExecutedRoutedEventArgs e)
        {
            graph_provider.SaveGraph();
            CreateNewGraph();
        }

        private void DataGrid_HandBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void ExpandExpander(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var vc = Utils.TreeHelpers.FindVisualParent<VertexControl>((DependencyObject)e.OriginalSource);
                var expander = Utils.TreeHelpers.FindVisualChild<Expander>(vc);
                expander.IsExpanded = true;
            }

        }

        private void NotesEditor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                NotesExpander.IsExpanded = false;
            }
        }
    }

    public static class Commands
    {
        public static readonly RoutedUICommand SearchString = new RoutedUICommand("Search String", "SearchString", typeof(MainWindow));
        public static readonly RoutedUICommand ExpanderRelayout = new RoutedUICommand("Relayout Graph", "ExpanderRelayout", typeof(MainWindow));
        public static readonly RoutedUICommand OnCloseVertex = new RoutedUICommand("Close Vertex", "OnCloseVertex", typeof(MainWindow));
        public static readonly RoutedUICommand SaveAs = new RoutedUICommand("Save Project As", "SaveProjectAs", typeof(MainWindow)) { InputGestures = { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) } };
    }
}
