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
using QuickGraph;
using ICSharpCode.AvalonEdit.Document;
using GraphX;

namespace CodeNaviWPF.Models
{
    public class PocVertex : VertexBase, INotifyPropertyChanged
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
            }
        }
        public List<Item> Files { get { return files; } }

        public PocVertex(string id, string path)
        {
            //ID = id;
            file_path = path;
            files = new List<Item>();
            ip = new ItemProvider();
            files = ip.GetItems(file_path);
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", ID, file_path);
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    public class PocEdge : EdgeBase<PocVertex>, INotifyPropertyChanged
    {
        //private string id;

        //public string ID
        //{
        //    get { return id; }
        //    set
        //    {
        //        id = value;
        //        NotifyPropertyChanged("ID");
        //    }
        //}

        public PocEdge(string id, PocVertex source, PocVertex target)
            : base(source, target)
        {
            //ID = id;
        }


        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }
    public class PocGraph : BidirectionalGraph<PocVertex, PocEdge>
    {
        public PocGraph() { }

        public PocGraph(bool allowParallelEdges)
            : base(allowParallelEdges) { }

        public PocGraph(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity) { }
    }

    public class FileVertex : PocVertex
    {
        public TextDocument Document { get; set; }
        public FileVertex(string id, string path)
            : base(id, path)
        {
            Document = new TextDocument();
        }
    }

    public class SearchResultsVertex: PocVertex
    {
        public string SearchString { get; set; }
        private List<SearchResult> results;
        public List<SearchResult> Results
        {
            get { return results; }
            set { results = value; }
        }
        public SearchResultsVertex(string id, string search_term)
            : base(id, search_term)
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
}
