using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Sophia.Messaging.IO
{
    public abstract class FileMessage<T> : ResponsiveMessageBase<string[]> where T : FileDialog
    {
        [NotNull]
        public FileDialogConfiguration<T> Configuration { get; }

        protected FileMessage([NotNull] FileDialogConfiguration<T> configuration)
            : this(null, configuration)
        {
        }

        protected FileMessage([CanBeNull] string key, [NotNull] FileDialogConfiguration<T> configuration)
            : base(key)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            Configuration = configuration;
        }
    }

    public abstract class FileDialogConfiguration<T> where T : FileDialog
    {
        public bool AddExtension { get; set; }

        public bool CheckFileExists { get; set; }

        public bool CheckPathExists { get; set; }

        [NotNull]
        public IList<FileDialogCustomPlace> CustomPlaces { get; }

        public string DefaultExt { get; set; }

        public bool DereferenceLinks { get; set; }

        public string FileName { get; set; }

        public string Filter { get; set; }

        public int FilterIndex { get; set; }

        public string InitialDirectory { get; set; }

        public object Tag { get; set; }

        public string Title { get; set; }

        public bool ValidateNames { get; set; }

        protected FileDialogConfiguration()
        {
            // initialize placeholders and set default values
            AddExtension = true;
            CheckFileExists = false;
            CheckPathExists = true;
            CustomPlaces = new List<FileDialogCustomPlace>();
            DefaultExt = String.Empty;
            // according to MSDN, this property is false in default.
            // https://msdn.microsoft.com/ja-jp/library/microsoft.win32.filedialog.dereferencelinks(v=vs.110).aspx
            // however, MSDN for WinForms version says this property is true in default.
            // https://msdn.microsoft.com/ja-jp/library/system.windows.forms.filedialog.dereferencelinks(v=vs.110).aspx
            // we think true value is desiable in this property.
            DereferenceLinks = true;
            FileName = String.Empty;
            Filter = String.Empty;
            FilterIndex = 1;
            InitialDirectory = String.Empty;
            Title = String.Empty;
            ValidateNames = false;
        }

        public abstract void Apply([NotNull] T dialog);

        protected void ApplyCore([NotNull] T dialog)
        {
            dialog.AddExtension = AddExtension;
            dialog.CheckFileExists = CheckFileExists;
            dialog.CheckPathExists = CheckPathExists;
            dialog.CustomPlaces = CustomPlaces;
            dialog.DefaultExt = DefaultExt;
            dialog.DereferenceLinks = DereferenceLinks;
            dialog.FileName = FileName;
            dialog.Filter = Filter;
            dialog.FilterIndex = FilterIndex;
            dialog.InitialDirectory = InitialDirectory;
            dialog.Tag = Tag;
            dialog.Title = Title;
            dialog.ValidateNames = ValidateNames;
        }
    }
}