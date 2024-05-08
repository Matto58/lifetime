#!/usr/bin/bash
v=$1
if [ -z $v ]
then
    read -p "version? " v
fi

arches=("x64" "arm64")
oses=("linux" "win")

for arch in ${arches[@]}
do
    for os in ${oses[@]}
    do
        dotnet publish -c Release --os $os -a $arch
        7z a -y lifetime-$v-$os-$arch.zip ./bin/Release/net8.0/$os-$arch/publish/*
    done
done
