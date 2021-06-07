# TODO: waiting for https://github.com/golang/go/issues/31105
#       to be useful
GOOS=js GOARCH=wasm go build -o test.wasm
