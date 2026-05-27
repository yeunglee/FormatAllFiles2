using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FormatAllFiles2
{
    public class FormatAllFilesOptions : DialogPage
    {
        private const string DefaultExtensions =
            ".cs; .vb; .cpp; .h; .c; .hpp; .cc; .cxx; " +
            ".xml; .xaml; .axml; .html; .htm; .css; " +
            ".js; .ts; .jsx; .tsx; .json; .sql; " +
            ".cshtml; .vbhtml; .aspx; .ascx; .master; " +
            ".config; .targets; .props; .resx; " +
            ".fs; .razor; .scss; .less; .svg; " +
            ".ps1; .psm1; .psd1; .bat; .cmd; " +
            ".ini; .toml; .yaml; .yml; .proto; .md; .py";

        private const string DefaultFormatCommand = "Edit.FormatDocument";

        private string fileExtensions = DefaultExtensions;
        private string formatCommand = DefaultFormatCommand;
        private HashSet<string> cachedExtensions;
        private string cachedExtensionsSource;

        [Category("Format All Files")]
        [DisplayName("File Extensions")]
        [Description("Semicolon-separated list of file extensions to format (include the dot). "
                   + "Example: .cs; .vb; .xml")]
        public string FileExtensions
        {
            get => fileExtensions;
            set
            {
                fileExtensions = value;
                cachedExtensions = null;
                cachedExtensionsSource = null;
            }
        }

        [Category("Format All Files")]
        [DisplayName("Format Command")]
        [Description("The Visual Studio command to execute for formatting each file. "
                   + "Default: Edit.FormatDocument. Other examples: Edit.FormatSelection, Edit.RemoveAndSort")]
        public string FormatCommand
        {
            get => formatCommand;
            set => formatCommand = value ?? DefaultFormatCommand;
        }

        public ISet<string> GetExtensions()
        {
            var current = FileExtensions;
            if (cachedExtensions != null && cachedExtensionsSource == current)
                return cachedExtensions;

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(current))
            {
                foreach (var part in current.Split(';'))
                {
                    var ext = part.Trim();
                    if (ext.Length > 0)
                    {
                        if (!ext.StartsWith("."))
                            ext = "." + ext;
                        set.Add(ext);
                    }
                }
            }

            cachedExtensions = set;
            cachedExtensionsSource = current;
            return set;
        }

        public override void ResetSettings()
        {
            base.ResetSettings();
            FileExtensions = DefaultExtensions;
            FormatCommand = DefaultFormatCommand;
        }
    }
}
