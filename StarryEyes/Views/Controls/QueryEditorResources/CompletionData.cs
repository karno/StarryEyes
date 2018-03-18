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
            Text = text;
            Description = desc;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, _completion);
        }

        public ImageSource Image => null;
        public string Text { get; }
        public object Content => Text;
        public object Description { get; }
        public double Priority => 0;
    }
}