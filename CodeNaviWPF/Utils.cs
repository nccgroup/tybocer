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
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using System.Threading;

namespace Tybocer.Utils
{
    class IDCounter
    {
        private static int counter = 0;

        public static int Counter
        {
            get { counter++; return counter; }
        }

    }

    class FilePathUtils
    {
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (!fromPath.EndsWith("\\"))
            {
                fromPath += "\\";
            }
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }

    class TreeHelpers
    {
        public static childItem FindVisualChild<childItem>(DependencyObject obj)
        where childItem : DependencyObject
        {
            try
            {
                ((dynamic)obj).ApplyTemplate();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static childItem FindVisualChildByName<childItem>(DependencyObject obj, String name)
        where childItem : DependencyObject
        {
            try
            {
                ((dynamic)obj).ApplyTemplate();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                var fe = child as FrameworkElement;
                if (child != null && child is childItem && fe.Name == name)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static T FindVisualParent<T>(DependencyObject element) where T : UIElement
        {
            DependencyObject parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as DependencyObject;
            }
            return null;
        }

        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }

    [ValueConversion(typeof(string), typeof(bool))]
    class StringToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (string)value == "") return false;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value) return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StringHelpers
    {
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }

    public class PathEnumerators
    {
        public static List<DirectoryInfo> EnumerateAccessibleDirectories(string path, IProgress<int> progress, CancellationToken ct, bool recurse = false)
        {
            return EnumerateAccessibleDirectories(new DirectoryInfo(path), progress, ct, recurse);
        }

        public static List<DirectoryInfo> EnumerateAccessibleDirectories(string path, bool recurse = false)
        {
            return EnumerateAccessibleDirectories(new DirectoryInfo(path), recurse);
        }

        public static List<DirectoryInfo> EnumerateAccessibleDirectories(DirectoryInfo directory, IProgress<int> progress, CancellationToken ct, bool recurse = false)
        {
            List<DirectoryInfo> results = new List<DirectoryInfo>();

            try
            {
                if (progress != null)
                {
                    int first = results.Count;
                    results.AddRange(directory.EnumerateDirectories());
                    progress.Report(results.Count - first);
                }

                if (recurse)
                {
                    foreach (DirectoryInfo dir in directory.EnumerateDirectories())
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            results.AddRange(EnumerateAccessibleDirectories(dir, progress, ct, recurse));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Can get this when accessing network drives.
            }
            return results;
        }

        public static List<DirectoryInfo> EnumerateAccessibleDirectories(DirectoryInfo directory, bool recurse = false)
        {
            List<DirectoryInfo> results = new List<DirectoryInfo>();

            try
            {
                results.AddRange(directory.EnumerateDirectories());
                if (recurse)
                {
                    foreach (DirectoryInfo dir in directory.EnumerateDirectories())
                    {
                        try
                        {
                            results.AddRange(EnumerateAccessibleDirectories(dir));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Can get this when accessing network drives.
            }
            return results;
        }

        public static List<FileInfo> GetFiles(DirectoryInfo directory)
        {
            List<FileInfo> results = new List<FileInfo>();
            
            try
            {
                directory.GetAccessControl();
            }
            catch (UnauthorizedAccessException)
            {
                return results;
            }

            try
            {
                foreach (FileInfo file in directory.EnumerateFiles())
                {
                    try
                    {
                        results.Add(file);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip this
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            return results;
        }
    }
}
