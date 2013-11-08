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
using System.IO.Packaging;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using GraphX;
using Tybocer.Models;
using Tybocer.Utils;

namespace Tybocer.ViewModels
{
    public class PocGraphLayout : GraphArea<PocVertex, PocEdge, PocGraph> { }

    public class GraphProvider : INotifyPropertyChanged
    {
        private static string EdgeRelationship = "http://schemas.nccgroup.com/package/relationships/edge";

        private ItemProvider item_provider;
        private PocGraph graph;
        public FileBrowser root_vertex;
        public string root_dir = "";
        public bool UsingTempFile = true;
        private string save_file;

        public string SaveFile
        {
            get { return save_file; }
            set { save_file = value; }
        }
        
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
            root_vertex = new FileBrowser("");
            graph.AddVertex(root_vertex);
            //layoutAlgorithmType = "EfficientSugiyama";
            //LayoutAlgorithmType="Circular"
            //LayoutAlgorithmType="CompundFDP"
            //LayoutAlgorithmType="EfficientSugiyama"
            //LayoutAlgorithmType="FR"
            //LayoutAlgorithmType="ISOM"
            //LayoutAlgorithmType="KK"
            //LayoutAlgorithmType="LinLog"
            //LayoutAlgorithmType="Tree"

            SaveFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".vizzy";
        }

        internal void UpdateRoot(string p)
        {
            root_vertex.FilePath = p;
            item_provider.RootDir = p;
        }

        internal FileVertex AddFileView(FileItem f)
        {
            return AddFileView(f, root_vertex);
        }

        internal FileVertex AddFileView(FileItem f, PocVertex from_vertex)
        {
            FileVertex new_vertex;
            FileVertex existing_file_view = (FileVertex)Graph.Vertices.Where(x => x as FileVertex != null && ((FileVertex)x).FileName == f.RelPath).FirstOrDefault();
            if (existing_file_view != null)
            {
                new_vertex = new FileVertex(f.FileName, f.FullPath, root_vertex.FilePath, existing_file_view.Document);
            }
            else
            {
                new_vertex = new FileVertex(f.FileName, f.FullPath, root_vertex.FilePath);
            }
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
                using (StreamReader streamreader = new StreamReader(filestream))
                {
                    string line = streamreader.ReadLine();
                    int count = 0;
                    while (line != null)
                    {
                        count++;
                        if (line.ToLower().Contains(search_term.ToLower()))
                        {
                            results.Add(new SearchResult
                            {
                                RelPath = Path.GetDirectoryName(FilePathUtils.GetRelativePath(root_vertex.FilePath, file_info.FullName)),
                                FullPath = file_info.FullName,
                                FileName = file_info.Name,
                                Extension = file_info.Extension,
                                LineNumber = count,
                                Line = line.Length > 500 ? line.TrimStart().Substring(0, 500) : line.TrimStart()
                            });
                        }
                        line = streamreader.ReadLine();
                    }
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

            results = SearchDirectory(search_term, results_vertex.ExtensionsToSearch, new DirectoryInfo(root_vertex.FilePath), progress);

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
        public void SaveGraph(string file_name=null)
        {
            if ((file_name != null && UsingTempFile) || (file_name != null && file_name != SaveFile))
            {
                if (File.Exists(SaveFile) && UsingTempFile) File.Delete(SaveFile);
                SaveFile = file_name;
                UsingTempFile = false;
            }
            
            serialize_graph();
            Properties.Settings.Default.PreviousFile = SaveFile;
        }

        private void serialize_graph()
        {
            Uri sourceVertUri;
            Uri targetVertUri;
            PackagePart source_vert_part;
            PackagePart target_vert_part;
            Stream vert_stream;
            XmlSerializer vert_serializer = new XmlSerializer(typeof(PocVertex));

            using (Package package = Package.Open(SaveFile, FileMode.Create))
            {

                Uri rootVertexUri = PackUriHelper.CreatePartUri(new Uri("vertices/vertex-" + root_vertex.ID.ToString(), UriKind.Relative));
                PackagePart rootPart = package.CreatePart(rootVertexUri, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);

                using (vert_stream = rootPart.GetStream(FileMode.Create))
                {
                    vert_serializer.Serialize(vert_stream, root_vertex);
                }

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

                    SaveNotes(package);
                }
            }
        }

        public void LoadProject(string filename)
        {
            using (Package package = Package.Open(filename, FileMode.OpenOrCreate))
            {
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
                    else if (p.Uri.OriginalString == "/notes/notes.txt")
                    {
                        using (var sr = new StreamReader(p.GetStream()))
                        {
                            graph.Notes.Text = sr.ReadToEnd();
                        }
                    }
                }

                root_vertex = (FileBrowser)graph.Vertices.Where(x => (x as FileBrowser) != null).First();
                item_provider.RootDir = root_vertex.FilePath;
                root_dir = root_vertex.FilePath;
            }

            UsingTempFile = false;
            SaveFile = filename;
            NotifyPropertyChanged("Graph");
        }
        #endregion

        internal CtagsVertex AddCtagsAnchor(string tag, PocVertex source_vertex)
        {
            CtagsVertex new_vertex = new CtagsVertex(tag);
            Graph.AddVertex(new_vertex);
            Graph.AddEdge(new PocEdge(source_vertex, new_vertex));
            return new_vertex;
        }

        internal PocVertex GetVertexById(string id)
        {
            return GetVertexById(int.Parse(id));
        }

        internal PocVertex GetVertexById(int id)
        {
            PocVertex result = graph.Vertices.Where(x => x.ID == id).First();
            return result;
        }

        internal void SaveNotes()
        {
            using (Package package = Package.Open(SaveFile, FileMode.Open))
            {
                SaveNotes(package);
            }
        }

        private void SaveNotes(Package package)
        {
            Uri notesUri = PackUriHelper.CreatePartUri(new Uri("notes/notes.txt", UriKind.Relative));
            PackagePart notesPart;
            if (!package.PartExists(notesUri))
            {
                notesPart = package.CreatePart(notesUri, System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum);
            }
            else
            {
                notesPart = package.GetPart(notesUri);
            }
            using (var notes_stream = new StreamWriter(notesPart.GetStream(FileMode.Create), System.Text.Encoding.UTF8))
            {
                notes_stream.Write(graph.Notes.Text);
            }
        }
    }
}
