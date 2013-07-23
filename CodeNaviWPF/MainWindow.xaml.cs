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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using CodeNaviWPF.Models;
using ICSharpCode;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using GraphX;
using GraphX.Controls;
using GraphX.Models;
using Xceed.Wpf.Toolkit.Zoombox;

namespace CodeNaviWPF
{
    public partial class MainWindow : Window
    {
        private GraphProvider gp;

        public MainWindow()
        {
            gp = new GraphProvider();

            this.DataContext = gp.Graph;
            InitializeComponent();
            tg_Area.AsyncAlgorithmCompute = true;
            tg_Area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.KK;
            tg_Area.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
            tg_Area.Graph = gp.Graph;
            tg_Area.GenerateGraph(gp.Graph);
            tg_Area.DefaultLayoutAlgorithmParams = tg_Area.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.EfficientSugiyama);
            tg_Area.RelayoutGraph(true);

            Zoombox.SetViewFinderVisibility(tg_zoomctrl, System.Windows.Visibility.Visible);
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
                    FileVertex v = gp.AddFileView(fi);
                    tg_Area.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.EfficientSugiyama;
                    tg_Area.AddVertex(v, new VertexControl(v) { DataContext = v });
                    tg_Area.RelayoutGraph(true);
                    tg_zoomctrl.FitToBounds();
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
                    gp.ExpandDirectory(di);
                }
            }
        }

        private void SearchString(object sender, RoutedEventArgs e)
        {
            TextArea textarea = e.OriginalSource as TextArea;
            PocVertex v = (PocVertex)((VertexControl)e.Source).Vertex;
            if (textarea != null)
            {
                string selected_text = textarea.Selection.GetText();
                if (selected_text != null && selected_text != "")
                {
                    SearchResultsVertex s = gp.PerformSearch(selected_text, v);
                    VertexControl to_vertex_control = new VertexControl(s) { DataContext = s };
                    VertexControl from_vertex_control = (VertexControl)e.Source;
                    tg_Area.AddVertex(s, to_vertex_control);
                    PocEdge new_edge = new PocEdge("sdfsdfdsf", v, s);
                    tg_Area.InsertEdge(new_edge, new EdgeControl(from_vertex_control, to_vertex_control, new_edge));
                    tg_Area.RelayoutGraph(true);
                    tg_zoomctrl.FitToBounds();

                }
            }
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
            FileVertex f = gp.AddFileView(new FileItem { FileName = result.FileName, FullPath = result.FullPath, Extension = result.Extension, RelPath = result.RelPath }, (PocVertex)sv.Vertex);
            VertexControl to_vertex_control = new VertexControl(f) { DataContext = f };
            tg_Area.AddVertex(f, to_vertex_control);
            PocEdge new_edge = new PocEdge("sdfsdfdsf", (PocVertex)sv.Vertex, f);
            tg_Area.InsertEdge(new_edge, new EdgeControl(sv, to_vertex_control, new_edge));
            tg_Area.RelayoutGraph(true);
            tg_zoomctrl.FitToBounds();
        }
    }

    public static class Commands
    {
        public static readonly RoutedUICommand SearchString = new RoutedUICommand("Search String", "SearchString", typeof(MainWindow));
    }
}
