using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
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

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte == null) return;

            var solutionExplorer = dte.ToolWindows.SolutionExplorer;
            var items = solutionExplorer.SelectedItems as object[];
            if (items == null || items.Length == 0) return;

            var logger = OutputWindowLogger.Instance;
            logger.Clear();
            logger.LogLine($"Format All Files started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logger.LogLine(string.Empty);

            int successCount = 0;
            int failCount = 0;

            foreach (UIHierarchyItem item in items)
            {
                FormatItem(item.Object, dte, logger, ref successCount, ref failCount);
            }

            logger.LogLine(string.Empty);
            logger.LogLine($"Format All Files ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logger.LogLine($"  Succeeded: {successCount}");
            logger.LogLine($"  Failed:    {failCount}");
            logger.Activate();
        }

        private void FormatItem(object item, DTE2 dte, OutputWindowLogger logger, ref int successCount, ref int failCount)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item is Solution solution)
            {
                foreach (Project project in solution.Projects)
                {
                    FormatProjectItems(project.ProjectItems, dte, project.Name, logger, ref successCount, ref failCount);
                }
            }
            else if (item is Project project)
            {
                FormatProjectItems(project.ProjectItems, dte, project.Name, logger, ref successCount, ref failCount);
            }
            else if (item is ProjectItem projectItem)
            {
                var projectName = projectItem.ContainingProject?.Name ?? "(Solution)";
                FormatProjectItem(projectItem, dte, projectName, logger, ref successCount, ref failCount);
            }
        }

        private void FormatProjectItems(ProjectItems projectItems, DTE2 dte, string projectName, OutputWindowLogger logger, ref int successCount, ref int failCount)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItems == null) return;

            foreach (ProjectItem item in projectItems)
            {
                FormatProjectItem(item, dte, projectName, logger, ref successCount, ref failCount);
            }
        }

        private void FormatProjectItem(ProjectItem projectItem, DTE2 dte, string projectName, OutputWindowLogger logger, ref int successCount, ref int failCount)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
            {
                FormatProjectItems(projectItem.ProjectItems, dte, projectName, logger, ref successCount, ref failCount);
            }
            else if (IsPhysicalFile(projectItem))
            {
                FormatFile(projectItem, dte, projectName, logger, ref successCount, ref failCount);
            }
        }

        private static bool IsPhysicalFile(ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return string.Equals(
                projectItem.Kind,
                EnvDTE.Constants.vsProjectItemKindPhysicalFile,
                StringComparison.OrdinalIgnoreCase);
        }

        private void FormatFile(ProjectItem projectItem, DTE2 dte, string projectName, OutputWindowLogger logger, ref int successCount, ref int failCount)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var filePath = projectItem.FileNames[0];
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            if (!IsFormatableExtension(extension)) return;

            var relativePath = GetRelativePath(projectItem, filePath);
            var prefix = $"[{projectName}]{relativePath}: ";

            try
            {
                Document doc = null;
                try
                {
                    doc = dte.Documents.Item(filePath);
                }
                catch
                {
                    // Document not open
                }

                if (doc != null)
                {
                    var formatCommand = GetFormatCommand();
                    doc.Activate();
                    dte.ExecuteCommand(formatCommand);
                    if (!doc.Saved) doc.Save();
                }
                else
                {
                    var window = projectItem.Open(EnvDTE.Constants.vsViewKindCode);
                    window.Visible = false;
                    projectItem.Document.Activate();
                    dte.ExecuteCommand(GetFormatCommand());
                    if (!projectItem.Document.Saved) projectItem.Document.Save();
                    window.Close(vsSaveChanges.vsSaveChangesYes);
                }

                logger.LogLine(prefix + "Success");
                successCount++;
            }
            catch (Exception ex)
            {
                logger.LogLine(prefix + $"Failed - {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.LogLine($"    Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                failCount++;
            }
        }

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

        private bool IsFormatableExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            var options = (FormatAllFilesOptions)package.GetDialogPage(typeof(FormatAllFilesOptions));
            return options.GetExtensions().Contains(extension);
        }

        private string GetFormatCommand()
        {
            var options = (FormatAllFilesOptions)package.GetDialogPage(typeof(FormatAllFilesOptions));
            return options.FormatCommand;
        }
    }
}
