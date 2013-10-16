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

        public PocGraph Graph
        {
            get { return graph; }
            set
            {
                graph = value;
            }
        }

        public GraphProvider()
        {
            item_provider = new ItemProvider();
            Graph = new PocGraph(true);
            root = new FileBrowser("");
            graph.AddVertex(root);
            //layoutAlgorithmType = "EfficientSugiyama";
            //LayoutAlgorithmType="Circular"
            //LayoutAlgorithmType="CompundFDP"
            //LayoutAlgorithmType="EfficientSugiyama"
            //LayoutAlgorithmType="FR"
            //LayoutAlgorithmType="ISOM"
            //LayoutAlgorithmType="KK"
            //LayoutAlgorithmType="LinLog"
            //LayoutAlgorithmType="Tree"
        }
        
        internal void UpdateRoot(string p)
        {
            root.FilePath = p;
            item_provider.RootDir = p;
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
            if (!extensions_to_skip.Contains(file_info.Extension.ToLower()))
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
                            Line = line.Length > 500 ? line.TrimStart().Substring(0, 500) : line.TrimStart()
                        };
                    }
                    line = streamreader.ReadLine();
                }
            }
            return null;
        }

        internal BlockingCollection<SearchResult> SearchDirectory(string search_term, List<string> extensions_to_search, DirectoryInfo directory, IProgress<int> progress)
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
                Parallel.ForEach(Utils.PathEnumerators.GetFiles(directory), file_info =>
                {
                    if (extensions_to_search == null || extensions_to_search.Count == 0 || extensions_to_search.Contains(file_info.Extension.ToLower()))
                    {
                        SearchResult result = SearchFile(file_info, search_term, extensions_to_skip);
                        if (result != null) results.Add(result);
                    }
                });

                //Parallel.ForEach(directory.EnumerateDirectories(), dir_info =>
                Parallel.ForEach(Utils.PathEnumerators.EnumerateAccessibleDirectories(directory), dir_info =>
                {
                    progress.Report(1);
                    foreach (var s in SearchDirectory(search_term, extensions_to_search, dir_info, progress))
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
            results_vertex.SearchRunning = true;

            BlockingCollection<SearchResult> results = new BlockingCollection<SearchResult>();

            results = SearchDirectory(search_term, results_vertex.ExtensionsToSearch, new DirectoryInfo(root.FilePath), progress);

            results_vertex.Results = results.ToList<SearchResult>();
            results_vertex.SearchRunning = false;
        }

        internal Task PopulateResultsAsync(string search_string, SearchResultsVertex search_result, IProgress<int> progress)
        {
            search_result.SearchRunning = true;
            return Task.Factory.StartNew(() => PopulateResults(search_string, search_result, progress));
        }

        internal SearchResultsVertex PerformSearch(string search_string, PocVertex source_vertex, List<string> extensions_to_search)
        {
            SearchResultsVertex search_result = new SearchResultsVertex(search_string);
            search_result.Results = new List<SearchResult>();
            search_result.ExtensionsToSearch = extensions_to_search;
            
            Graph.AddVertex(search_result);
            Graph.AddEdge(new PocEdge(source_vertex, search_result));
            return search_result;
        }
    }
}
