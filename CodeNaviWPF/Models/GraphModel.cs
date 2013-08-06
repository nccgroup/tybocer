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
using System.ComponentModel;
using System.Text;
using System.IO;
//using GraphSharp.Controls;
using GraphX;
using CodeNaviWPF.Utils;

namespace CodeNaviWPF.Models
{
    public class PocGraphLayout : GraphArea<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        private ItemProvider ip;
        private PocGraph graph;
        private FileBrowser root;
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
            root = new FileBrowser("");
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

        internal FileVertex AddFileView(FileItem f)
        {
            return AddFileView(f, root);
        }

        internal FileVertex AddFileView(FileItem f, PocVertex from_vertex)
        {

            FileVertex new_vertex = new FileVertex(f.FileName, f.FullPath, root.FilePath);
            Graph.AddVertex(new_vertex);
            Graph.AddEdge(new PocEdge("Open...", from_vertex, new_vertex));
            //}
            return new_vertex;
        }

        internal void ExpandDirectory(DirectoryItem di)
        {
            List<Item> items = ip.GetItems(di.FullPath);
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

        //private List<string> extensions_to_skip = new List<string> { 
        //    ".exe",
        //    ".pdb", 
        //    ".dll", 
        //    ".zip",
        //    ".cache", 
        //    ".suo",
        //    ".resources",
        //    ".baml",
        //};

        internal List<SearchResult> SearchItems(List<Item> items, string selected_text)
        {
            List<String> extensions_to_skip = new List<String>(Properties.Settings.Default.ExcludedExtensions.Split(';'));
            List<String> directories_to_skip = new List<String>(Properties.Settings.Default.ExcludedDirectories.Split(';'));
            List<SearchResult> s = new List<SearchResult>();
            foreach (Item i in items)
            {
                if (i is FileItem)
                {
                    FileItem new_i = i as FileItem;
                    FileInfo f = new FileInfo(i.FullPath);
                    if (!extensions_to_skip.Contains(f.Extension))
                    {
                        int count = 0;
                        foreach (string line in File.ReadAllLines(i.FullPath, Encoding.UTF8))
                        {
                            count++;
                            if (line.Contains(selected_text))
                            {
                                string line_copy;
                                if (line.Length > 500)
                                {
                                    // For when we search a binary file by mistake.
                                    // Or some perl code
                                    line_copy = line.Substring(0, 500);
                                }
                                else
                                {
                                    line_copy = line;
                                }
                                s.Add(new SearchResult
                                {
                                    RelPath = Path.GetDirectoryName(FilePathUtils.GetRelativePath(root.FilePath, new_i.FullPath)),
                                    FullPath = new_i.FullPath,
                                    FileName = new_i.FileName,
                                    Extension = new_i.Extension,
                                    LineNumber = count,
                                    Line = line_copy
                                });
                            }
                        }
                    }
                }
                if (i is DirectoryItem)
                {
                    if (!directories_to_skip.Contains(i.FileName))
                    {
                        ExpandDirectory((DirectoryItem)i);
                        s.AddRange(SearchItems(((DirectoryItem)i).Items, selected_text));
                    }
                }
            }
            return s;
        }

        internal SearchResultsVertex PerformSearch(string selected_text, PocVertex source_vertex)
        {
            SearchResultsVertex search_results = new SearchResultsVertex(selected_text);
            search_results.Results = SearchItems(ip.GetItems(root.FilePath), selected_text);
            Graph.AddVertex(search_results);
            Graph.AddEdge(new PocEdge("Search", source_vertex, search_results));
            NotifyPropertyChanged("Graph");
            return search_results;
        }
    }

}
