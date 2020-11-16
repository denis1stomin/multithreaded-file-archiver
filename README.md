# Multithreaded file archiver
Simple multithreaded file archiver implemented using dotnet framework.

## Main things to understand or reproduce this code
To run the app  
`cd GzipArchiver`  
`dotnet run`  

To run tests  
`cd GzipArchiver.Test`  
`dotnet test`  

To create project structure run  
`dotnet new console --name GzipArchiver`  
`dotnet new mstest --name GzipArchiver.Test`  
`cd GzipArchiver.Test`  
`dotnet add reference ./../GzipArchiver/GzipArchiver.csproj`  

## Some links
https://docs.microsoft.com/ru-ru/dotnet/api/system.io.compression.gzipstream?view=net-5.0
