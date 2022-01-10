# Project System Tools

_Project System Tools_ is a diagnostic extension for the C#, Visual Basic, and F# Project Systems in Visual Studio.

It can be downloaded from the Visual Studio Extension Marketplace:

- [Project System Tools for Visual Studio 2022](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools2022)
  [![Visual Studio Marketplace](https://vsmarketplacebadge.apphb.com/version/VisualStudioProductTeam.ProjectSystemTools2022.svg)](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools2022)
  [![Visual Studio Marketplace Downloads](https://vsmarketplacebadge.apphb.com/downloads-short/VisualStudioProductTeam.ProjectSystemTools2022.svg)](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools2022)
- [Project System Tools for Visual Studio 2017 and 2019](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools)
  [![Visual Studio Marketplace](https://vsmarketplacebadge.apphb.com/version/VisualStudioProductTeam.ProjectSystemTools.svg)](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools)
  [![Visual Studio Marketplace Downloads](https://vsmarketplacebadge.apphb.com/downloads-short/VisualStudioProductTeam.ProjectSystemTools.svg)](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools)

## Features

Once installed, some new items appear in the `View > Other Windows` menu:

<img src="img/view-menu.png" width="470">

Selecting `Build Logging` will show a new pane in Visual Studio:

<img src="img/build-logging-click-to-record.png" width="664">

Click the first toolbar icon to start recording both regular and design-time builds in the project system.

Once a build is recorded, it will appear as shown. Right-clicking the build item produces a context menu:

<img src="img/build-logging-context-menu.png" width="470">

From here you may:

- _Save Logs_ which prompts for a folder to save the `.binlog` file into
- _Open Logs_ which opens the log file inside Visual Studio
- _Open Logs External_ which opens the `.binlog` file outside of Visual Studio (we recommend https://msbuildlog.com)

The `Open Logs` option displays build results in a tree view:

<img src="img/open-log-view.png" width="470">

By opening the `Build Message List` pane (via the `View > Other Windows` menu, as above) you can see data about the selected tree node.

<img src="img/build-message-list.png" width="470">

## Getting higher-fidelity logs from VS

The build events this extension subscribes contain the most useful information for diagnosing problems, but do omit some data for performance reasons.

In cases where more information is needed in binlogs, you can configure MSBuild to automatically write diagnostic data to disk. To do this for all builds within Visual Studio:

1. Open a Developer Command Prompt for the version of Visual Studio you want to use
1. Set two environment variables as follows:
   ```
   set MSBuildDebugEngine=1
   set MSBUILDDEBUGPATH=c:\some\path
   ```
   You can use whatever path you like for `MSBUILDDEBUGPATH`, but it must be writeable by the current user.
1. Type `devenv` to start Visual Studio with this configuration
1. Open the `MSBUILDDEBUGPATH` path in Windows Explorer to see the captured binlog and other diagnostic files

For more information, see [this documentation section](https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Building-Testing-and-Debugging-on-Full-Framework-MSBuild.md#logs).

If you see:

> error : The type initializer for ‘Microsoft.Build.Shared.Debugging.DebugUtils’ threw an exception.

This indicates that the path set in `MSBUILDDEBUGPATH` is not writeable by Visual Studio. Close VS, set a new path and try again.

## Contributing

We welcome contributions and suggestions!

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see [Contributors Code of conduct](https://github.com/dotnet/home/blob/master/guidance/be-nice.md).
