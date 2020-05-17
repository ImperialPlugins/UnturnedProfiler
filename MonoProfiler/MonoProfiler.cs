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
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Logger = Rocket.Core.Logging.Logger;

namespace ImperialPlugins.UnturnedProfiler.MonoProfiler
{
    public class MonoProfiler : IDisposable
    {
        private readonly IntPtr m_ProfilerMemory;

        private int m_NoProfileRegionCount;
        private bool m_Installed;
        private bool m_Diposed;

        public MonoProfiler()
        {
            m_ProfilerMemory = Marshal.AllocHGlobal(10);

            if (m_ProfilerMemory == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate memory for profiler");
            }
        }

        public void Install()
        {
            if (m_Installed)
            {
                return;
            }

            if (m_Diposed)
            {
                throw new ObjectDisposedException("MonoProfiler");
            }

            MonoAPI.mono_profiler_install(m_ProfilerMemory, OnProfilerShutdown);
            MonoAPI.mono_profiler_install_enter_leave(OnMethodEnter, OnMethodLeave);
            MonoAPI.mono_profiler_set_events(MonoProfileFlags.MonoProfileEnterLeave);

            Logger.Log("MonoProfiler is ready.");
            Thread.Sleep(1000);
            m_Installed = true;
        }

        [SuppressUnmanagedCodeSecurity]
        private void OnMethodEnter(IntPtr prof, IntPtr method)
        {
            Console.WriteLine(method.ToString("X2"));
            //if (m_NoProfileRegionCount > 0)
            //{
            //    return;
            //}

            //EnterNoProfileRegion();

            //try
            //{
            //    IntPtr klass = MonoAPI.mono_method_get_class(method);
            //    string name = MonoAPI.mono_class_get_namespace(klass) + "." + MonoAPI.mono_class_get_name(klass) +
            //                  "::" + MonoAPI.mono_method_get_name(method);

            //    Logger.Log(name);
            //}
            //finally
            //{
            //    LeaveNoProfileRegion();
            //}
        }

        [SuppressUnmanagedCodeSecurity]
        private void OnMethodLeave(IntPtr prof, IntPtr method)
        {

        }

        private void OnProfilerShutdown(IntPtr prof)
        {
            Logger.Log("MonoProfiler shutting down");
        }

        public void EnterNoProfileRegion()
        {
            m_NoProfileRegionCount++;
        }

        public void LeaveNoProfileRegion()
        {
            m_NoProfileRegionCount--;
        }

        public void Dispose()
        {
            if (m_ProfilerMemory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_ProfilerMemory);
            }

            m_Diposed = true;
        }
    }
}