using System;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace Sophia.Messaging.IO
{
    public sealed class FolderBrowserMessage : ResponsiveMessageBase<string>
    {
        public FolderBrowserConfiguration Configuration { get; set; }

        public FolderBrowserMessage([NotNull] FolderBrowserConfiguration configuration)
            : this(null, configuration)
        {
        }

        public FolderBrowserMessage([CanBeNull] string key, [NotNull] FolderBrowserConfiguration configuration)
            : base(key)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            Configuration = configuration;
        }
    }

    public sealed class FolderBrowserConfiguration
    {
        public string Description { get; set; }

        public Environment.SpecialFolder RootFolder { get; set; }

        public string SelectedPath { get; set; }

        public bool ShowNewFolderButton { get; set; }

        public void Apply([NotNull] FolderBrowserDialog dialog)
        {
            dialog.Description = Description;
            dialog.RootFolder = RootFolder;
            dialog.SelectedPath = SelectedPath;
            dialog.ShowNewFolderButton = ShowNewFolderButton;
        }
    }
}