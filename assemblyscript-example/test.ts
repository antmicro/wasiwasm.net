import "wasi";

import { Console, Environ } from "as-wasi";

Console.log("Hello world!");

let env = new Environ();
Console.log(env.get("HOME")!);

