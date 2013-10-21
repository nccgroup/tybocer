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
    public class HighlightNotesSnippetBackgroundRenderer : IBackgroundRenderer
    {
        private TextEditor _editor;
        private int start_line;
        private int no_lines;

        public HighlightNotesSnippetBackgroundRenderer(TextEditor editor, int start_line, int no_lines)
        {
            _editor = editor;
            this.start_line = start_line;
            this.no_lines = no_lines;
        }

        public KnownLayer Layer
        {
            get { return KnownLayer.Background; }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_editor.Document == null)
                return;

            for (int i=start_line; i<start_line+no_lines-1; i++)
            {
                if (!(i > 0)) return;
                textView.EnsureVisualLines();
                var currentLine = _editor.Document.GetLineByNumber(i);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(0x20, 0, 0xFF, 0)), null,
                        new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height)));
                }
            }
        }
    }

    public class HighlightSearchLineBackgroundRenderer : IBackgroundRenderer
    {
        private TextEditor _editor;
        private List<int> _lines = new List<int>();

        public HighlightSearchLineBackgroundRenderer(TextEditor editor, int line)
        {
            _editor = editor;
            //_lines = new List<int>();
            _lines.Add(line);
        }

        public HighlightSearchLineBackgroundRenderer(TextEditor editor, List<int> lines)
        {
            _editor = editor;
            if (lines != null)
            {
                _lines.AddRange(lines);
            }
        }

        public KnownLayer Layer
        {
            get { return KnownLayer.Background; }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_editor.Document == null)
                return;

            foreach (int line in _lines)
            {
                if (!(line > 0)) return;
                textView.EnsureVisualLines();
                var currentLine = _editor.Document.GetLineByNumber(line);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0xFF)), null,
                        new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height)));
                }
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
                int index;
                foreach (Match match in reg.Matches(text))
                {
                    index = match.Index;
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + tag.Length, // endOffset
                        (VisualLineElement element) =>
                        {
                            // This lambda gets called once for every VisualLineElement
                            // between the specified offsets.
                            //Typeface tf = element.TextRunProperties.Typeface;
                            // Replace the typeface with a modified version of
                            // the same typeface
                            element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            //element.TextRunProperties.SetTypeface(new Typeface(
                            //    tf.FontFamily,
                            //    FontStyles.Italic,
                            //    FontWeights.Bold,
                            //    tf.Stretch
                            //));
                        }
                    );
                }
            }
        }
    }

    public class NotesLinkUnderliner : DocumentColorizingTransformer
    {
        private string notes_link_regex = @"(?<vertex_id>\d+):(?<file_name>.+):\(Line (?<line_no>\d+), Col \d+\):(?<no_lines>\d+)";

        public NotesLinkUnderliner() { }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            var match = Regex.Match(text, notes_link_regex);
            if (match != null)
            {
                int index;
                index = match.Index;
                base.ChangeLinePart(
                    lineStartOffset + index, // startOffset
                    lineStartOffset + index + match.Length, // endOffset
                    (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                    }
                );
            }
        }
    }

    public class HighlightSelection : DocumentColorizingTransformer
    {
        private string _selection;

        public HighlightSelection(string selection)
        {
            _selection = selection;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            int index = 0;
            string text = CurrentContext.Document.GetText(line);
            if (!(string.IsNullOrEmpty(_selection) || string.IsNullOrWhiteSpace(_selection)))
            {
                while ((index = text.IndexOf(_selection, index)) != -1)
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + _selection.Length, // endOffset
                        (VisualLineElement element) =>
                        {
                            // This lambda gets called once for every VisualLineElement
                            // between the specified offsets.
                            //Typeface tf = element.TextRunProperties.Typeface;
                            // Replace the typeface with a modified version of
                            // the same typeface
                            element.TextRunProperties.SetBackgroundBrush(Brushes.AliceBlue);
                            //element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            //element.TextRunProperties.SetTypeface(new Typeface(
                            //    tf.FontFamily,
                            //    FontStyles.Italic,
                            //    FontWeights.Bold,
                            //    tf.Stretch
                            //));
                        }
                    );
                    index = index + _selection.Length;
                }
            }
        }
    }
}
