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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using CodeNaviWPF.Models;
using ICSharpCode.AvalonEdit.Editing;
using GraphX;
using GraphX.Xceed.Wpf.Toolkit.Zoombox;
using CodeNaviWPF.Utils;

namespace CodeNaviWPF
{
    public partial class MainWindow : Window
    {
        private GraphProvider graph_provider;
        private VertexControl root_control;
        private PocVertex root_vertex;
        private VertexControl centre_on_me;
        private bool recentre = true;
        private int directory_count = 0;

        public MainWindow()
        {
            graph_provider = new GraphProvider();

            //this.DataContext = gp.Graph;
            InitializeComponent();
            Zoombox.SetViewFinderVisibility(zoom_control, System.Windows.Visibility.Visible);
            graph_area.AsyncAlgorithmCompute = true;
            graph_area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.ISOM;
            graph_area.RelayoutFinished += OnRelayoutFinished;
            graph_area.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
            graph_area.UseNativeObjectArrange = true;
            graph_area.Graph = graph_provider.Graph;
            graph_area.GenerateGraph(graph_provider.Graph);
            graph_area.RelayoutGraph(true);

            //tg_Area.UseNativeObjectArrange = false;
            root_control = graph_area.VertexList.Values.First();
            root_vertex = graph_area.VertexList.Keys.First();
            centre_on_me = root_control;
            zoom_control.CenterContent();
        }

        #region Events
        private void OnRelayoutFinished(object sender, EventArgs e)
        {
            graph_area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.EfficientSugiyama;
            if (recentre)
            {
                CenterOnVertex(centre_on_me);
            }
            else
            {
                recentre = true;
            }
        }

        async private void DirPicker_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                graph_provider.UpdateRoot(dialog.SelectedPath);
                directory_count = await CountDirs(dialog.SelectedPath);
            }
        }

        private Task<int> CountDirs(string path)
        {
            return Task.Run(() => { return Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Count(); });
        }

        private void OnTreeNodeDoubleClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                FileItem file_item = item.Header as FileItem;
                if (file_item != null)
                {
                    AddFileView(file_item, root_control, root_vertex);
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
            graph_area.UpdateLayout();
            recentre = true;
            centre_on_me = graph_area.VertexList.Where(x => x.Key == in_edge.Source).First().Value;
        }
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            VertexControl sv = TreeHelpers.FindVisualParent<VertexControl>(sender as DataGridRow);
            SearchResult result = (SearchResult)((System.Windows.Controls.DataGridRow)sender).Item;
            FileItem fi = new FileItem { FileName = result.FileName, FullPath = result.FullPath, Extension = result.Extension, RelPath = result.RelPath };
            AddFileView(fi, sv, (PocVertex)sv.Vertex, result.LineNumber);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Properties.Settings.Default.Save();
        }

        private void TestEditor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualParent<ICSharpCode.AvalonEdit.TextEditor>((DependencyObject)sender);

                // Ctrl+Click Go to definition
                var position = editor.GetPositionFromPoint(e.GetPosition(editor));
                System.Diagnostics.Debug.Print(position.ToString());
                e.Handled = true;
            }
        }

        #endregion

        private void AddFileView(FileItem file_item, VertexControl source, PocVertex source_vertex, int line = 0)
        {
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

            PocEdge new_edge = new PocEdge("sdfsdfdsf", source_vertex, new_vertex);
            graph_area.InsertEdge(new_edge, new EdgeControl(source, new_vertex_control, new_edge));
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
            centre_on_me = new_vertex_control;
            ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(new_vertex_control);
            if (editor != null)
            {
                editor.ScrollToLine(line);
                editor.TextArea.TextView.MouseDown += TestEditor_MouseDown;
                //((ICSharpCode.AvalonEdit.TextEditor)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(vc, 0), 0), 0), 0), 0), 0), 1), 0), 0)).ScrollToLine(line);
            }
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

        private void ExpanderRelayout(object sender, RoutedEventArgs e)
        {
            Expander expander = e.Source as Expander;
            recentre = expander.IsExpanded;
            VertexControl parent_vertex_control = TreeHelpers.FindVisualParent<VertexControl>(e.Source as Expander);
            RelayoutGraph(parent_vertex_control);
        }

        private void RelayoutGraph(VertexControl vertex_control)
        {
            centre_on_me = vertex_control;
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
        }

        async private void SearchString(object sender, RoutedEventArgs e)
        {
            string selected_text = "";
            TextArea textarea = e.OriginalSource as TextArea;
            PocVertex source_vertex = (PocVertex)((VertexControl)e.Source).Vertex;
            if (textarea == null)
            {
                VertexControl source_vertex_control = e.Source as VertexControl;
                if (source_vertex_control == null) return;
                selected_text = ((FileBrowser)source_vertex_control.DataContext).SearchTerm;
            }
            else
            {
                selected_text = textarea.Selection.GetText();
            }
            if (selected_text != null && selected_text != "")
            {
                SearchResultsVertex new_search_results_vertex = graph_provider.PerformSearch(selected_text, source_vertex);
                VertexControl to_vertex_control = new VertexControl(new_search_results_vertex) { DataContext = new_search_results_vertex };
                VertexControl from_vertex_control = (VertexControl)e.Source;
                graph_area.AddVertex(new_search_results_vertex, to_vertex_control);
                PocEdge new_edge = new PocEdge("Search for: "+selected_text, source_vertex, new_search_results_vertex);
                graph_area.InsertEdge(new_edge, new EdgeControl(from_vertex_control, to_vertex_control, new_edge));
                graph_area.RelayoutGraph(true);
                graph_area.UpdateLayout();
                centre_on_me = to_vertex_control;
                System.Windows.Controls.ProgressBar bar = TreeHelpers.FindVisualChild<System.Windows.Controls.ProgressBar>(to_vertex_control);
                System.Windows.Controls.DataGrid grid = TreeHelpers.FindVisualChild<System.Windows.Controls.DataGrid>(to_vertex_control);
                bar.Maximum = directory_count;
                var search_progress = new Progress<int>((int some_int) => ReportProgress(bar));
                await graph_provider.PopulateResultsAsync(selected_text, new_search_results_vertex, search_progress);
                bar.Visibility = System.Windows.Visibility.Collapsed;
                grid.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ReportProgress(System.Windows.Controls.ProgressBar bar)
        {
            bar.Value++;
        }

        private void CloseVertex(PocVertex vertex_to_remove)
        {
            if (graph_provider.Graph.OutEdges(vertex_to_remove).Count() > 0)
            {
                var edges = graph_area.Graph.OutEdges(vertex_to_remove).ToList();
                foreach (PocEdge edge in edges)
                {
                    CloseVertex(edge.Target);
                    RemoveEdge(edge);
                }
            }
            graph_provider.Graph.RemoveVertex(vertex_to_remove);
            graph_area.RemoveVertex(vertex_to_remove);
        }

        private void RemoveEdge(PocEdge edge)
        {
            graph_provider.Graph.RemoveEdge(edge);
            //tg_Area.Graph.RemoveEdge(e); // TODO - get this working.
            foreach (PocEdge f in graph_area.EdgesList.Keys.ToList())
            {
                if (edge.Target == f.Target && edge.Source == f.Source)
                {
                    graph_area.RemoveEdge(f);
                }
            }
        }

        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            graph_area.RelayoutGraph(true);
            graph_area.UpdateLayout();
        }
    }

    public static class Commands
    {
        public static readonly RoutedUICommand SearchString = new RoutedUICommand("Search String", "SearchString", typeof(MainWindow));
        public static readonly RoutedUICommand ExpanderRelayout = new RoutedUICommand("Relayout Graph", "ExpanderRelayout", typeof(MainWindow));
        public static readonly RoutedUICommand OnCloseVertex = new RoutedUICommand("Close Vertex", "OnCloseVertex", typeof(MainWindow));
    }
}
