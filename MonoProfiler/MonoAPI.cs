#region Copyright
/*
 *  Unturned Profiler - A plugin for profiling Unturned servers and analyzing lag causes
 *  Copyright (C) 2017-2019 Enes Sadık Özbek <esozbek.me>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using ImperialPlugins.UnturnedProfiler.Platform;
using UnityEngine;

namespace ImperialPlugins.UnturnedProfiler.MonoProfiler
{
    public static class MonoAPI
    {
        private static bool s_Initialized;
        private static IntPtr s_MonoHandle;
        private static LibraryPlatform s_Platform;

        public static void Initialize(LibraryPlatform platform)
        {
            if (s_Initialized)
            {
                return;
            }

            s_Platform = platform;
            var unturnedDirectory = Directory.GetParent(Application.dataPath);

            string monoLibraryName = PlatformHelper.IsLinux ? "mono-2.0-bdwgc.so" : "mono-2.0-bdwgc.dll";
            var monoPath = Path.Combine(unturnedDirectory.FullName, "MonoBleedingEdge", "EmbedRuntime", monoLibraryName);
            s_MonoHandle = s_Platform.LoadLibrary(monoPath);

            if (s_MonoHandle == IntPtr.Zero)
            {
                throw new Exception($"Failed to open mono library from {monoPath}");
            }

            FindMonoExport("mono_profiler_install", ref mono_profiler_install);
            FindMonoExport("mono_profiler_install_enter_leave", ref mono_profiler_install_enter_leave);
            FindMonoExport("mono_profiler_set_events", ref mono_profiler_set_events);
            FindMonoExport("mono_method_get_name", ref mono_method_get_name);
            FindMonoExport("mono_method_get_class", ref mono_method_get_class);
            FindMonoExport("mono_class_get_name", ref mono_class_get_name);
            FindMonoExport("mono_class_get_namespace", ref mono_class_get_namespace);
            FindMonoExport("mono_thread_attach", ref mono_thread_attach);
            FindMonoExport("mono_thread_detach", ref mono_thread_detach);

            s_Initialized = true;
        }

        public static void Deinitialize()
        {
            if (s_MonoHandle != IntPtr.Zero)
            {
                s_Platform.FreeLibrary(s_MonoHandle);
            }

            mono_profiler_install = null;
            mono_profiler_install_enter_leave = null;
            mono_profiler_set_events = null;
            mono_method_get_name = null;
            mono_method_get_class = null;
            mono_class_get_name = null;
            mono_class_get_namespace = null;
            mono_thread_attach = null;
            mono_thread_detach = null;

            s_Initialized = false;
        }

        private static void FindMonoExport<TDelegate>(string functionName, ref TDelegate target) where TDelegate : Delegate
        {
            var functionPointer = s_Platform.GetProcAddress(s_MonoHandle, functionName);
            if (functionPointer == IntPtr.Zero)
            {
                throw new Exception($"Failed to find {functionName} export");
            }

            target = Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPointer);
        }

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_profile_func(IntPtr prof);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_profile_method_func(IntPtr prof, IntPtr method);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_profiler_install_func(IntPtr prof, mono_profile_func callback);
        public static mono_profiler_install_func mono_profiler_install;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_profiler_install_enter_leave_func(mono_profile_method_func enterCallback, mono_profile_method_func leaveCallback);
        public static mono_profiler_install_enter_leave_func mono_profiler_install_enter_leave;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_profiler_set_events_func(MonoProfileFlags events);
        public static mono_profiler_set_events_func mono_profiler_set_events;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate StringBuilder mono_method_get_name_func(IntPtr method);
        public static mono_method_get_name_func mono_method_get_name;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr mono_method_get_class_func(IntPtr method);
        public static mono_method_get_class_func mono_method_get_class;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate StringBuilder mono_class_get_name_func(IntPtr klass);
        public static mono_class_get_name_func mono_class_get_name;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate StringBuilder mono_class_get_namespace_func(IntPtr klass);
        public static mono_class_get_namespace_func mono_class_get_namespace;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_thread_attach_func();
        public static mono_thread_attach_func mono_thread_attach;

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void mono_thread_detach_func();
        public static mono_thread_detach_func mono_thread_detach;
    }
}