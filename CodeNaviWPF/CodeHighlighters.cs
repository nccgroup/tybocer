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
using ICSharpCode.AvalonEdit.Document;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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

    public class EscapeSequenceLineTransformer : IVisualLineTransformer
    {
        private List<string> _tags;
        
        public EscapeSequenceLineTransformer(List<string> tags)
        {
            _tags = tags;
        }

        public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
        {
            foreach (VisualLineElement element in elements)
            {
                //if (element is EscapeSequenceElement)
                //{
                //    currentEscapeSequence = (EscapeSequenceElement)element;
                //}
                //else if (currentEscapeSequence != null)
                //{
                //    element.TextRunProperties.SetForegroundBrush(currentEscapeSequence.ForegroundBrush);
                //}
            }
        }
    }

    public class UnderlineCtagsMatches : DocumentColorizingTransformer
    {
        private List<string> _tags;
        
        public UnderlineCtagsMatches(List<string> tags)
        {
            _tags = tags;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            foreach (string tag in _tags)
            {
                Regex reg = new Regex(@"\b" + tag + @"\b", RegexOptions.None);
                int start = 0;
                int index;
                foreach (Match match in reg.Matches(text))
                {
                    index = match.Index;
                //while ((index = reg.intext.IndexOf(tag, start)) >= 0)
                //{
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + tag.Length, // endOffset
                        (VisualLineElement element) =>
                        {
                            // This lambda gets called once for every VisualLineElement
                            // between the specified offsets.
                            Typeface tf = element.TextRunProperties.Typeface;
                            // Replace the typeface with a modified version of
                            // the same typeface
                            element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            //element.TextRunProperties.SetTypeface(new Typeface(
                            //    tf.FontFamily,
                            //    FontStyles.Italic,
                            //    FontWeights.Bold,
                            //    tf.Stretch
                            //));
                        });
                    start = index + 1; // search for next occurrence
                }
            }
        }
    }
}
