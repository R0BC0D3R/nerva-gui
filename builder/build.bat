@echo off

dotnet build /t:restore /t:build /p:GenerateFullPaths=true ^
/p:Configuration=Release /p:TrimUnusedDependencies=true ^
../Src/Nerva.Toolkit/Nerva.Toolkit.Windows.csproj