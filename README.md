## Nerva.Desktop

Nerva Desktop application mining and wallet management, written in C#

If you're looking to install the latest version of the software, download it from [Nerva Desktop Releases][nerva-desktop-releases]

If you'd like to be able to build Nerva.Desktop yourself, see below notes.

### Install Dotnet

Below needs to be done only once.  Once you have .NET Core installed, you can just pull latest changes and build.

`sudo apt-get install dotnet-sdk-5.0`

### Linux/Ubuntu notes

If you get error that above dotnet-sdk package cannot be found, you might need to run this:
 
`sudo wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`

`sudo dpkg -i packages-microsoft-prod.deb`

`sudo add-apt-repository universe`

`sudo apt-get update`

`sudo apt-get install apt-transport-https`

`sudo apt-get update`

`sudo apt-get install dotnet-sdk-5.0`  

### Clone the repository and build Nerva.Desktop

`sudo git clone --recursive https://github.com/nerva-project/nerva-gui.git`

`cd nerva-gui/builder`

#### Linux

`sudo ./build "linux-x64"`

#### MacOS

`./build "osx-x64"`

#### Windows

First, you'll need something that will allow you to run bash commands. It can be MINGW64, Cygwin or something else.  Once you have your environment set up, run:

`./build "win-x64"`



<!-- Reference links -->
[nerva-desktop-releases]: https://github.com/nerva-project/nerva-gui/releases