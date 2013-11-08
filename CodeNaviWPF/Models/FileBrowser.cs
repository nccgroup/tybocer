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
using System.IO;

namespace Tybocer.Models
{

    public class Item
    {
        public string FullPath { get; set; }
        public string RelPath { get; set; }
        public string FileName { get; set; }
    }

    public class FileItem : Item
    {
        public string Extension { get; set; }
    }

    public class DirectoryItem : Item
    {
        public List<Item> Items { get; set; }

        public DirectoryItem()
        {
            Items = new List<Item>();
            Items.Add(new FileItem { FullPath = "DummyName", RelPath = "DummyPath" });
        }
    }

    public partial class ItemProvider
    {
        public string RootDir { get; set; }

        public List<Item> GetItems(string value)
        {
            List<Item> items = new List<Item>();
            if (!System.IO.Directory.Exists(value)) { return items; }
            var dirinfo = new DirectoryInfo(value);
            foreach (DirectoryInfo d in Utils.PathEnumerators.EnumerateAccessibleDirectories(dirinfo))
            //foreach (DirectoryInfo d in dirinfo.EnumerateDirectories())
            {
                try
                {
                    var item = new DirectoryItem
                    {
                        FileName = d.Name,
                        RelPath = Utils.FilePathUtils.GetRelativePath(RootDir, d.FullName),
                        FullPath = d.FullName
                    };
                    items.Add(item);
                }
                catch (System.UnauthorizedAccessException)
                {
                    var item = new DirectoryItem
                    {
                        RelPath = "Access Denied",
                        FullPath = value,
                        Items = new List<Item>()
                    };
                }
            }

            foreach (FileInfo f in dirinfo.EnumerateFiles())
            {
                var item = new FileItem
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    RelPath = Utils.FilePathUtils.GetRelativePath(RootDir, f.FullName),
                };
                items.Add(item);
            }
            return items;
        }

        private List<Item> _files = new List<Item>();
        public List<Item> Files
        {
            get { return _files; }
            set { }
        }
    }
}

