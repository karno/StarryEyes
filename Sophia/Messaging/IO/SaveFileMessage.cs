using JetBrains.Annotations;
using Microsoft.Win32;

namespace Sophia.Messaging.IO
{
    public class SaveFileMessage : FileMessage<SaveFileDialog>
    {
        public SaveFileMessage([NotNull] SaveFileDialogConfiguration configuration) : base(configuration)
        {
        }

        public SaveFileMessage(string key, [NotNull] SaveFileDialogConfiguration configuration)
            : base(key, configuration)
        {
        }
    }

    public class SaveFileDialogConfiguration : FileDialogConfiguration<SaveFileDialog>
    {
        public bool CreatePrompt { get; set; }

        public bool OverwritePrompt { get; set; }

        public SaveFileDialogConfiguration()
        {
            // set default value
            CreatePrompt = false;
            OverwritePrompt = true;
        }

        public override void Apply(SaveFileDialog dialog)
        {
            ApplyCore(dialog);
            dialog.CreatePrompt = CreatePrompt;
            dialog.OverwritePrompt = OverwritePrompt;
        }
    }
}