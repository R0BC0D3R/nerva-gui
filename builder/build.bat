@echo off

dotnet publish -f net5.0 -r win-x64 ^
/p:Publish=true /p:GenerateFullPaths=true /p:Configuration=Release /p:TrimUnusedDependencies=true ^
/p:OutputPath=%~dp0..\Bin\Release\win-x64\ %~dp0..\Src\Nerva.Toolkit\Nerva.Toolkit.Windows.csproj