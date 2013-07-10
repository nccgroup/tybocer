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
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeNaviWPF.Models
{
    public class Item
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
    public class FileItem : Item
    {

    }

    public class DirectoryItem : Item
    {
        public List<Item> Items { get; set; }

        public DirectoryItem()
        {
            Items = new List<Item>();
            Items.Add(new FileItem
            {
                Name = "DummyName",
                Path = "DummyPath"
            });
        }
    }


    /// <summary>
    /// Interaction logic for FileBrowser.xaml
    /// </summary>
    public partial class ItemProvider
    {
        private string _rootdir;
        public string RootDir
        {
            get { return _rootdir; }
            set
            {
                _rootdir = value;
                _files.Clear();
                _files = GetItems(value);
            }
        }

        public List<Item> GetItems(string value)
        {
            List<Item> items = new List<Item>();
            if (!System.IO.Directory.Exists(value)) { return items; }
            var dirinfo = new DirectoryInfo(value);
            foreach (DirectoryInfo d in dirinfo.GetDirectories())
            {
                try
                {

                    var item = new DirectoryItem
                    {
                        Name = d.Name,
                        Path = d.FullName,
                        //Items = GetItems(d.FullName)
                    };
                    items.Add(item);

                }
                catch (System.UnauthorizedAccessException)
                {
                    var item = new DirectoryItem
                    {
                        Name = "Access Denied",
                        Path = value,
                        Items = new List<Item>()
                    };
                }
            }
            foreach (FileInfo f in dirinfo.GetFiles())
            {
                var item = new FileItem
                {
                    Name = f.Name,
                    Path = f.FullName
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

