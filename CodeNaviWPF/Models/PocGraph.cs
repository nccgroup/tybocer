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
using QuickGraph;
using ICSharpCode.AvalonEdit.Document;
using GraphX;
using CodeNaviWPF.Utils;

namespace CodeNaviWPF.Models
{
    public class PocVertex : VertexBase, INotifyPropertyChanged
    {

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
        //public string ID;
        private ItemProvider ip;
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
        public List<Item> Files { get { return files; } }

        private string searchterm;

        public string SearchTerm
        {
            get { return searchterm; }
            set { searchterm = value; }
        }

        public FileBrowser(string path)
        {
            //ID = id;
            file_path = path;
            files = new List<Item>();
            ip = new ItemProvider();
            files = ip.GetItems(file_path);
        }

        public override string ToString()
        {
            return string.Format("{1}", file_path);
        }

    }

    public class FileVertex : PocVertex
    {
        public string _rootdir;
        public string FileName { get; set; }
        
        public TextDocument Document { get; set; }
        //private string file_path;
        public string FilePath { get; set; }
        //{
        //    get { return file_path; }
        //    set
        //    {
        //        file_path = value;
        //        NotifyPropertyChanged("FilePath");
        //    }
        //}

        public FileVertex(string filename, string path, string root)
        {
            FileName = FilePathUtils.GetRelativePath(root, path);
            FilePath = path;
            _rootdir = root;
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
            set { results = value; NotifyPropertyChanged("Results"); NotifyPropertyChanged("Graph"); }
        }
        public SearchResultsVertex(string search_term)
        {
            SearchString = search_term;
            results = new List<SearchResult>();
        }
    }

    public class SearchResult
    {
        public string FullPath { get; set; }
        public string RelPath { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string Line { get; set; }
        public int LineNumber { get; set; }
    }

    public class PocEdge : EdgeBase<PocVertex>
    {
        public PocEdge(string id, PocVertex source, PocVertex target)
            : base(source, target) { }
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
