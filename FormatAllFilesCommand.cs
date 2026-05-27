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
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte == null) return;

            var solutionExplorer = dte.ToolWindows.SolutionExplorer;
            var items = solutionExplorer.SelectedItems as object[];
            if (items == null || items.Length == 0) return;

            foreach (UIHierarchyItem item in items)
            {
                FormatItem(item.Object, dte);
            }
        }

        private void FormatItem(object item, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item is Solution solution)
            {
                foreach (Project project in solution.Projects)
                {
                    FormatProjectItems(project.ProjectItems, dte);
                }
            }
            else if (item is Project project)
            {
                FormatProjectItems(project.ProjectItems, dte);
            }
            else if (item is ProjectItem projectItem)
            {
                FormatProjectItem(projectItem, dte);
            }
        }

        private void FormatProjectItems(ProjectItems projectItems, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItems == null) return;

            foreach (ProjectItem item in projectItems)
            {
                FormatProjectItem(item, dte);
            }
        }

        private void FormatProjectItem(ProjectItem projectItem, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
            {
                FormatProjectItems(projectItem.ProjectItems, dte);
            }
            else if (IsPhysicalFile(projectItem))
            {
                FormatFile(projectItem, dte);
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

        private void FormatFile(ProjectItem projectItem, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var filePath = projectItem.FileNames[0];
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            if (!IsFormatableExtension(extension)) return;

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
                    doc.Activate();
                    dte.ExecuteCommand("Edit.FormatDocument");
                    if (!doc.Saved) doc.Save();
                }
                else
                {
                    var window = projectItem.Open(EnvDTE.Constants.vsViewKindCode);
                    window.Visible = false;
                    dte.ExecuteCommand("Edit.FormatDocument");
                    if (!projectItem.Document.Saved) projectItem.Document.Save();
                    window.Close(vsSaveChanges.vsSaveChangesYes);
                }
            }
            catch
            {
                // Skip files that can't be formatted
            }
        }

        private bool IsFormatableExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            var options = (FormatAllFilesOptions)package.GetDialogPage(typeof(FormatAllFilesOptions));
            return options.GetExtensions().Contains(extension);
        }
    }
}
