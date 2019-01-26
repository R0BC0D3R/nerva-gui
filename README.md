# nerva-unified-toolkit

Unified toolkit for NERVA, written in C#

## Prerequisites

To run the NERVA GUI, you need .NET 4.7/Mono 5.8 (minimum)  
To build the GUI from source, you additionally need the .NET Core SDK (All platforms) and the Microsoft Build Tools (Windows) installed

Mono/.NET

- Ubuntu 17.10

    `sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF`  
    `echo "deb http://download.mono-project.com/repo/ubuntu xenial/snapshots/5.8.0.127 main" | sudo tee /etc/apt/sources.list.d/mono.list`  
    `sudo apt update`  
    `sudo apt install mono-complete`

The latest Mono release for Ubuntu is broken. The above commands will install a slightly older, but tested working version of Mono  
For other Linux distributions, please consult the [Mono documentation](https://www.mono-project.com/download/stable/#download-lin) for information specific to your distro.

Windows 10 has .NET 4.7 pre-installed. For older versions of Windows, you can get [here](https://www.microsoft.com/net/download/dotnet-framework-runtime)

.NET Core SDK

Please refer to the [.NET Core website](https://www.microsoft.com/net) and follow the instructions specific to your OS.

Microsoft Build Tools (Windows Only)

The Microsoft Build Tools are available for download from the Microsoft [Visual Studio website](https://visualstudio.microsoft.com/downloads/). Look for the download link for *Build Tools for Visual Studio 2017*

When running the installer, select the following workloads

- .NET desktop build tools 
- .NET Core build tools
- Visual C++ build tools (Optional, for building the NERVA CLI from source if desired)

## Building

### CLI

Please refer to the document [Building the NERVA CLI tools](https://bitbucket.org/snippets/nerva-project/kejLB4)

### GUI

The build instructions are the same for Linux and Windows

- All systems
    
    `git clone https://bitbucket.org/nerva-project/nerva-unified-toolkit`  
    `./nerva-unified-toolkit/builder/build release-<os>`, where `<os>` is any one of `linux`, `windows` or `mac`  

## Development

Development is done entirely within VS Code using the C# extension. A cross platform GUI is made available via Eto.Forms.  
To set up your environment install the following in addition to the above described dependencies

- VS Code
- VS Code C# extension
- Eto.Forms templates  
`dotnet new -i "Eto.Forms.Templates::*"`

## Usage and more information

Please refer to the following documentation for information on the NERVA GUI

[NERVA GUI: Getting Started](https://bitbucket.org/snippets/nerva-project/7edAp6)  
[NERVA GUI: Essentials](https://bitbucket.org/snippets/nerva-project/yedn4X)  
[NERVA GUI: Known Issues](https://bitbucket.org/snippets/nerva-project/keGne4)

