@echo off
echo Building Database PDF Generator...

REM Restore NuGet packages
echo Restoring NuGet packages...
dotnet restore

REM Build the application
echo Building application...
dotnet build --configuration Release

REM Publish as self-contained executable
echo Publishing self-contained executable...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish

echo Build completed successfully!
echo Executable location: ./publish/DatabasePDFGenerator.exe
echo.
echo To run the application:
echo 1. Update config.json with your database credentials
echo 2. Run: ./publish/DatabasePDFGenerator.exe
pause