csc dotnet-webassembly/WebAssembly/*.cs dotnet-webassembly/WebAssembly/Runtime/*.cs dotnet-webassembly/WebAssembly/Runtime/Compilation/*.cs dotnet-webassembly/WebAssembly/Instructions/*.cs wasiwasm.cs -langversion:9.0 -unsafe -nowarn:8632

mkbundle --simple -z --static --library /usr/lib64/libmono-native.so -L /usr/lib/mono/4.5 wasiwasm.exe -o wasiwasm

