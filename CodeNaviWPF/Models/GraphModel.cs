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
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using GraphSharp.Controls;

namespace CodeNaviWPF.Models
{
    public class PocGraphLayout : GraphLayout<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        private ItemProvider ip;
        private PocGraph graph;
        private PocVertex root;
        private string _rootdir;
        private string layoutAlgorithmType;
        
        public string LayoutAlgorithmType
        {
            get { return layoutAlgorithmType; }
            set
            {
                layoutAlgorithmType = value;
                NotifyPropertyChanged("LayoutAlgorithmType");
            }
        }

        public PocGraph Graph
        {
            get { return graph; }
            set
            {
                graph = value;
                NotifyPropertyChanged("Graph");
            }
        }

        public string RootDir
        {
            get { return _rootdir; }
            set { _rootdir = value; }
        }

        public GraphProvider()
        {
            ip = new ItemProvider();
            Graph = new PocGraph(true);
            root = new PocVertex("ROOT", "");
            graph.AddVertex(root);
            layoutAlgorithmType = "EfficientSugiyama";
            //LayoutAlgorithmType="Circular"
            //LayoutAlgorithmType="CompundFDP"
            //LayoutAlgorithmType="EfficientSugiyama"
            //LayoutAlgorithmType="FR"
            //LayoutAlgorithmType="ISOM"
            //LayoutAlgorithmType="KK"
            //LayoutAlgorithmType="LinLog"
            //LayoutAlgorithmType="Tree"
            NotifyPropertyChanged("Graph");
        }

        internal void UpdateRoot(string p)
        {
            root.FilePath = p;
            NotifyPropertyChanged("Graph");
        }

        internal void AddFileView(FileItem f)
        {
            FileVertex v = new FileVertex(f.Name, f.Path);
            StreamReader sr = new StreamReader(f.Path);
            v.Document.Text = sr.ReadToEnd();
            Graph.AddVertex(v);
            Graph.AddEdge(new PocEdge("Open...", root, v));
            NotifyPropertyChanged("Graph");
        }

        internal void ExpandDirectory(DirectoryItem di)
        {
            List<Item> items = ip.GetItems(di.Path);
            di.Items.Clear();
            foreach (Item i in items)
            {
                di.Items.Add(i);
            }
        }

        #region Property Changed Stuff
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion
    }

}
