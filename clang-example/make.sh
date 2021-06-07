git clone https://github.com/WebAssembly/wasi-libc.git
cd wasi-libc ; make ; cd ..
clang --target=wasm32 -L. -nostdlib -Wl,--export-all -D__wasi__ -Wl,--allow-undefined -o test.wasm -I./wasi-libc/sysroot/include test.c wasi-libc/sysroot/lib/wasm32-wasi/libc.a wasi-libc/sysroot/lib/wasm32-wasi/crt1.o ./libclang_rt.builtins-wasm32.a

