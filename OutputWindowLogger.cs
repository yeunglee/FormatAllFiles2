using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace FormatAllFiles2
{
    internal sealed class OutputWindowLogger
    {
        private static readonly Guid PaneGuid = new Guid("f8a5c2b7-3d1e-4f9a-8c6d-2e4f7a1b5d3c");
        private const string PaneTitle = "Format All Files";

        private readonly IVsOutputWindowPane pane;

        private OutputWindowLogger()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
            {
                pane = null;
                return;
            }

            var guid = PaneGuid;
            var hr = outputWindow.GetPane(ref guid, out pane);
            if (hr != VSConstants.S_OK || pane == null)
            {
                outputWindow.CreatePane(ref guid, PaneTitle, 1, 1);
                outputWindow.GetPane(ref guid, out pane);
            }
        }

        public static OutputWindowLogger Instance { get; private set; }

        public static void Initialize()
        {
            Instance = new OutputWindowLogger();
        }

        public void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            pane?.Clear();
        }

        public void Activate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            pane?.Activate();
        }

        public void LogLine(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pane != null && !string.IsNullOrEmpty(message))
            {
                pane.OutputStringThreadSafe(message + Environment.NewLine);
            }
        }
    }
}
