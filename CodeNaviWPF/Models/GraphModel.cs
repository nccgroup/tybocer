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
using System.Threading.Tasks;
using GraphSharp.Controls;

namespace CodeNaviWPF.Models
{
    public class PocGraphLayout : GraphLayout<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        static Random random = new Random();
        private ItemProvider ip;
        public List<Item> Files
        {
            get { var items = ip.GetItems("c:\\temp"); return items; }
        }
        public string Test = "sdsfsdfsfd";
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
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
            set
            {
                _rootdir = value;
            }
        }

        public GraphProvider()
        {
            ip = new ItemProvider();
            Graph = new PocGraph(true);
            root = new PocVertex("ROOT", "", "sdfsdfsdf");
            graph.AddVertex(root);
            //List<PocVertex> existingVertices = new List<PocVertex>();
            //existingVertices.Add(new PocVertex("Sacha Barber", "sdfsdf", "My Test string")); //0
            //existingVertices.Add(new PocVertex("Sarah Barber", false, "My Test string")); //1
            //existingVertices.Add(new PocVertex("Marlon Grech", true, "My Test string")); //2


            //foreach (PocVertex vertex in existingVertices)
            //    Graph.AddVertex(vertex);
            //Graph.AddVertex(new PocVertex("hgfhfghff", false, "87ffyfyff"));
            NotifyPropertyChanged("Graph");
        }

        public void ReLayoutGraph()
        {
            //graph = new PocGraph(true);
            
            //List<PocVertex> existingVertices = new List<PocVertex>();
            //existingVertices.Add(new PocVertex("Barn Rubble{0}", true, "My Test string")); //0
            ////existingVertices.Add(new PocVertex(String.Format("Frank Zappa{0}", count), false, "My Test string")); //1
            ////existingVertices.Add(new PocVertex(String.Format("Gerty CrinckleBottom{0}", count), true, "My Test string")); //2


            //foreach (PocVertex vertex in existingVertices)
            //    Graph.AddVertex(vertex);


            //add some edges to the graph
            //AddNewGraphEdge(existingVertices[0], existingVertices[1]);
            //AddNewGraphEdge(existingVertices[0], existingVertices[2]);

            PocVertex v = new PocVertex("sdfsdfsd", "", "sdfsdfsdfdsfsdf");
            Graph.AddVertex(v);
            int r = random.Next(Graph.Vertices.Count());
            Graph.AddEdge(new PocEdge("sdsdf", v, Graph.Vertices.ToList()[r]));

            NotifyPropertyChanged("Graph");

        }

        internal void UpdateRoot(string p)
        {
            root.FilePath = p;
            NotifyPropertyChanged("Graph");
        }

        internal void AddFileView(FileItem f)
        {
            PocVertex v = new PocVertex(f.Name, f.Path, f.Name);
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
    }
}
