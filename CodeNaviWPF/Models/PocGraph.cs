﻿/*
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
using System.Diagnostics;
using System.Threading.Tasks;
using QuickGraph;
using ICSharpCode.AvalonEdit.Document;

namespace CodeNaviWPF.Models
{
    /// <summary>
    /// A simple identifiable vertex.
    /// </summary>
    [DebuggerDisplay("{ID} - {FilePath}")]
    public class PocVertex : INotifyPropertyChanged
    {
        public string ID;
        private ItemProvider ip;
        public String FileText { get; set; }
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
        public String Text { get; private set; }
        public List<Item> Files
        {
            get
            {
                return files;
            }
        }

        public PocVertex(string id, string path, string text)
        {
            ID = id;
            file_path = path;
            FileText = "";
            Text = text;
            files = new List<Item>();
            ip = new ItemProvider();
            files = ip.GetItems(path);
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
    /// <summary>
    /// A simple identifiable edge.
    /// </summary>
    [DebuggerDisplay("{Source.ID} -> {Target.ID}")]
    public class PocEdge : Edge<PocVertex>, INotifyPropertyChanged
    {
        private string id;

        public string ID
        {
            get { return id; }
            set
            {
                id = value;
                NotifyPropertyChanged("ID");
            }
        }

        public PocEdge(string id, PocVertex source, PocVertex target)
            : base(source, target)
        {
            ID = id;
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
        public FileVertex(string id, string path, string text)
            : base(id, path, text)
        {
            Document = new TextDocument();
        }
    }
}
