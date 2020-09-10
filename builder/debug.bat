@echo off

dotnet build ^
/p:GenerateFullPaths=true /p:Configuration=Debug /p:TrimUnusedDependencies=true ^
/p:OutputPath=%~dp0..\Bin\Debug\win-x64\ %~dp0..\Src\Nerva.Toolkit\Nerva.Toolkit.Windows.csproj