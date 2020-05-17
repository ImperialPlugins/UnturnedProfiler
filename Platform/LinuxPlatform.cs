// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace ImperialPlugins.UnturnedProfiler.Platform
{
    internal sealed class LinuxPlatform : LibraryPlatform
    {
        public override IntPtr LoadLibrary(string filename)
        {
            var result = dlopen(filename, RTLD_NOW | RTLD_GLOBAL);
#if DEBUG
            if (result == IntPtr.Zero)
            {
                Console.WriteLine("Fail reason: " + dlerror());
            }
#endif
            return result;
        }

        public override bool FreeLibrary(IntPtr module)
        {
            return dlclose(module) == 0;
        }

        public override IntPtr GetProcAddress(IntPtr module, string method)
        {
            return dlsym(module, method);
        }

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        private static extern string dlerror();

        [DllImport("libdl.so")]
        private static extern int dlclose(IntPtr module);

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        public static extern int symlink(string file, string symlink);

        private const int RTLD_LAZY = 0x00001;
        private const int RTLD_NOW = 0x00002;
        private const int RTLD_GLOBAL = 0x00100;
    }
}