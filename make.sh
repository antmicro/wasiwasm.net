git submodule init
git submodule update

csc dotnet-webassembly/WebAssembly/*.cs dotnet-webassembly/WebAssembly/Runtime/*.cs dotnet-webassembly/WebAssembly/Runtime/Compilation/*.cs dotnet-webassembly/WebAssembly/Instructions/*.cs wasiwasm.cs -langversion:9.0 -unsafe -nowarn:8632

if [ -e /usr/lib64/libmono-native.so ] ; then
  NATIVE="/usr/lib64/libmono-native.so"
else
  NATIVE="/usr/lib/libmono-native.so"
fi

mkbundle --simple -z --static --library $NATIVE -L /usr/lib/mono/4.5 wasiwasm.exe -o wasiwasm

