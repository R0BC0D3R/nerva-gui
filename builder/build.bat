@echo off

dotnet publish ^
/p:GenerateFullPaths=true /p:Configuration=Release /p:TrimUnusedDependencies=true ^
/p:OutputPath=%~dp0..\Bin\Release\win-x64\ %~dp0..\Src\Nerva.Toolkit\Nerva.Toolkit.Windows.csproj