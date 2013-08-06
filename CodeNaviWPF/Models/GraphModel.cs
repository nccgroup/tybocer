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
            List<SearchResult> search_results = new List<SearchResult>();
            foreach (Item item_to_search in items)
            {
                if (item_to_search is FileItem)
                {
                    FileItem item_to_search_copy = item_to_search as FileItem;
                    FileInfo item_fileinfo = new FileInfo(item_to_search.FullPath);
                    if (!extensions_to_skip.Contains(item_fileinfo.Extension))
                    {
                        if (item_fileinfo.Extension == ".docx")
                        {
                        }
                        int count = 0;
                        FileStream filestream = new FileStream(item_to_search.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        StreamReader streamreader = new StreamReader(filestream);
                        //foreach (string line in File.ReadAllLines(item_to_search.FullPath, Encoding.UTF8))
                        string line = streamreader.ReadLine();
                        while (line != null)
                        {
                            count++;
                            if (line.ToLower().Contains(selected_text.ToLower()))
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
                                search_results.Add(new SearchResult
                                {
                                    RelPath = Path.GetDirectoryName(FilePathUtils.GetRelativePath(root.FilePath, item_to_search_copy.FullPath)),
                                    FullPath = item_to_search_copy.FullPath,
                                    FileName = item_to_search_copy.FileName,
                                    Extension = item_to_search_copy.Extension,
                                    LineNumber = count,
                                    Line = line_copy
                                });
                            }
                            line = streamreader.ReadLine();
                        }
                    }
                }
                if (item_to_search is DirectoryItem)
                {
                    if (!directories_to_skip.Contains(item_to_search.FileName))
                    {
                        ExpandDirectory((DirectoryItem)item_to_search);
                        search_results.AddRange(SearchItems(((DirectoryItem)item_to_search).Items, selected_text));
                    }
                }
            }
            return search_results;
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
