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
using System.IO.Packaging;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
//using GraphSharp.Controls;
using GraphX;
using CodeNaviWPF.Models;
using CodeNaviWPF.Utils;

namespace CodeNaviWPF.ViewModels
{
    public class PocGraphLayout : GraphArea<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        private static string EdgeRelationship = "http://schemas.nccgroup.com/package/relationships/edge";

        private ItemProvider item_provider;
        private PocGraph graph;
        public FileBrowser root;
        public FileBrowser root_vertex;
        public string root_dir = "";
        private Dictionary<string, List<List<string>>> ctags_matches;
        private Package package;
        private string save_file = "";

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

        private List<SearchResult> SearchFile(FileInfo file_info, string search_term, List<string> extensions_to_skip)
        {
            if (!extensions_to_skip.Contains(file_info.Extension.ToLower()))
            {
                List<SearchResult> results = new List<SearchResult>();
                FileStream filestream = new FileStream(file_info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader streamreader = new StreamReader(filestream);
                string line = streamreader.ReadLine();
                int count = 0;
                while (line != null)
                {
                    count++;
                    if (line.ToLower().Contains(search_term.ToLower()))
                    {
                        results.Add(new SearchResult
                        {
                            RelPath = Path.GetDirectoryName(FilePathUtils.GetRelativePath(root.FilePath, file_info.FullName)),
                            FullPath = file_info.FullName,
                            FileName = file_info.Name,
                            Extension = file_info.Extension,
                            LineNumber = count,
                            Line = line.Length > 500 ? line.TrimStart().Substring(0, 500) : line.TrimStart()
                        });
                    }
                    line = streamreader.ReadLine();
                }
                return results;
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
                        List<SearchResult> results_to_add = SearchFile(file_info, search_term, extensions_to_skip);
                        if (results_to_add != null)
                        {
                            foreach (var r in results_to_add)
                            {
                                results.Add(r);
                            }
                        }
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

        #region Saving and loading
        public void SaveGraph(string file_name = null)
        {
            if (file_name != null && package != null)
            {
                //package.Close();
            }

            if (file_name == null && string.IsNullOrEmpty(save_file))
            {
                file_name = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".vizzy";
                //if (package != null) package.Close();
            }

            if (file_name == null && !string.IsNullOrEmpty(save_file))
            {
                file_name = save_file;
                //if (package != null) package.Close();
            }

            package = Package.Open(file_name, FileMode.Create);
            
            serialize_graph(package);
            Properties.Settings.Default.PreviousFile = file_name;
            save_file = file_name;
            package.Close();
            package = null;
            //XmlSerializer graph_serializer = new XmlSerializer(typeof(PocGraph));
            //BinaryFormatter ctags_serializer = new BinaryFormatter();

            //Uri graphUri = PackUriHelper.CreatePartUri(new Uri("graph", UriKind.Relative));

            //PackagePart graph_part = package.CreatePart(graphUri, System.Net.Mime.MediaTypeNames.Text.Xml);

            //Stream graph_stream = graph_part.GetStream(FileMode.Create);
            
            //graph_serializer.Serialize(graph_stream, graph);

            //if (ctags_matches != null)
            //{
            //    Uri ctagsUri = PackUriHelper.CreatePartUri(new Uri("ctags", UriKind.Relative));
            //    PackagePart ctags_part = package.CreatePart(ctagsUri, System.Net.Mime.MediaTypeNames.Application.Octet);
            //    Stream ctags_stream = ctags_part.GetStream(FileMode.Create);
            //    ctags_serializer.Serialize(ctags_stream, ctags_matches);
            //}

            //if (root_vertex.CtagsRun) graph_area.SaveIntoFile(file_name);
        }

        private void serialize_graph(Package package)
        {
            Uri sourceVertUri;
            Uri targetVertUri;
            PackagePart source_vert_part;
            PackagePart target_vert_part;
            Stream vert_stream;
            XmlSerializer vert_serializer = new XmlSerializer(typeof(PocVertex));
            
            Uri rootVertexUri = PackUriHelper.CreatePartUri(new Uri("vertices/vertex-"+root_vertex.ID.ToString(), UriKind.Relative));
            if (!package.PartExists(rootVertexUri))
            {
                PackagePart rootPart = package.CreatePart(rootVertexUri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                using (vert_stream = rootPart.GetStream(FileMode.Create))
                {
                    vert_serializer.Serialize(vert_stream, root_vertex);
                }
            }
            //foreach (var vert in graph.Vertices)
            //{
            //vertUri = PackUriHelper.CreatePartUri(new Uri("vertices/vertex-"+vert.ID.ToString(), UriKind.Relative));
            //vert_part = package.CreatePart(vertUri, System.Net.Mime.MediaTypeNames.Text.Xml);
            //vert_stream = vert_part.GetStream(FileMode.Create);
            //vert_serializer.Serialize(vert_stream, vert);
            //}

            // This isn't the way we should do it.
            // Ideally these should be saved as relationships.
            foreach (var edge in graph.Edges)
            {
                sourceVertUri = PackUriHelper.CreatePartUri(new Uri("vertices/vertex-" + edge.Source.ID.ToString(), UriKind.Relative));
                targetVertUri = PackUriHelper.CreatePartUri(new Uri("vertices/vertex-" + edge.Target.ID.ToString(), UriKind.Relative));

                if (!package.PartExists(sourceVertUri))
                {
                    source_vert_part = package.CreatePart(sourceVertUri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                    using (vert_stream = source_vert_part.GetStream(FileMode.Create))
                    {
                        vert_serializer.Serialize(vert_stream, edge.Source);
                    }
                }
                else
                {
                    source_vert_part = package.GetPart(sourceVertUri);
                }

                if (!package.PartExists(targetVertUri))
                {
                    target_vert_part = package.CreatePart(targetVertUri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                    using (vert_stream = target_vert_part.GetStream(FileMode.Create))
                    {
                        vert_serializer.Serialize(vert_stream, edge.Target);
                    }
                }
                else
                {
                    target_vert_part = package.GetPart(targetVertUri);
                }

                var rels = source_vert_part.GetRelationshipsByType(EdgeRelationship).Where(x => x.TargetUri == targetVertUri);
                if (rels.Count() == 0) source_vert_part.CreateRelationship(targetVertUri, TargetMode.Internal, EdgeRelationship);

                //edgeUri = PackUriHelper.CreatePartUri(new Uri("edges/edge-" + edge.ID.ToString(), UriKind.Relative));
                //edge_part = package.CreatePart(edgeUri, System.Net.Mime.MediaTypeNames.Text.Xml);
                //edge_stream = edge_part.GetStream(FileMode.Create);
                //edge_serializer.Serialize(edge_stream, edge);
            }
        }

        public void LoadProject(string filename)
        {
            package = Package.Open(filename, FileMode.OpenOrCreate);
            Properties.Settings.Default.PreviousFile = filename;

            XmlSerializer vertex_loader = new XmlSerializer(typeof(PocVertex));
            graph = new PocGraph(true);

            foreach (var p in package.GetParts())
            {
                if (p.ContentType == "text/xml"
                    && p.Uri.OriginalString.StartsWith("/vertices")
                    && !p.Uri.OriginalString.EndsWith(".rels")
                    )
                {
                    PocVertex new_vertex = (PocVertex)vertex_loader.Deserialize(p.GetStream(FileMode.Open));
                    graph.AddVertex(new_vertex);
                }
            }

            foreach (var p in package.GetParts())
            {
                if (p.ContentType == "text/xml"
                    && p.Uri.OriginalString.StartsWith("/vertices")
                    && !p.Uri.OriginalString.EndsWith(".rels")
                    )
                {
                    PocVertex new_vertex = (PocVertex)vertex_loader.Deserialize(p.GetStream(FileMode.Open));

                    PocVertex source = graph.Vertices.Where(x => x.ID == new_vertex.ID).First();
                    var edges = p.GetRelationshipsByType(EdgeRelationship);
                    foreach (var edge in edges)
                    {
                        new_vertex = (PocVertex)vertex_loader.Deserialize(package.GetPart(edge.TargetUri).GetStream(FileMode.Open));
                        PocVertex target = graph.Vertices.Where(x => x.ID == new_vertex.ID).First();
                        PocEdge new_edge = new PocEdge(source, target);
                        if (!(source == target)) graph.AddEdge(new_edge);
                    }
                }
            }

            //foreach (PocVertex x in graph.Vertices)
            //{
            //    if (typeof(x) == typeof(FileBrowser)) root = x;
            //}
            root = (FileBrowser)graph.Vertices.Where(x => (x as FileBrowser) != null).First();
            item_provider.RootDir = root.FilePath;
            package.Close();
            package = null; // Surely not needed.
            NotifyPropertyChanged("Graph");
        }

        ~GraphProvider()
        {
            try
            {
                //package.Close();
            }
            catch (System.NullReferenceException) { }
            catch (System.ObjectDisposedException) { }
            catch (System.ArgumentException) { }
        }
        #endregion
    }
}
