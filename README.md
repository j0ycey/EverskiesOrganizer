# Everskies Studio Organizer

Everskies Studio Organizer is a free community tool for Everskies designers and creators. It helps you turn exported PNG layers into organized items, variations, tags, and upload-ready folders.

> This is an independent community project. It is not affiliated with, endorsed by, or officially connected to Everskies.

## Download and start

The easiest way to use the organizer is the Windows release:

1. Open the repository's **Releases** page.
2. Download the latest `EverskiesOrganizer-win-x64.zip`.
3. Extract the ZIP file.
4. Start `EverskiesOrganizer.exe`.

No Visual Studio, .NET SDK, or separate .NET installation is required for the published Windows version.

Windows may show a SmartScreen warning because the app is not signed by a commercial publisher. If you downloaded the release from this repository, choose **More info** and then **Run anyway**.

## What it does

- Import PNGs from a folder, including subfolders
- Create items from selected PNGs
- Assign PNGs to items by drag and drop
- Create and edit variations
- Assign `Minus`, `Plus`, or `All` assets
- Add Variation and color tags
- Choose an item preview
- Find possible matching PNG groups with Suggestions
- Save and reopen organizer projects
- Validate projects before export
- Export original PNGs without changing their size or contents

## Basic workflow

1. Import the folder that contains your exported PNGs.
2. Select PNGs and create an item, or drag a PNG onto an existing item.
3. Give the item a name and category.
4. Add variations manually.
5. Drag PNGs onto a variation or directly onto its `Minus`, `Plus`, or `All` slot.
6. Select a category and choose ist color tags.
7. Set the item preview from the item's asset list.
8. Use **Validate & Export** when the item is ready.

`Minus + Plus` and `All` are mutually exclusive for one variation. A variation must use one arrangement or the other.

## Projects and exports

Use **Save** to create an `.eskproj` project file and **Open** to reopen it later. Project files store the organization data and the original PNG paths. They do not copy the PNGs into the project file.

If you move the source PNGs to another folder or computer, the project may no longer be able to find them. Keep the project and source PNG folder together, or import the PNGs again on the new computer.

**Validate & Export** asks for a target folder and creates:

```text
target folder/
└── tagged assets/
    ├── item-preview.png
    ├── item-name/
    │   ├── category_...-body_minus-...png
    │   └── category_...-body_plus-...png
    └── ...
```

The exported PNGs are copied from the original files. The organizer only creates cropped thumbnails for the interface.

## Troubleshooting

### Windows blocks the EXE

The release is a self-contained Windows application, but it is not code-signed. In the SmartScreen dialog, select **More info** and **Run anyway** if you trust the downloaded release.

### The app cannot find my PNGs

Projects keep the original file paths. Do not move or rename the source PNG folder after saving a project. If the files were moved, import the new folder again.

### A project does not appear in the Open dialog

Choose the `Everskies Organizer project (*.eskproj)` filter, or choose `All files (*.*)`. Make sure you are opening the actual `.eskproj` file, not only an entry from Windows Explorer's “Recent” list.

### Export validation reports errors

Open the reported item and check that it has:

- a name
- a category
- a preview
- at least one non-empty variation
- at least one tag per variation
- either `Minus + Plus` or `All`, but not both

### Suggestions show too many or too few matches

Suggestions are based on visible PNG shape and color heuristics. They are only a starting point. Remove individual assets from the Suggestions window and review the remaining groups before accepting them.

### The interface looks wrong after an update

Close the app completely and start the newly downloaded release again. Do not run an old EXE from an earlier extracted ZIP folder by accident.

## Feedback and bug reports

Please use [GitHub Issues](../../issues) for bugs and feature requests. Include:

- what you were trying to do
- the steps that caused the problem
- what you expected to happen
- what happened instead
- screenshots or a small example when possible

Do not upload private PNGs or project files unless you are comfortable sharing them publicly.

## For developers

The project is a .NET 8 WPF desktop application for Windows. It uses WPF and Windows Forms dialogs and has no third-party runtime packages.

To run it from source, install the .NET 8 SDK on Windows and run:

```powershell
dotnet run
```

To create the self-contained Windows executable locally:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

The output is written to `publish\win-x64\EverskiesOrganizer.exe`.

The GitHub Actions workflow can also build the Windows ZIP automatically when a version tag such as `v1.0.0` is pushed.
