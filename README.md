# BlueLabel

This application will help you sort your files by labeling each of them.

This application depends on `libvlc` package on Linux.

## Features

- Preview content of the file
    - Audio files (mp3, wav, ogg, etc.)
    - Video files (mp4, avi, wmv, mkv etc.)
    - Text files (txt, html, xml, js, md etc.)
    - Image files (png, bmp, ico, jpg, gif)
    - Archive files /zip, gz, bz2, tar, tgz, tar.gz, tar.bz2)
- View File Information
- Multiple labels
- Options to either sort them to folders or rename them.
- Automatically sort files
    - Sort by Duration (Audio and Video only)
    - Sort by File Type or Extension
    - Sort by Picture Size (Image and Video only)
    - Sort by File Size
- Copy or Move
- Filters

## Build

Requires .NET SDK 8 or newer.

If using another .NET version, please change the "TargetFramework" versions of each .csproj file accordingly. We prefer
using the latest LTS version of .NET.

To build, head to the folder of any trget system (ex. BlueLabel.Linux) and run "dotnet publish" to build it (ex. dotnet
build -r linux-x64 -c Release). The application should be inside the bin folder of that folder.

***NOTE: PLEASE DO NOT SKIP ANY STEPS UNLESS IT IS TOLD TO OK TO SKIP IT.***

1. Install [.NET 8 SDK](https://dotnet.microsoft.com) to your system. This will also should install .NET 8 runtime.
2. Clone the repository, use the green "Code" arrow to get the code.
3. Either open the "BlueLabel.sln" file in an IDE that supports .NET solution files (Visual Studio -any edition- or
   JetBrains Rider) or navigate to the target folder (ex. BlueLabel.Windows)
4. Open a terminal application and run `dotnet build`.
    - On Windows, hold SHift and Right-click on the folder and press either the Windows Terminal, Powershell or Command
      Prompt option
    - Most file managers on Linux do already support opening a terminal in right-click without holding any other key.
    - You can open up the terminal application directly and navigate to the target folder using `cd` command.
5. The build files should be in `bin` folder.

If you want a production version of BlueLabel instead, use `dotnet publish -c Release" instead.
