#!/usr/bin/bash
# todo clean this up a bit
s=$1
if [ -z $s ]
then
    read -p "version? " s
fi

dotnet publish -c Release --os linux -a arm64
dotnet publish -c Release --os linux -a x64
dotnet publish -c Release --os win -a arm64
dotnet publish -c Release --os win -a x64

7z a -y lifetime-$s-linux-arm64.zip ./bin/Release/net8.0/linux-arm64/publish/*
7z a -y lifetime-$s-linux-x64.zip ./bin/Release/net8.0/linux-x64/publish/*
7z a -y lifetime-$s-win-arm64.zip ./bin/Release/net8.0/win-arm64/publish/*
7z a -y lifetime-$s-win-x64.zip ./bin/Release/net8.0/win-x64/publish/*
