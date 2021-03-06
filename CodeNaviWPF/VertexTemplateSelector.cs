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
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;
using Tybocer.Models;

namespace Tybocer
{
    public class VertexTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileBrowserTemplate
        { get; set; }

        public DataTemplate FileContentView
        { get; set; }

        public DataTemplate SearchResultsView
        { get; set; }

        public DataTemplate CtagsView
        { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FileVertex) return FileContentView;
            if (item is SearchResultsVertex) return SearchResultsView;
            if (item is FileBrowser) return FileBrowserTemplate;
            if (item is CtagsVertex) return CtagsView;
            return base.SelectTemplate(item, container);
        }
    }
}
