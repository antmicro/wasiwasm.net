use std::env;
use std::io::{self, Write};

fn main() {
        io::stdout().write_all(b"hello world\n");
        eprintln!("Error print");
        eprintln!("Error print");

        for argument in env::args() {
            println!("one is:\n");
            println!("{}", argument);
        }

        return;
}

#[no_mangle]
fn test() {
    io::stdout().write_all(b"hello world from test\n");
    main();
}

