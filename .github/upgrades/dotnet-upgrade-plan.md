# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade gg-file-organizer\gg-file-organizer.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

No projects are excluded from the upgrade.

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### gg-file-organizer\gg-file-organizer.csproj modifications

Project properties changes:
- Project file needs to be converted from legacy format to SDK-style format
- Target framework should be changed from `net48` to `net8.0-windows`

Feature upgrades:
- WPF application conversion from .NET Framework 4.8 to .NET 8.0
- The project uses `System.Drawing` for image metadata extraction which will need the `System.Drawing.Common` Windows compatibility package
- The project uses `System.Windows.Forms` for dialogs (FolderBrowserDialog, SaveFileDialog, MessageBox) which requires Windows Forms interop support

Other changes:
- UseWPF property must be set to true
- UseWindowsForms property must be set to true for Windows Forms interop
