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

        internal string GetRelativePath(string toPath)
        {
            string fromPath = root.FilePath;
            if (!fromPath.EndsWith("\\"))
            {
                fromPath += "\\";
            }
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        internal void AddFileView(FileItem f)
        {
            FileVertex new_vertex = null;
            foreach (PocVertex v in graph.Vertices)
            {
                if (v is FileVertex && v.FilePath == f.Path)
                {
                    new_vertex = (FileVertex)v;
                }
            }
            if (new_vertex == null)
            {
                new_vertex = new FileVertex(f.Name, f.Path);
                StreamReader sr = new StreamReader(f.Path);
                new_vertex.Document.Text = sr.ReadToEnd();
                Graph.AddVertex(new_vertex);
                Graph.AddEdge(new PocEdge("Open...", root, new_vertex));
            }
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

        internal List<SearchResult> SearchItems(List<Item> items, string selected_text)
        {
            List<SearchResult> s = new List<SearchResult>();
            foreach (Item i in items)
            {
                if (i is FileItem)
                {
                    FileInfo f = new FileInfo(i.Path);
                    int count = 0;
                    foreach (string line in File.ReadAllLines(i.Path, Encoding.UTF8))
                    {
                        count++;
                        if (line.Contains(selected_text))
                        {
                            s.Add(new SearchResult
                            {
                                Path = i.Path,
                                File = i.Path,
                                Ext = i.Path,
                                LineNumber = count,
                                Line = line
                            });
                        }
                    }
                }
                if (i is DirectoryItem)
                {
                    ExpandDirectory((DirectoryItem)i);
                    s.AddRange(SearchItems(((DirectoryItem)i).Items, selected_text));
                }
            }
            return s;
        }

        internal void PerformSearch(string selected_text, PocVertex source_vertex)
        {
            SearchResultsVertex s = new SearchResultsVertex("some search", selected_text);
            s.Results = SearchItems(ip.GetItems(root.FilePath), selected_text);
            Graph.AddVertex(s);
            Graph.AddEdge(new PocEdge("Search", source_vertex, s));
            NotifyPropertyChanged("Graph");
        }
    }

}
