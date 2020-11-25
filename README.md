# Multithreaded file archiver
Simple multithreaded file archiver implemented using dotnet core framework.  
Most commands below are for Linux system but it is just because I have Ubuntu on my home machine. The commands can be easily tuned or even used as-is on Windows.

## Main things to understand and reproduce this code
To run the app  
`cd GzipArchiver`  
`dotnet run`  

To publish single executable file on Linux  
`dotnet publish -c release --self-contained --runtime linux-x64`  
or on Windows  
`dotnet publish -c release --self-contained --runtime win-x64`  

To run unit tests  
`cd GzipArchiver.Test`  
`dotnet test`  

To run E2E tests  
`sh e2e-test-suite.sh`  

To create project structure run  
`dotnet new console --name GzipArchiver`  
`dotnet new mstest --name GzipArchiver.Test`  
`cd GzipArchiver.Test`  
`dotnet add reference ./../GzipArchiver/GzipArchiver.csproj`  

## Some links
https://docs.microsoft.com/ru-ru/dotnet/api/system.io.compression.gzipstream?view=net-5.0
