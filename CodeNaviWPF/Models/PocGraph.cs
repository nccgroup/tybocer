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
using System.ComponentModel;
using System.IO;
using System;
using QuickGraph;
using ICSharpCode.AvalonEdit.Document;
using GraphX;
using CodeNaviWPF.Utils;
using System.Xml.Serialization;
using YAXLib;
using System.Text;
using System.Linq;

namespace CodeNaviWPF.Models
{
    [XmlInclude(typeof(FileBrowser))]
    [XmlInclude(typeof(FileVertex))]
    [XmlInclude(typeof(SearchResultsVertex))]
    [XmlInclude(typeof(CtagsVertex))]
    public class PocVertex : VertexBase, INotifyPropertyChanged
    {
        public PocVertex()
        {
            ID = Utils.IDCounter.Counter;
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public class FileBrowser : PocVertex
    {
        [YAXDontSerialize]
        [XmlIgnore]
        private ItemProvider ip;
     
        [YAXDontSerialize]
        [XmlIgnore]
        private List<Item> files;

        [XmlIgnore]
        private Dictionary<string, List<List<string>>> ctags_matches;

        [XmlIgnore]
        public Dictionary<string, List<List<string>>> CtagsMatches
        {
            get { return ctags_matches; }
            set { ctags_matches = value; }
        }

        [XmlElement("CtagsMatches")]
        public String ctags_as_string
        {
            get {
                var sb = new StringBuilder();
                foreach (var key in CtagsMatches.Keys)
                {
                    foreach (var fieldlist in CtagsMatches[key])
                    {
                        sb.AppendLine(String.Join("\t", fieldlist));
                    }
                }
                return sb.ToString();
            }
        
            set
            {
                string new_value = value as string;
                using (StringReader sr = new StringReader(value))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("!")) continue; // Skip comments lines

                        List<string> fields = line.Split(new string[] { "\t" }, StringSplitOptions.None).ToList();
                        try
                        {
                            CtagsMatches[fields[0]].Add(fields);
                        }
                        catch (KeyNotFoundException)
                        {
                            CtagsMatches.Add(fields[0], new List<List<string>>());
                            CtagsMatches[fields[0]].Add(fields);
                        }
                    }
                }
            }
        }
        

        public bool CtagsRun { get; set; }
        
        private string file_path;
        public string FilePath
        {
            get { return file_path; }
            set
            {
                file_path = value;
                ip.RootDir = value;
                files = ip.GetItems(value);
                NotifyPropertyChanged("Files");
                NotifyPropertyChanged("FilePath");
            }
        }

        [YAXDontSerialize]
        [XmlIgnore]
        public List<Item> Files { get { return files; } }

        [YAXDontSerialize]
        [XmlIgnore]
        private string searchterm;

        [YAXDontSerialize]
        [XmlIgnore]
        public string SearchTerm
        {
            get { return searchterm; }
            set { searchterm = value; }
        }

        public FileBrowser() 
        { 
            files = new List<Item>();
            ip = new ItemProvider();
            ctags_matches = new Dictionary<string, List<List<string>>>();
        }

        public FileBrowser(string path) : base()
        {
            file_path = path;
            files = new List<Item>();
            ip = new ItemProvider();
            ctags_matches = new Dictionary<string, List<List<string>>>();
            files = ip.GetItems(file_path);
        }
    }

    public class FileVertex : PocVertex
    {
        public string FileName { get; set; }

        private string _filepath;
        public string FilePath
        {
            get { return _filepath; }
            set
            {
                _filepath = value;
                StreamReader sr = new StreamReader(value);
                Document.Text = sr.ReadToEnd();
            }
        }

        [YAXDontSerialize]
        [XmlIgnore]
        public TextDocument Document { get; set; }

        public List<int> LinesToHighlight;

        public FileVertex()
            : base()
        {
            LinesToHighlight = new List<int>();
            Document = new TextDocument();
        }

        public FileVertex(string filename, string path, string root) : base()
        {
            base.ID = Utils.IDCounter.Counter;
            Document = new TextDocument();
            FileName = FilePathUtils.GetRelativePath(root, path);
            FilePath = path;
            LinesToHighlight = new List<int>();
            StreamReader sr = new StreamReader(path);
            Document.Text = sr.ReadToEnd();
        }
    }

    public class SearchResultsVertex : PocVertex
    {
        private bool _searchRunning;

        public List<string> ExtensionsToSearch { get; set; }

        public bool SearchRunning
        {
            get { return _searchRunning; }
            set { _searchRunning = value; NotifyPropertyChanged("SearchRunning"); }
        }
        
        public string SearchString { get; set; }
        private List<SearchResult> results;
        public List<SearchResult> Results
        {
            get { return results; }
            set { results = value; NotifyPropertyChanged("Results"); }
        }

        public SearchResultsVertex()
        {
            results = new List<SearchResult>();
        }

        public SearchResultsVertex(string search_term)
            : base()
        {
            SearchString = search_term;
            results = new List<SearchResult>();
            SearchRunning = false;
        }
    }

    public class SearchResult : INotifyPropertyChanged
    {
        private bool _checked;
        public bool Checked {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                NotifyPropertyChanged("Checked");
            }
        }

        public string FullPath { get; set; }
        public string RelPath { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        [YAXDontSerialize]
        [XmlIgnore]
        public string Line { get; set; }

        public string EncodedLine
        {
            get { return Convert.ToBase64String(
                Utils.StringHelpers.GetBytes(Line)); }
            set { Line = Utils.StringHelpers.GetString(Convert.FromBase64String(value)); }
        }

        public int LineNumber { get; set; }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public class CtagsVertex : PocVertex
    {
        public string Tag { get; set; }
     
        public CtagsVertex()
        {
            Tag = "";
        }
        
        public CtagsVertex(string tag)
            : base()
        {
            Tag = tag;
        }
    }

    public class PocEdge : EdgeBase<PocVertex>
    {
        public PocEdge()
            : base(null, null, 1)
        { }

        public PocEdge(PocVertex source, PocVertex target)
            : base(source, target) 
{
            ID = Utils.IDCounter.Counter;
        }
    }

    public class PocGraph : BidirectionalGraph<PocVertex, PocEdge>
    {
        public TextDocument Document { get; set; }

        public PocGraph()
        {
            Document = new TextDocument();
        }

        public PocGraph(bool allowParallelEdges)
            : base(allowParallelEdges) { Document = new TextDocument(); }

        public PocGraph(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity) { Document = new TextDocument(); }
    }    
}
