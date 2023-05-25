#!/bin/bash

filename_default="words.txt"
echo Enter name for generated random words file [$filename_default]: 
read filename
filename="${file_name:-$filename_default}"

filesize_default=1024
echo Enter the size of file in Megabytes [$filesize_default]:
read filesize
filesize="${filesize:-$filesize_default}"

dotnet build --configuration Release

mkdir Artifacts
cd Artifacts

cp -R ../DataGenerator/bin/Release/net7.0 ./DataGenerator
cp -R ../Application/bin/Release/net7.0 ./Application
cp -R ../Source ./Source

./DataGenerator/DataGenerator $filename $filesize
./Application/Application $filename
