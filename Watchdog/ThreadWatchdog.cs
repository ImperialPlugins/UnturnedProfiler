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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace ImperialPlugins.UnturnedProfiler.Watchdog
{
    public class ThreadWatchdog : IDisposable
    {
        private readonly Thread m_TargetThread;
        private readonly TimeSpan m_Timeout;
        private readonly ManualResetEvent m_AliveEvent = new ManualResetEvent(false);
        private MonoProfiler.MonoProfiler m_MonoProfiler;

        private Thread m_WatchdogThread;
        private bool m_IsRunning;
        private bool m_NotifyNextFrozen = true;

        public ThreadWatchdog(Thread targetThread, TimeSpan timeout)
        {
            m_TargetThread = targetThread;
            m_Timeout = timeout;
        }

        public void Start()
        {
            // doesn't work for now
            // m_MonoProfiler = new MonoProfiler.MonoProfiler();
            // m_MonoProfiler.Install();

            m_IsRunning = true;
            m_WatchdogThread = new Thread(WatchdogEntryPoint);
            m_WatchdogThread.Start();
        }

        public void Stop()
        {
            m_IsRunning = false;
            m_WatchdogThread = null;
        }

        private void WatchdogEntryPoint()
        {
            while (m_IsRunning)
            {
                bool isAlive = m_AliveEvent.WaitOne(m_Timeout);
                m_AliveEvent.Reset();

                if (isAlive)
                {
                    m_NotifyNextFrozen = true;
                }
                else if (m_NotifyNextFrozen)
                {
                    var stackTraceList = StackTraceHelper.GetStackTrace(m_TargetThread).Reverse().ToList();
                    string stacktrace = "";
                    foreach (var methodBase in stackTraceList)
                    {
                        string name = methodBase.Name;
                        if (methodBase is MemberInfo m)
                        {
                            name = (m.DeclaringType?.FullName ?? "<unknown>") + "." + name;
                        }

                        stacktrace += "at " + name + Environment.NewLine;
                    }

                    var foregroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] --------------------------------");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Watchdog] Warning: Server is frozen since " + m_Timeout.TotalMilliseconds + "ms!");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] Main thread Stacktrace: ");
                    Console.WriteLine(string.IsNullOrWhiteSpace(stacktrace) ? "Failed to get stacktrace." : stacktrace);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] --------------------------------");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Watchdog] Caused by assembly: " + (stackTraceList.FirstOrDefault()?.DeclaringType?.Assembly.GetName().Name ?? "<unknown>"));
                    Console.ForegroundColor = foregroundColor;

                    m_NotifyNextFrozen = false;
                }
            }
        }

        public void NotifyAlive()
        {
            if (Thread.CurrentThread != m_TargetThread)
            {
                throw new Exception("NotifyAlive must be called from the watched thread!");
            }

            if (!m_AliveEvent.WaitOne(0))
            {
                m_AliveEvent.Set();
            }
        }

        public void Dispose()
        {
            Stop();
            m_AliveEvent?.Dispose();
            m_MonoProfiler?.Dispose();
        }
    }
}