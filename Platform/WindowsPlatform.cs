// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Rocket.Core.Logging;

namespace ImperialPlugins.UnturnedProfiler.Platform
{
    internal sealed class WindowsPlatform : LibraryPlatform
    {
        public override bool FreeLibrary(IntPtr module)
        {
            return NativeMethods.FreeLibrary(module);
        }

        public override IntPtr GetProcAddress(IntPtr module, string method)
        {
            return NativeMethods.GetProcAddress(module, method);
        }

        public override IntPtr LoadLibrary(string lpFileName)
        {
            var result = NativeMethods.LoadLibraryEx(lpFileName, 0, NativeMethods.LoadLibraryFlags.NoFlags);
            if (result == IntPtr.Zero)
            {
                Logger.LogError(new Win32Exception().Message);
            }
            return result;
        }

        internal static class NativeMethods
        {
            private const string Kernel32LibraryName = "kernel32.dll";

            [DllImport(Kernel32LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);

            [DllImport(Kernel32LibraryName, SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibraryEx(string fileName, int hFile, LoadLibraryFlags dwFlags);

            [Flags]
            public enum LoadLibraryFlags : uint
            {
                NoFlags = 0x00000000,
                DontResolveDllReferences = 0x00000001,
                LoadIgnoreCodeAuthzLevel = 0x00000010,
                LoadLibraryAsDatafile = 0x00000002,
                LoadLibraryAsDatafileExclusive = 0x00000040,
                LoadLibraryAsImageResource = 0x00000020,
                LoadWithAlteredSearchPath = 0x00000008
            }

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
        }
    }
}