using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;

namespace FormatAllFiles2
{
    internal sealed class FormatAllFilesCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("6d4a8b3e-2c5f-4d7a-9e1b-8f3c5a7d9e2a");

        private readonly AsyncPackage package;

        private FormatAllFilesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static FormatAllFilesCommand Instance { get; private set; }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new FormatAllFilesCommand(package, commandService);
            OutputWindowLogger.Initialize();
        }

#pragma warning disable VSTHRD100 // async void is required for MenuCommand event handler
        private async void Execute(object sender, EventArgs e)
#pragma warning restore VSTHRD100
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte == null) return;

            var solutionExplorer = dte.ToolWindows.SolutionExplorer;
            var items = solutionExplorer.SelectedItems as object[];
            if (items == null || items.Length == 0) return;

            var formatCommands = GetFormatCommands();

            // ── Phase 1: Collect all formatable files ──
            var files = CollectFiles(items, dte);

            if (files.Count == 0)
            {
                var logger = OutputWindowLogger.Instance;
                logger.Clear();
                logger.LogLine("No formatable files found in the selection.");
                logger.Activate();
                return;
            }

            var logger2 = OutputWindowLogger.Instance;
            logger2.Clear();
            logger2.LogLine($"Format All Files started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logger2.LogLine($"Format Commands: {string.Join("; ", formatCommands)}");
            logger2.LogLine($"Files to format: {files.Count}");
            logger2.LogLine(string.Empty);

            int successCount = 0;
            int failCount = 0;
            int total = files.Count;

            // ── Phase 2: Process files with a custom WPF modal progress dialog ──

            var progressDialog = new ProgressDialog("Format All Files", total);

            // Set the owner to the VS main window so the dialog is properly modal.
            if (System.Windows.Application.Current?.MainWindow != null)
            {
                progressDialog.Owner = System.Windows.Application.Current.MainWindow;
            }

            // Start background work once the dialog is loaded and its Dispatcher is pumping.
            progressDialog.Loaded += (sender2, args2) =>
            {
#pragma warning disable VSTHRD110 // Intentional fire-and-forget: task runs while dialog blocks UI thread
                System.Threading.Tasks.Task.Run(() =>
#pragma warning restore VSTHRD110
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        var fileName = Path.GetFileName(file.FilePath);

                        // Update the progress dialog from the background thread
                        // via the dialog's Dispatcher (runs on the UI thread).
                        progressDialog.Dispatcher.Invoke(() =>
                        {
                            progressDialog.UpdateProgress(
                                current: i,
                                total: total,
                                message: $"Formatting files... ({i + 1}/{total})",
                                detail: $"[{file.ProjectName}] {file.RelativePath}"
                            );
                        });

                        // Execute DTE formatting on the UI thread via Dispatcher.
                        // ShowDialog() pumps messages, so Dispatcher.Invoke can marshal
                        // the call to the UI thread even though it is "blocked".
                        bool fileSuccess = false;
                        progressDialog.Dispatcher.Invoke(() =>
                        {
                            fileSuccess = FormatFile(file, dte, formatCommands, logger2);
                        });

                        if (fileSuccess)
                            successCount++;
                        else
                            failCount++;
                    }

                    // Close the dialog once all files are processed.
                    progressDialog.Dispatcher.Invoke(() => progressDialog.CloseDialog());
                });
            };

            // ShowDialog blocks the UI thread but runs a nested message pump.
            // This allows Dispatcher.Invoke calls from the background thread to
            // execute on the UI thread while the dialog remains modal (blocking
            // all other VS operations). The user cannot close the dialog — it
            // closes automatically when all files have been processed.
            progressDialog.ShowDialog();

            logger2.LogLine(string.Empty);
            logger2.LogLine($"Format All Files ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logger2.LogLine($"  Succeeded: {successCount}");
            logger2.LogLine($"  Failed:    {failCount}");
            logger2.Activate();
        }

        // ──────────────────────────────────────────
        //  File collection (two-pass: collect → format)
        // ──────────────────────────────────────────

        private class FormatFileEntry
        {
            public ProjectItem ProjectItem { get; set; }
            public string ProjectName { get; set; }
            public string RelativePath { get; set; }
            public string FilePath { get; set; }
        }

        private List<FormatFileEntry> CollectFiles(object[] items, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = new List<FormatFileEntry>();
            foreach (UIHierarchyItem item in items)
            {
                CollectFilesFromItem(item.Object, dte, result);
            }
            return result;
        }

        private void CollectFilesFromItem(object item, DTE2 dte, List<FormatFileEntry> result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item is Solution solution)
            {
                foreach (Project project in solution.Projects)
                {
                    CollectFilesFromProjectItems(project.ProjectItems, project.Name, result);
                }
            }
            else if (item is Project project)
            {
                CollectFilesFromProjectItems(project.ProjectItems, project.Name, result);
            }
            else if (item is ProjectItem projectItem)
            {
                var projectName = projectItem.ContainingProject?.Name ?? "(Solution)";
                CollectFilesFromProjectItem(projectItem, projectName, result);
            }
        }

        private void CollectFilesFromProjectItems(ProjectItems projectItems, string projectName, List<FormatFileEntry> result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItems == null) return;

            foreach (ProjectItem item in projectItems)
            {
                CollectFilesFromProjectItem(item, projectName, result);
            }
        }

        private void CollectFilesFromProjectItem(ProjectItem projectItem, string projectName, List<FormatFileEntry> result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.SubProject != null)
            {
                CollectFilesFromProjectItems(projectItem.SubProject.ProjectItems, projectItem.SubProject.Name, result);
            }
            else if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
            {
                CollectFilesFromProjectItems(projectItem.ProjectItems, projectName, result);
            }
            else if (IsPhysicalFile(projectItem))
            {
                var filePath = projectItem.FileNames[0];
                var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

                if (IsFormatableExtension(extension))
                {
                    var relativePath = GetRelativePath(projectItem, filePath);
                    result.Add(new FormatFileEntry
                    {
                        ProjectItem = projectItem,
                        ProjectName = projectName,
                        RelativePath = relativePath,
                        FilePath = filePath
                    });
                }
            }
        }

        // ──────────────────────────────────────────
        //  Single-file formatting
        // ──────────────────────────────────────────

        /// <summary>
        /// Format a single file. Returns true on success, false on failure.
        /// Must be called on the UI thread.
        /// </summary>
        private bool FormatFile(FormatFileEntry entry, DTE2 dte, IList<string> formatCommands, OutputWindowLogger logger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var prefix = $"[{entry.ProjectName}]{entry.RelativePath}: ";

            try
            {
                Document doc = null;
                try
                {
                    doc = dte.Documents.Item(entry.FilePath);
                }
                catch
                {
                    // Document not open
                }

                if (doc != null)
                {
                    doc.Activate();
                    foreach (var cmd in formatCommands)
                        dte.ExecuteCommand(cmd);
                    if (!doc.Saved) doc.Save();
                }
                else
                {
                    var window = entry.ProjectItem.Open(EnvDTE.Constants.vsViewKindCode);
                    window.Visible = false;
                    entry.ProjectItem.Document.Activate();
                    foreach (var cmd in formatCommands)
                        dte.ExecuteCommand(cmd);
                    if (!entry.ProjectItem.Document.Saved) entry.ProjectItem.Document.Save();
                    window.Close(vsSaveChanges.vsSaveChangesYes);
                }

                logger.LogLine(prefix + "Success");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogLine(prefix + $"Failed - {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.LogLine($"    Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // ──────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────

        private static string GetRelativePath(ProjectItem projectItem, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = projectItem.ContainingProject;
            if (project == null) return filePath;

            try
            {
                var projectDir = Path.GetDirectoryName(project.FullName);
                if (projectDir != null && filePath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = filePath.Substring(projectDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return relative;
                }
            }
            catch
            {
                // Fall through to returning the file path
            }

            return filePath;
        }

        private static bool IsPhysicalFile(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return string.Equals(
                projectItem.Kind,
                EnvDTE.Constants.vsProjectItemKindPhysicalFile,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsFormatableExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            var options = (FormatAllFilesOptions)package.GetDialogPage(typeof(FormatAllFilesOptions));
            return options.GetExtensions().Contains(extension);
        }

        private IList<string> GetFormatCommands()
        {
            var options = (FormatAllFilesOptions)package.GetDialogPage(typeof(FormatAllFilesOptions));
            return options.GetCommands();
        }
    }
}
