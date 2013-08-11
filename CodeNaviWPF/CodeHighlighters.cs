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
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace CodeNaviWPF
{
    public class HighlightSearchLineBackgroundRenderer : IBackgroundRenderer
    {
        private TextEditor _editor;
        private int _line;

        public HighlightSearchLineBackgroundRenderer(TextEditor editor, int line)
        {
            _editor = editor;
            _line = line;
        }

        public KnownLayer Layer
        {
            get { return KnownLayer.Background; }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_editor.Document == null)
                return;

            if (!(_line > 0)) return;
            textView.EnsureVisualLines();
            var currentLine = _editor.Document.GetLineByNumber(_line);
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
            {
                drawingContext.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0xFF)), null,
                    new Rect(rect.Location, new Size(textView.ActualWidth - 32, rect.Height)));
            }
        }
    }
}
