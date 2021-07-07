//
// C#/Mono wasi wasm runner
//
// (c) 2020-2021 Antmicro <www.antmicro.com>
//
// License: Apache 2.0
//

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

using WebAssembly;
using WebAssembly.Instructions;
using WebAssembly.Runtime;

using System.Runtime.InteropServices;

using System.Diagnostics;

static class Program
{
    public static bool debug = false;

    public static void dbgmsg(string msg) {
        if (!debug) return;
        Console.Write(">>>> ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("{0}", new StackFrame(1).GetMethod().ToString().Replace("Int32", "i32").Replace("Int64", "i64").Replace("Void", "void"));
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(" ({0})", msg);
        Console.ResetColor();
    }

    public static int fd_seek(int a, long b, int c, int d) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_close(int a) { dbgmsg("UNIMPLEMENTED"); return 1; }

    public static int fd_fdstat_get(int fd, int addr) {
        dbgmsg(string.Format("{0}, 0x{1:X}", fd, addr));
        if (fd <= 2) { // stdin,out,err
            Marshal.WriteInt64(memory.Start + addr, 2); // type = char dev
            Marshal.WriteInt64(memory.Start + addr + 8, 0); // flags
            Marshal.WriteInt64(memory.Start + addr + 16, 0); // rights
            Marshal.WriteInt64(memory.Start + addr + 24, 0); // rights inheriting
        }
        return 0;
    }

    public static int fd_write(int fd, int iovs_addr, int iovs_len, int nwritten_addr) { 
        dbgmsg(string.Format("{0}, 0x{1:X}, {2}, 0x{3:X}", fd, iovs_addr, iovs_len, nwritten_addr));
        string toDisplay = "";
        for (int i = 0; i < iovs_len; i++) {
            var addr = Marshal.ReadInt32(memory.Start + iovs_addr + (i*2)*4);
            var len = Marshal.ReadInt32(memory.Start + iovs_addr + ((i*2)+1)*4);
            if (len > 0) {
                toDisplay = toDisplay + Marshal.PtrToStringAuto(memory.Start + addr, len);
            }
        }
        Marshal.WriteInt32(memory.Start + nwritten_addr, toDisplay.Length);
        if (debug) {
            Console.WriteLine(".... '{0}'", toDisplay.Replace("\n", "\n.... "));
        } else {
            Console.Write(toDisplay);
        }
        return 0;
    }

    public static int args_get(int argv_addr, int argv_buf_addr) {
        dbgmsg(string.Format("0x{0:X}, 0x{1:X}", argv_addr, argv_buf_addr));
        int step = 0;
        for (int i = 0; i < argv.Count; i++) {
            var val = Encoding.ASCII.GetBytes(string.Format("{0}\0",argv[i]));
            Marshal.WriteInt32(memory.Start + argv_addr + (i * 4), argv_buf_addr + step);
            Marshal.Copy(val, 0, memory.Start + argv_buf_addr + step, val.Length);
            step += val.Length;
        }
        return 0;
    }

    public static int args_sizes_get(int argc_addr, int argv_buf_size_addr) {
        dbgmsg(string.Format("0x{0:X}, 0x{1:X}", argc_addr, argv_buf_size_addr));
        Marshal.WriteInt32(memory.Start + argc_addr, argv.Count);
        int len = 0;
        for (int i = 0; i < argv.Count; i++) len += Encoding.ASCII.GetBytes(string.Format("{0}\0", argv[i])).Length;
        Marshal.WriteInt32(memory.Start + argv_buf_size_addr, len);
        return 0;
    }
    
    public static int environ_get(int environ_addr, int environ_buf_addr) {
        // TODO
        dbgmsg(string.Format("0x{0:X}, 0x{1:X}", environ_addr, environ_buf_addr));
        return 0;
    }

    public static int environ_sizes_get(int environ_count_addr, int environ_buf_size_addr) {
        dbgmsg(string.Format("0x{0:X}, 0x{1:X}", environ_count_addr, environ_buf_size_addr));
        Marshal.WriteInt32(memory.Start + environ_count_addr, 0);
        Marshal.WriteInt32(memory.Start + environ_buf_size_addr, 0);
        return 0;
    }

    public static int clock_res_get(int a, int b) { dbgmsg("UNIMPLEMENTED"); return 1; }

    public static int clock_time_get(int clock_id, long precision, int result_addr) {
        dbgmsg(string.Format("{0}, {1}, 0x{2:X}", clock_id, precision, result_addr));
        Marshal.WriteInt64(memory.Start + result_addr, (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds) * 1000 * 1000);
        return 0;
    }

    public static int fd_advice(int a, long b, long c, int d) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_allocate(int a, long b, long c) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_datasync(int a) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_fdstat_set_flags(int a, int b) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_fdstat_set_rights(int a, long b, long c) { dbgmsg("UNIMPLEMENTED"); return 1; }
    
    public static int fd_filestat_get(int fd, int result_addr) {
        dbgmsg(string.Format("{0}, 0x{1:X}", fd, result_addr));
        return 8; // EBADF
    }
    
    public static int fd_filestat_set_size(int a, long b) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_filestat_set_times(int a, long b, long c, int d) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_pread(int a, int b, int c, long d, int e) { dbgmsg("UNIMPLEMENTED"); return 1; }

    public static int fd_prestat_get(int fd, int addr) { 
        dbgmsg(string.Format("fd={0}, addr=0x{1:X}", fd, addr));
        if (fd == 3) {
            Marshal.WriteInt64(memory.Start + addr, 0); // WASI_PREOPENTYPE_DIR
            Marshal.WriteInt64(memory.Start + addr + 8, 1);
            return 0;
        }
        return 8; // EBADF
    }

    public static int fd_prestat_dir_name(int fd, int path_addr, int len) { 
        dbgmsg(string.Format("fd={0}, path_addr=0x{1:X}, len={2}", fd, path_addr, len));
        if (fd == 3) {
            Marshal.WriteByte(memory.Start + path_addr, (byte)'.');
            return 0;
        }
        return 1; 
    }

    public static int fd_pwrite(int a, int b, int c, long d, int e) { dbgmsg("UNIMPLEMENTED"); return 1; }

    public static int fd_read(int fd, int iovs_addr, int iovs_len, int nread_addr) {
        dbgmsg(string.Format("{0}, 0x{1:X}, {2}, 0x{3:X}", fd, iovs_addr, iovs_len, nread_addr));
        if ((fd > 0) && (fd < 100)) {
            // stdout, stderr or unknown id
            return 1;
        } else {
            Stream file;
            if (fd >= 100) {
                if ((fd - 100) >= FileDescriptors.Count) {
                    // no such descriptor
                    return 1;
                }
                if (FileDescriptors[fd - 100] == null) {
                    // closed?
                    return 1;
                }
                file = FileDescriptors[fd - 100];
            } else if (fd == 0) {
                file = Console.OpenStandardInput();
            } else return 1;
            int nread = 0;
            for (int i = 0; i < iovs_len; i++) {
                var addr = Marshal.ReadInt32(memory.Start + iovs_addr + (i*2)*4);
                var len = Marshal.ReadInt32(memory.Start + iovs_addr + (i*2+1)*4);
                if ((i+1) == iovs_len) if (len == 1024) len = 1;
                byte[] c = new byte[len];
                int reallen = file.Read(c, 0, len);
                for (int j = 0; j < reallen; j++) Marshal.WriteByte(memory.Start + addr + j, c[j]);
                nread += reallen;
            }
            Marshal.WriteInt32(memory.Start + nread_addr, nread);
            return 0;
        }
    }

    public static int fd_readdir(int a, int b, int c, long d, int e ) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_renumber(int a, int b) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_sync(int a) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int fd_tell(int a, int b) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_create_directory(int a, int b, int c) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_filestat_get(int a, int b, int c, int d, int e) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_filestat_set_times(int a, int b, int c, int d, long e, long f, int g) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_link(int a, int b, int c, int d, int e, int f, int g) { dbgmsg("UNIMPLEMENTED"); return 1; }
    
    public static int path_open(int dir_fd, int dirflags, int path_addr, int path_len, int oflags, long fs_rights_base, long fs_rights_inherit, int fs_flags, int fd_addr) {
        dbgmsg(string.Format("{0}, {1}, 0x{2:X}, {3}, {4}, {5}, 0x{6:X}, 0x{7:X}, 0x{8:X}", dir_fd, dirflags, path_addr, path_len, oflags, fs_rights_base, fs_rights_inherit, fs_flags, fd_addr));
        var path = Marshal.PtrToStringAuto(memory.Start + path_addr, path_len);
        if (File.Exists(path)) {
          FileStream file = File.Open(path, FileMode.Open);
          FileDescriptors.Add(file);
          Marshal.WriteInt64(memory.Start + fd_addr, 100 + FileDescriptors.Count - 1);
          return 0;
        } else {
          Console.WriteLine("Path {0} does not exist", path);
          Marshal.WriteInt64(memory.Start + fd_addr, 0);
          return 1;
        }
    }

    public static int path_readlink(int a, int b, int c, int d, int e, int f) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_remove_directory(int a, int b, int c) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_rename(int a, int b, int c, int d, int e, int f) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_symlink(int a, int b, int c, int d, int e) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int path_unlink_file(int a, int b, int c) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int poll_oneoff(int a, int b, int c, int d) { dbgmsg("UNIMPLEMENTED"); return 1; }
    
    public static void proc_exit(int exit_code) {
        dbgmsg(string.Format("{0}", exit_code));
        Environment.Exit(exit_code);
    }
    
    public static int proc_raise(int a) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int sched_yield() { dbgmsg("UNIMPLEMENTED"); return 1; }

    static Random rndgen = new Random();
    public static int random_get(int buf_addr, int buf_len) {
        dbgmsg(string.Format("0x{0:X}, {1}", buf_addr, buf_len));
        var buf = new byte[buf_len];
        rndgen.NextBytes(buf);
        Marshal.Copy(buf, 0, memory.Start + buf_addr, buf.Length);
        return 0;
    }

    public static int sock_recv(int a, int b, int c, int d, int e, int f) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int sock_send(int a, int b, int c, int d, int e) { dbgmsg("UNIMPLEMENTED"); return 1; }
    public static int sock_shutdown(int a, int b) { dbgmsg("UNIMPLEMENTED"); return 1; }

    public static void dummy() { }

    static List<FileStream> FileDescriptors;

    static int Main(string[] args)
    {
        FileDescriptors = new List<FileStream>();

        var imports = new ImportDictionary
        {
            { "wasi_snapshot_preview1", "args_get", new FunctionImport(new Func<int, int, int>(args_get)) },
            { "wasi_snapshot_preview1", "args_sizes_get", new FunctionImport(new Func<int, int, int>(args_sizes_get)) },
            { "wasi_snapshot_preview1", "environ_get", new FunctionImport(new Func<int, int, int>(environ_get)) },
            { "wasi_snapshot_preview1", "environ_sizes_get", new FunctionImport(new Func<int, int, int>(environ_sizes_get)) },
            { "wasi_snapshot_preview1", "clock_res_get", new FunctionImport(new Func<int, int, int>(clock_res_get)) },
            { "wasi_snapshot_preview1", "clock_time_get", new FunctionImport(new Func<int, long, int, int>(clock_time_get)) },
            { "wasi_snapshot_preview1", "fd_advise", new FunctionImport(new Func<int, long, long, int, int>(fd_advice)) },
            { "wasi_snapshot_preview1", "fd_allocate", new FunctionImport(new Func<int, long, long, int>(fd_allocate)) },
            { "wasi_snapshot_preview1", "fd_close", new FunctionImport(new Func<int, int>(fd_close)) },
            { "wasi_snapshot_preview1", "fd_datasync", new FunctionImport(new Func<int, int>(fd_datasync)) },
            { "wasi_snapshot_preview1", "fd_fdstat_get", new FunctionImport(new Func<int, int, int>(fd_fdstat_get)) },
            { "wasi_snapshot_preview1", "fd_fdstat_set_flags", new FunctionImport(new Func<int, int, int>(fd_fdstat_set_flags)) },
            { "wasi_snapshot_preview1", "fd_fdstat_set_rights", new FunctionImport(new Func<int, long, long, int>(fd_fdstat_set_rights)) },
            { "wasi_snapshot_preview1", "fd_filestat_get", new FunctionImport(new Func<int, int, int>(fd_filestat_get)) },
            { "wasi_snapshot_preview1", "fd_filestat_set_size", new FunctionImport(new Func<int, long, int>(fd_filestat_set_size)) },
            { "wasi_snapshot_preview1", "fd_filestat_set_times", new FunctionImport(new Func<int, long, long, int, int>(fd_filestat_set_times)) },
            { "wasi_snapshot_preview1", "fd_pread", new FunctionImport(new Func<int, int, int, long, int, int>(fd_pread)) },
            { "wasi_snapshot_preview1", "fd_prestat_get", new FunctionImport(new Func<int, int, int>(fd_prestat_get)) },
            { "wasi_snapshot_preview1", "fd_prestat_dir_name", new FunctionImport(new Func<int, int, int, int>(fd_prestat_dir_name)) },
            { "wasi_snapshot_preview1", "fd_pwrite", new FunctionImport(new Func<int, int, int, long, int, int>(fd_pwrite)) },
            { "wasi_snapshot_preview1", "fd_read", new FunctionImport(new Func<int, int, int, int, int>(fd_read)) },
            { "wasi_snapshot_preview1", "fd_readdir", new FunctionImport(new Func<int, int, int, long, int, int>(fd_readdir)) },
            { "wasi_snapshot_preview1", "fd_renumber", new FunctionImport(new Func<int, int, int>(fd_renumber)) },
            { "wasi_snapshot_preview1", "fd_seek", new FunctionImport(new Func<int, long, int, int, int>(fd_seek)) },
            { "wasi_snapshot_preview1", "fd_sync", new FunctionImport(new Func<int, int>(fd_sync)) },
            { "wasi_snapshot_preview1", "fd_tell", new FunctionImport(new Func<int, int, int>(fd_tell)) },
            { "wasi_snapshot_preview1", "fd_write", new FunctionImport(new Func<int, int, int, int, int>(fd_write)) },
            { "wasi_snapshot_preview1", "path_create_directory", new FunctionImport(new Func<int, int, int, int>(path_create_directory)) },
            { "wasi_snapshot_preview1", "path_filestat_get", new FunctionImport(new Func<int, int, int, int, int, int>(path_filestat_get)) },
            { "wasi_snapshot_preview1", "path_filestat_set_times", new FunctionImport(new Func<int, int, int, int, long, long, int, int>(path_filestat_set_times)) },
            { "wasi_snapshot_preview1", "path_link", new FunctionImport(new Func<int, int, int, int, int, int, int, int>(path_link)) },
            { "wasi_snapshot_preview1", "path_open", new FunctionImport(new Func<int, int, int, int, int, long, long, int, int, int>(path_open)) },
            { "wasi_snapshot_preview1", "path_readlink", new FunctionImport(new Func<int, int, int, int, int, int, int>(path_readlink)) },
            { "wasi_snapshot_preview1", "path_remove_directory", new FunctionImport(new Func<int, int, int, int>(path_remove_directory)) },
            { "wasi_snapshot_preview1", "path_rename", new FunctionImport(new Func<int, int, int, int, int, int, int>(path_rename)) },
            { "wasi_snapshot_preview1", "path_symlink", new FunctionImport(new Func<int, int, int, int, int, int>(path_symlink)) },
            { "wasi_snapshot_preview1", "path_unlink_file", new FunctionImport(new Func<int, int, int, int>(path_unlink_file)) },
            { "wasi_snapshot_preview1", "poll_oneoff", new FunctionImport(new Func<int, int, int, int, int>(poll_oneoff)) },
            { "wasi_snapshot_preview1", "proc_exit", new FunctionImport(new Action<int>(proc_exit)) },
            { "wasi_snapshot_preview1", "proc_raise", new FunctionImport(new Func<int, int>(proc_raise)) },
            { "wasi_snapshot_preview1", "sched_yield", new FunctionImport(new Func<int>(sched_yield)) }, 
            { "wasi_snapshot_preview1", "random_get", new FunctionImport(new Func<int, int, int>(random_get)) },
            { "wasi_snapshot_preview1", "sock_recv", new FunctionImport(new Func<int, int, int, int, int, int, int>(sock_recv)) },
            { "wasi_snapshot_preview1", "sock_send", new FunctionImport(new Func<int, int, int, int, int, int>(sock_send)) },
            { "wasi_snapshot_preview1", "sock_shutdown", new FunctionImport(new Func<int, int, int>(sock_shutdown)) },

            { "wasi_unstable", "__dummy__", new FunctionImport(new Action(dummy)) }, // TODO
        };

        // copy wasi_snapshot_preview1 -> wasi_unstable
        foreach (var item in imports["wasi_snapshot_preview1"]) {
            imports["wasi_unstable"].Add(item);
        }

        if (args.Length == 0) {
            Console.WriteLine("Need at least one argument");
            return 1;
        }

        argv = new List<string>();
        for (int i = 1; i < args.Length; i++) {
            argv.Add(args[i]);
        }

        var compiled = Compile.FromBinary<dynamic>(new FileStream(args[0], FileMode.Open, FileAccess.Read))(imports);

        if (debug) foreach (var nm in RuntimeImport.FromCompiledExports(compiled.Exports)) {
            Console.WriteLine(string.Format("---- Export {0}", nm.ToString().Split("(")[1].Split(",")[0]));
        }
        if (debug) {
            Console.Write(">>>> ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Going to execute ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(args[0]);
            Console.ResetColor();
        }

        memory = compiled.Exports.memory;
        //compiled.Exports.__wasm_call_ctors();
        try {
            compiled.Exports._start();
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
            return 1;
        }
        return 0;
    }

    static UnmanagedMemory memory;
    static List<string> argv;
}
