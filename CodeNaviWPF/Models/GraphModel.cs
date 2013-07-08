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
using GraphSharp.Controls;

namespace CodeNaviWPF.Models
{
    class GraphProvider
    {
        private string _rootdir;
        public string RootDir
        {
            get { return _rootdir; }
            set
            {
                _rootdir = value;
            }
        }
    }
}
