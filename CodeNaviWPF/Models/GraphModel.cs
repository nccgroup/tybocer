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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.IO;
//using GraphSharp.Controls;
using GraphX;
using CodeNaviWPF.Utils;

namespace CodeNaviWPF.Models
{
    public class PocGraphLayout : GraphArea<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        private ItemProvider item_provider;
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
            item_provider = new ItemProvider();
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
            Graph.AddEdge(new PocEdge(from_vertex, new_vertex));
            return new_vertex;
        }

        internal void ExpandDirectory(DirectoryItem di)
        {
            List<Item> items = item_provider.GetItems(di.FullPath);
            di.Items.Clear();
            di.Items.AddRange(items);
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

        private SearchResult SearchFile(FileInfo file_info, String search_term, List<String> extensions_to_skip)
        {
            if (!extensions_to_skip.Contains(file_info.Extension))
            {
                FileStream filestream = new FileStream(file_info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader streamreader = new StreamReader(filestream);
                string line = streamreader.ReadLine();
                int count = 0;
                while (line != null)
                {
                    count++;
                    if (line.ToLower().Contains(search_term.ToLower()))
                    {
                        return new SearchResult
                        {
                            RelPath = Path.GetDirectoryName(FilePathUtils.GetRelativePath(root.FilePath, file_info.FullName)),
                            FullPath = file_info.FullName,
                            FileName = file_info.Name,
                            Extension = file_info.Extension,
                            LineNumber = count,
                            Line = line.Length > 500 ? line.Substring(0, 500) : line
                        };
                    }
                    line = streamreader.ReadLine();
                }
            }
            return null;
        }

        internal BlockingCollection<SearchResult> SearchDirectory(string search_term, DirectoryInfo directory, IProgress<int> progress)
        {
            List<String> extensions_to_skip = new List<String>(Properties.Settings.Default.ExcludedExtensions.Split(';'));
            List<String> directories_to_skip = new List<String>(Properties.Settings.Default.ExcludedDirectories.Split(';'));
            if (directories_to_skip.Contains(directory.Name))
            {
                progress.Report(1);
                return new BlockingCollection<SearchResult>();
            }
            BlockingCollection<SearchResult> results = new BlockingCollection<SearchResult>();
            try
            {
                Parallel.ForEach(directory.EnumerateFiles(), file_info =>
                {
                    SearchResult result = SearchFile(file_info, search_term, extensions_to_skip);
                    if (result != null) results.Add(result);
                });

                Parallel.ForEach(directory.EnumerateDirectories(), dir_info =>
                {
                    progress.Report(1);
                    foreach (var s in SearchDirectory(search_term, dir_info, progress))
                    {
                        if (s != null) results.Add(s);
                    }
                });
            }
            catch (DirectoryNotFoundException)
            { 
                // TODO - Hmm. I think this could short circuit things a little too early.
            }

            return results;

        }

        internal void PopulateResults(string search_term, SearchResultsVertex results_vertex, IProgress<int> progress)
        {
            BlockingCollection<SearchResult> results = new BlockingCollection<SearchResult>();

            results = SearchDirectory(search_term, new DirectoryInfo(root.FilePath), progress);

            results_vertex.Results = results.ToList<SearchResult>();

        }

        internal Task PopulateResultsAsync(string search_string, SearchResultsVertex search_result, IProgress<int> progress)
        {
            return Task.Factory.StartNew(() => PopulateResults(search_string, search_result, progress));
        }

        internal SearchResultsVertex PerformSearch(string search_string, PocVertex source_vertex)
        {
            SearchResultsVertex search_result = new SearchResultsVertex(search_string);
            search_result.Results = new List<SearchResult>();
            
            Graph.AddVertex(search_result);
            Graph.AddEdge(new PocEdge(source_vertex, search_result));
            NotifyPropertyChanged("Graph");
            return search_result;
        }
    }

}
