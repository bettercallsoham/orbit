# Orbit

**Make every move count.** Orbit is a native Windows file utility that puts conversions, tools, and file actions in a contextual radial wheel. Choose a file in Orbit or drag one in File Explorer, then pick an action without navigating through separate apps.

## Features

- Convert supported images, media, documents, and archives.
- Use focused tools for PDFs, images, media, compression, and file protection.
- Open the Convert, Tools, or Actions wheel while dragging a file in Explorer. The default keys are Shift, Control, and Alt; change them in Settings.
- Navigate the wheel with arrow keys, confirm with Enter, or cancel with Esc.
- See recent activity, progress, results, and actionable errors on Home.
- Configure output location, wheel behavior, sound, notifications, accessibility, and optional engines.

Orbit processes one dropped file at a time and tells you when a multi-file drop contains more. File availability and action choices depend on the selected format and installed tools.

## Requirements

- Windows 10 version 1809 or newer.
- The release executable bundles the .NET 10 runtime; users do not need to install .NET. Building from source requires the .NET 10 SDK.
- Optional: FFmpeg for many video, audio, GIF, WebP, AVIF, and JPEG XL operations; Microsoft Edge or another supported Chromium browser for HTML, SVG, and Markdown rendering; Microsoft Office or LibreOffice for some office-document operations. Some archive and image tools use 7-Zip, WinRAR, or Guetzli.

Missing optional tools affect only the actions that need them. Orbit reports the dependency in the relevant workflow. External tools are not silently installed.

## How to Download

Open the [latest Orbit release](https://github.com/bettercallsoham/orbit/releases/latest), download **Orbit-win-x64.exe**, and run it directly on Windows x64. No .NET installation is needed. The executable embeds the license and font notices; run `Orbit-win-x64.exe --notices` from a terminal to read them. **Orbit-Setup.exe** is the optional per-user installer; it installs under `%LOCALAPPDATA%\Programs\Orbit`, adds a Start Menu entry, and does not require administrator privileges.

## Build and run

From this directory in PowerShell:

```powershell
dotnet restore .\Orbit.csproj
dotnet build .\Orbit.csproj --no-restore
dotnet run --project .\Orbit.csproj
```

The development build creates `Orbit.exe` and companion files under `bin\Debug\net10.0-windows10.0.19041.0\`; it requires the .NET 10 Desktop Runtime and its build output must stay together. The downloadable release is a separate self-contained, single-file publish. Orbit runs portably; **Settings > System** offers an optional per-user installation and Start with Windows.

## Use Orbit

1. On Home, follow the three steps to open the wheel from Explorer or your editor. The title-bar preview uses a sample file and never runs an action.
2. In Explorer, drag a file and press the configured key for Convert, Tools, or Actions to open the wheel.
3. Select an action with the pointer or arrow keys and Enter. Press Esc to close the wheel.
4. Review progress and results in Recent Activity. Adjust output and shortcut preferences in Settings.

Update checks are manual under **Settings > System** and use [Orbit's GitHub releases](https://github.com/bettercallsoham/orbit/releases). Automatic replacement requires a compatible single-file release and a matching SHA-256 checksum file.

## Privacy and network use

Orbit keeps selected files, file paths, conversion results, settings, and notification history on your computer. Its own network code uses GET requests to fetch the public [announcement feed](docs/notifications.md) at startup and at most once every six hours, and to check GitHub releases when you select **Check for updates**. Installing an update downloads the release executable and checksum. These requests do not include file contents, filenames, paths, conversion history, or settings. There is no analytics or telemetry code.

Orbit opens a website in your default browser only when you choose a link, such as a notification action, the GitHub star link, an About link, a Google Drive shortcut, or a download page. Opening a Google Drive shortcut sends the URL or document ID stored in that shortcut to Google through your browser. Optional tool installation uses winget or downloads Guetzli when you request it. The headless browser used for HTML, Markdown, and SVG rendering is configured with an unavailable local proxy and blocked DNS resolution to prevent page-origin HTTP(S) requests. Other optional programs, including Microsoft Office, LibreOffice, and FFmpeg, process files locally, but Orbit cannot guarantee how those separate programs handle external references inside a document or media file. Opening an exported HTML file in your own browser may also load resources referenced by that file. Avoid those workflows with untrusted files when strict offline processing is required.

## Development

Run the built-in automated checks with the SDK:

```powershell
dotnet .\bin\Debug\net10.0-windows10.0.19041.0\Orbit.dll --test templates
dotnet .\bin\Debug\net10.0-windows10.0.19041.0\Orbit.dll --test archive
```

`--test` runs a broader suite and needs optional external tools. See [source layout](docs/STRUCTURE.md), [wheel plugins](docs/PLUGINS.md), and [contributing](docs/CONTRIBUTING.md).

## Release packaging

Set the release version in `Orbit.csproj`. In PowerShell, from this directory, run:

```powershell
dotnet publish .\Orbit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\release
Move-Item .\artifacts\release\Orbit.exe .\artifacts\release\Orbit-win-x64.exe -Force
```

The file to upload is `artifacts\release\Orbit-win-x64.exe`. It includes the .NET runtime, app resources, and required notices. Create a GitHub release tagged `v<version>` and upload that executable as a release asset. Orbit's in-app updater additionally requires a matching `Orbit-win-x64.exe.sha256` asset; generate it with:

```powershell
$hash = (Get-FileHash .\artifacts\release\Orbit-win-x64.exe -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  Orbit-win-x64.exe" | Set-Content .\artifacts\release\Orbit-win-x64.exe.sha256 -Encoding ascii
```

Upload the checksum file alongside the executable if you want automatic in-app updates. A development `dotnet build` is framework-dependent and must not be uploaded as the release executable. Test the published file on a clean Windows x64 machine without .NET 10 before claiming that scenario is verified.

To build the optional installer, install Inno Setup 6 and run this from the `Orbit` directory:

```powershell
winget install --id JRSoftware.InnoSetup --exact
.\installer\build-installer.ps1
```

This publishes a fresh self-contained single-file build and creates `artifacts\installer\Orbit-Setup.exe`. The installer includes the GPL license, copyright file, third-party notices, and Anton font notice. It leaves Orbit settings and templates in `%APPDATA%\Orbit` when upgraded or uninstalled.

## Project and license

Orbit is maintained by [Soham Kulkarni](https://bettercallsoham.vercel.app/) ([GitHub](https://github.com/bettercallsoham)). The code is distributed under [GPLv3](LICENSE); copyright information is in [COPYRIGHT](COPYRIGHT). The bundled Anton font retains its [SIL Open Font License](resources/Fonts/OFL.txt).


Made with ☕ and ❤️ by Soham.
