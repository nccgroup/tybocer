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

namespace CodeNaviWPF.Models
{
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
        private ItemProvider ip;
     
        [YAXDontSerialize]
        private List<Item> files;
        
        private string file_path;
        public string FilePath
        {
            get { return file_path; }
            set
            {
                file_path = value;
                files = ip.GetItems(value);
                NotifyPropertyChanged("Files");
                NotifyPropertyChanged("FilePath");
            }
        }

        [YAXDontSerialize]
        public List<Item> Files { get { return files; } }

        [YAXDontSerialize]
        private string searchterm;

        [YAXDontSerialize]
        public string SearchTerm
        {
            get { return searchterm; }
            set { searchterm = value; }
        }

        public FileBrowser(string path) : base()
        {
            file_path = path;
            files = new List<Item>();
            ip = new ItemProvider();
            files = ip.GetItems(file_path);
        }
    }

    public class FileVertex : PocVertex
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }

        [YAXDontSerialize]
        public TextDocument Document { get; set; }

        public List<int> LinesToHighlight;

        public FileVertex(string filename, string path, string root) : base()
        {
            base.ID = Utils.IDCounter.Counter;
            FileName = FilePathUtils.GetRelativePath(root, path);            
            FilePath = path;
            LinesToHighlight = new List<int>();
            Document = new TextDocument();
            StreamReader sr = new StreamReader(path);
            Document.Text = sr.ReadToEnd();
        }
    }

    public class SearchResultsVertex : PocVertex
    {
        public string SearchString { get; set; }
        private List<SearchResult> results;
        public List<SearchResult> Results
        {
            get { return results; }
            set { results = value; NotifyPropertyChanged("Results"); }
        }
        public SearchResultsVertex(string search_term)
            : base()
        {
            SearchString = search_term;
            results = new List<SearchResult>();
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

    public class PocEdge : EdgeBase<PocVertex>
    {
        public PocEdge(PocVertex source, PocVertex target)
            : base(source, target) 
        {
            ID = Utils.IDCounter.Counter;
        }
    }

    public class PocGraph : BidirectionalGraph<PocVertex, PocEdge>
    {
        public PocGraph() { }

        public PocGraph(bool allowParallelEdges)
            : base(allowParallelEdges) { }

        public PocGraph(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity) { }
    }    
}
