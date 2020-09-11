# nerva-unified-toolkit

Unified toolkit for NERVA mining and wallet management, written in C#

## Prerequisites

Each binary package uses the .NET core `PublishSingleFile` option to pack the application, dependencies and .NET core runtime into a single executable.  
Therefore, no dependencies are required to run the provided binary packages

Building from source requires the [.NET Core SDK 3.0+](https://www.microsoft.com/net)

## Building

Clone the repo and enter the builder directory

`git clone --recursive https://bitbucket.org/nerva-project/nerva.gui`  
`cd ./nerva-gui/builder`

Execute the appropriate build  

Linux: `./build.unix linux`  
Mac: `./build.unix mac`  
Windows: `./build.bat`  
