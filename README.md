# nerva-unified-toolkit

Unified toolkit for NERVA, written in C#

## Prerequisites

Running the pre-compiled binaries:   

Windows: [.NET Framework 4.8+ Runtime](https://dotnet.microsoft.com/download/dotnet-framework/net48)
Mac: [Mono 6+](https://www.mono-project.com/download/stable/)
Linux: None

Building from source:  

Windows: [.NET Framework 4.8+ Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48) and [Build Tools for Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)  
Mac: [Mono 6+](https://www.mono-project.com/download/stable/)  
Linux: [Mono 6+](https://www.mono-project.com/download/stable/) or [.NET Core 3.0+](https://www.microsoft.com/net)

Linux builds have the option of being built against Mono or .NET core, so only one is required.  
The current binaries are built against Mono 6.8.0. Older versions should be supported, but YMMV.

## Building

Clone the repo and enter the builder directory

`git clone --recursive https://bitbucket.org/nerva-project/nerva.gui`  
`cd ./nerva-gui/builder`

Execute the appropriate build  

Windows: `./build.bat`  
Mac: `./build.mac`  
Linux: `./build.linux <option>`  
The linux build has the following options:
- `release`: Builds an unpackaged binary without debugging information against .NET Core  
- `publish`: Builds an packaged single executable binary against .NET Core  
- `mono`: Builds and packaged binary without debugging information against the Mono framework
