@echo off

dotnet publish /t:restore /t:build /p:GenerateFullPaths=true ^
/p:Configuration=Release /p:TrimUnusedDependencies=true ^
/p:Publish=true /p:RuntimeIdentifier=win-x64 ^
../Src/Nerva.Toolkit/Nerva.Toolkit.Windows.csproj