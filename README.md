# Format All Files

A Visual Studio extension that formats all code files in a solution, project, or folder with a single click — and executes arbitrary Visual Studio commands against them.

## Overview

Right-click any solution, project, or folder in **Solution Explorer** and choose **Format All Files**. The extension walks every matching file, opens it, runs your configured commands, saves, and closes it — all automatically. A dedicated output pane logs every file processed, so you can see exactly what happened.

This project is a ground-up reimplementation of the now-unmaintained [FormatAllFiles](https://github.com/munyabe/FormatAllFiles) by Munyabe. In practice, [CodeMaid](https://www.codemaid.net) has since grown to fully cover (and exceed) what the original extension offered. I rebuilt this extension from scratch for two reasons: to get hands-on experience with **vibe coding** and to learn the ins and outs of **VSIX extension development**. Every line of code here was produced through vibe coding.

## Features

- **Bulk formatting** — format every eligible file in a solution, project, or folder from the context menu.
- **Configurable file extensions** — specify exactly which file types to process (e.g., `.cs`, `.tsx`, `.py`). Defaults to a broad set covering C#, C++, web, scripting, and configuration files.
- **Multiple format commands** — run more than one Visual Studio command per file. The default is `Edit.FormatDocument`, but you can chain anything: `Edit.FormatDocument; Edit.RemoveAndSort` to format *and* sort usings, for example.
- **Output window logging** — each run writes a timestamped log to a dedicated **Format All Files** pane in the Visual Studio Output window, including per-file success/failure status and exception details.
- **Handles unopened files** — files that aren't already open in the editor are opened silently, formatted, saved, and closed without you needing to touch them.
- **Visual Studio 2022 and later** — targets VS 17.0+ across Community, Professional, and Enterprise editions.

## Configuration

Open **Tools → Options → Format All Files → General** to customize:

| Setting | Description | Default |
|---|---|---|
| **File Extensions** | Semicolon-separated list of extensions to process (include the dot). | `.cs; .vb; .cpp; .h; …` (40+ types) |
| **Format Commands** | Semicolon-separated list of VS command names to execute on each file. | `Edit.FormatDocument` |

## Installation

Install directly from the Visual Studio Marketplace:

**[Format All Files on Marketplace](https://marketplace.visualstudio.com/items?itemName=yeunglee.FormatAllFiles2)**

Or build from source — open the solution in Visual Studio and compile to produce a `.vsix` package, then double-click it to install. The extension will appear under **Tools → Extensions and Updates** as *Format All Files*.

```bash
# Build from the command line
msbuild FormatAllFiles2.csproj /p:Configuration=Release
```

## Usage

1. Open a solution in Visual Studio.
2. In **Solution Explorer**, right-click a solution, project, or folder.
3. Select **Format All Files** from the context menu.
4. Watch the **Format All Files** output pane for progress and results.

## How It Works

The command traverses the selected item's hierarchy:

- **Solution** — iterates every project.
- **Project** — iterates every project item recursively, descending into sub-projects and nested folders.
- **Folder / individual file** — processes only the items under that node.

For each physical file whose extension matches the configured list, the extension opens it in a hidden code window, executes each configured command in sequence, saves if modified, and closes the window. Already-open documents are formatted in-place and saved only if changed.

## Why Another Fork?

The original [FormatAllFiles](https://github.com/munyabe/FormatAllFiles) is no longer maintained. While [CodeMaid](https://www.codemaid.net) is the de facto solution for this workflow today, I wanted to understand what goes into a VSIX extension firsthand — package loading, command routing, the DTE automation model, options pages, and the VSIX manifest. Vibe coding the entire thing turned out to be a great way to learn.

## License

This project inherits the spirit of the original — consider it MIT-or-equivalent. See the [original repository](https://github.com/munyabe/FormatAllFiles) for reference.
