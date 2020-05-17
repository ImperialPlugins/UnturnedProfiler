// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ImperialPlugins.UnturnedProfiler.Platform
{
    /// <summary>
    /// A set of helper functions that are consistently implemented across platforms.
    /// </summary>
    public abstract class LibraryPlatform
    {
        public abstract IntPtr LoadLibrary(string lpFileName);
        public abstract bool FreeLibrary(IntPtr module);

        public abstract IntPtr GetProcAddress(IntPtr module, string method);
    }
}