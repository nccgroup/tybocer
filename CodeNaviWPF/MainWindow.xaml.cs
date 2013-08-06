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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private GraphProvider gp;
        private VertexControl root_control;
        private PocVertex root_vertex;
        private VertexControl centre_on_me;
        private bool recentre = true;

        public MainWindow()
        {
            gp = new GraphProvider();

            this.DataContext = gp.Graph;
            InitializeComponent();
            Zoombox.SetViewFinderVisibility(tg_zoomctrl, System.Windows.Visibility.Visible);
            tg_Area.AsyncAlgorithmCompute = true;
            tg_Area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.ISOM;
            tg_Area.RelayoutFinished += OnRelayoutFinished;
            tg_Area.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
            tg_Area.UseNativeObjectArrange = true;
            tg_Area.Graph = gp.Graph;
            tg_Area.GenerateGraph(gp.Graph);
            tg_Area.RelayoutGraph(true);

            //tg_Area.UseNativeObjectArrange = false;
            root_control = tg_Area.VertexList.Values.First();
            root_vertex = tg_Area.VertexList.Keys.First();
            centre_on_me = root_control;
            tg_zoomctrl.CenterContent();
            //CenterOnVertex(root_control);
        }

        private void OnRelayoutFinished(object sender, EventArgs e)
        {
            tg_Area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.EfficientSugiyama;
            //CenterOnVertex(((PocGraphLayout)sender).GetAllVertexControls().ToList().Last());
            if (recentre)
            {
                CenterOnVertex(centre_on_me);
            }
            else
            {
                recentre = true;
            }
        }

        private void DirPicker_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                gp.UpdateRoot(dialog.SelectedPath);
            }
        }

        private void OnTreeNodeDoubleClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                FileItem fi = item.Header as FileItem;
                if (fi != null)
                {
                    AddFileView(fi, root_control, root_vertex);
                }
            }
        }
            VertexControl sv = TreeHelpers.FindVisualParent<VertexControl>(sender as DataGridRow);
                ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualParent<ICSharpCode.AvalonEdit.TextEditor>((DependencyObject)sender);

        private void AddFileView(FileItem fi, VertexControl source, PocVertex source_vertex, int line = 0)
        {
            FileVertex v = gp.AddFileView(fi, source_vertex);
            VertexControl vc = new VertexControl(v) { DataContext = v };
            try
            {
                tg_Area.AddVertex(v, vc);
            }
            catch (GraphX.GX_InvalidDataException)
            {
                vc = tg_Area.GetAllVertexControls().Where(c => c.Vertex == v).First();
            }

            PocEdge new_edge = new PocEdge("sdfsdfdsf", source_vertex, v);
            tg_Area.InsertEdge(new_edge, new EdgeControl(source, vc, new_edge));
            tg_Area.RelayoutGraph(true);
            tg_Area.UpdateLayout();
            centre_on_me = vc;
            ICSharpCode.AvalonEdit.TextEditor editor = FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(vc);
            ICSharpCode.AvalonEdit.TextEditor editor = TreeHelpers.FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(new_vertex_control);
            if (editor != null)
            {
                editor.ScrollToLine(line);
                editor.TextArea.TextView.MouseDown += TestEditor_MouseDown;
                //((ICSharpCode.AvalonEdit.TextEditor)VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(vc, 0), 0), 0), 0), 0), 0), 1), 0), 0)).ScrollToLine(line);
            }
            //CenterOnVertex(vc);
        }

        private void CenterOnVertex(VertexControl vc)
        {
            var x = tg_zoomctrl.Position.X + vc.GetPosition().X;
            var y = tg_zoomctrl.Position.Y + vc.GetPosition().Y;
            var of = System.Windows.Media.VisualTreeHelper.GetOffset(vc);
            var new_point = new Point(
                (of.X
                * tg_zoomctrl.Scale
                + vc.ActualWidth / 2
                * tg_zoomctrl.Scale
                - tg_zoomctrl.ActualWidth / 2
                )
                ,
                (of.Y
                * tg_zoomctrl.Scale
                + vc.ActualHeight / 2
                * tg_zoomctrl.Scale
                - tg_zoomctrl.ActualHeight / 2
                )
                );
            tg_zoomctrl.ZoomTo(new_point);
        }

        private void OnTreeItemExpand(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                DirectoryItem di = item.Header as DirectoryItem;
                if (di != null)
                {
                    gp.ExpandDirectory(di);
                }
            }
        }

        private void RelayoutGraph(object sender, RoutedEventArgs e)
        {
            Expander ex = e.Source as Expander;
            recentre = ex.IsExpanded;
            VertexControl vc = FindVisualParent<VertexControl>(e.Source as Expander);
            centre_on_me = vc;
            tg_Area.RelayoutGraph(true);
            tg_Area.UpdateLayout();
            VertexControl parent_vertex_control = TreeHelpers.FindVisualParent<VertexControl>(e.Source as Expander);
            //if (ex.IsExpanded) CenterOnVertex(vc);
        }

        private void SearchString(object sender, RoutedEventArgs e)
        {
            string selected_text = "";
            TextArea textarea = e.OriginalSource as TextArea;
            PocVertex v = (PocVertex)((VertexControl)e.Source).Vertex;
            if (textarea == null)
            {
                VertexControl vc = e.Source as VertexControl;
                if (vc == null) return;
                selected_text = ((PocVertex)vc.DataContext).SearchTerm;
            }
            else
            {
                selected_text = textarea.Selection.GetText();
            }
            if (selected_text != null && selected_text != "")
            {
                SearchResultsVertex s = gp.PerformSearch(selected_text, v);
                VertexControl to_vertex_control = new VertexControl(s) { DataContext = s };
                VertexControl from_vertex_control = (VertexControl)e.Source;
                tg_Area.AddVertex(s, to_vertex_control);
                PocEdge new_edge = new PocEdge("sdfsdfdsf", v, s);
                tg_Area.InsertEdge(new_edge, new EdgeControl(from_vertex_control, to_vertex_control, new_edge));
                tg_Area.RelayoutGraph(true);
                tg_Area.UpdateLayout();
                centre_on_me = to_vertex_control;
                //CenterOnVertex(to_vertex_control);
            }
        }

        private void OnCloseVertex(object sender, RoutedEventArgs e)
        {
            VertexControl vc = e.Source as VertexControl;
            PocVertex v = vc.DataContext as PocVertex;
            PocEdge edge = gp.Graph.InEdge(v, 0); // Will only ever have one in edge
            if (gp.Graph.OutEdges(v).Count() > 0)
            {
                System.Windows.Forms.DialogResult d = System.Windows.Forms.MessageBox.Show("Vertex has child nodes. All nodes on this branch will be deleted. Continue?", "Delete Child Nodes?", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                if (d == System.Windows.Forms.DialogResult.No) return;
            }
            CloseVertex(v);
            gp.Graph.RemoveEdge(edge);
            foreach (PocEdge f in tg_Area.EdgesList.Keys.ToList())
            {
                if (edge.Target == f.Target && edge.Source == f.Source)
                {
                    tg_Area.RemoveEdge(f);
                }
            }
            tg_Area.UpdateLayout();
            recentre = true;
            centre_on_me = tg_Area.VertexList.Where(x => x.Key == edge.Source).First().Value;
        }

        private void CloseVertex(PocVertex v)
        {
            if (gp.Graph.OutEdges(v).Count() > 0)
            {
                var edges = gp.Graph.OutEdges(v).ToList();
                //var edges = tg_Area.EdgesList.Keys;
                foreach (PocEdge e in edges)
                {
                    CloseVertex(e.Target);
                    gp.Graph.RemoveEdge(e);
                    foreach (PocEdge f in tg_Area.EdgesList.Keys.ToList())
                    {
                        if (e.Target == f.Target && e.Source == f.Source)
                        {
                            tg_Area.RemoveEdge(f);
                        }

                    }
                }
            }
            gp.Graph.RemoveVertex(v);
            tg_Area.RemoveVertex(v);
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
        where childItem : DependencyObject
        {
            try
            {
                ((dynamic)obj).ApplyTemplate();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        static T FindVisualParent<T>(DependencyObject element) where T : UIElement
        {
            DependencyObject parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as DependencyObject;
            }
            return null;
        }

        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            VertexControl sv = FindVisualParent<VertexControl>(sender as DataGridRow);
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
                ICSharpCode.AvalonEdit.TextEditor editor = FindVisualParent<ICSharpCode.AvalonEdit.TextEditor>((DependencyObject)sender);

                // Ctrl+Click Go to definition
                var position = editor.GetPositionFromPoint(e.GetPosition(editor));
                System.Diagnostics.Debug.Print(position.ToString());
                e.Handled = true;
            }
        }

    }

    public static class Commands
    {
        public static readonly RoutedUICommand SearchString = new RoutedUICommand("Search String", "SearchString", typeof(MainWindow));
        public static readonly RoutedUICommand RelayoutGraph = new RoutedUICommand("Relayout Graph", "RelayoutGraph", typeof(MainWindow));
        public static readonly RoutedUICommand OnCloseVertex = new RoutedUICommand("Close Vertex", "OnCloseVertex", typeof(MainWindow));
    }
}
