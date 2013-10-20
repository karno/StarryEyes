using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace StarryEyes.Views.Controls.QueryEditorResources
{
    public class CompletionData : ICompletionData
    {
        private readonly string _completion;

        public CompletionData(string text, string desc)
            : this(text, text, desc)
        {
        }


        public CompletionData(string text, string completion, string desc)
        {
            _completion = completion;
            this.Text = text;
            this.Description = desc;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, _completion);
        }

        public ImageSource Image { get { return null; } }
        public string Text { get; private set; }
        public object Content { get { return Text; } }
        public object Description { get; private set; }
        public double Priority { get { return 0; } }
    }
}
