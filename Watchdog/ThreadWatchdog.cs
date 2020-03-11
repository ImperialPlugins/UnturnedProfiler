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
using System.Threading;

namespace ImperialPlugins.UnturnedProfiler.Watchdog
{
    public class ThreadWatchdog : IDisposable
    {
        private readonly Thread m_TargetThread;
        private readonly TimeSpan m_Timeout;
        private readonly ManualResetEvent m_AliveEvent = new ManualResetEvent(false);

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
                    var stackTrace = GetStackTrace(m_TargetThread);
                    var foregroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] --------------------------------");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Watchdog] Warning: Server is frozen since " + m_Timeout.TotalMilliseconds + "ms!");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] Main thread Stacktrace: ");
                    Console.WriteLine(stackTrace?.ToString() ?? "Failed to get stacktrace.");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Watchdog] --------------------------------");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Watchdog] Caused by assembly: " + (stackTrace?.GetFrame(0).GetMethod()?.DeclaringType?.Assembly.GetName().Name ?? "<unknown>"));
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
            m_AliveEvent?.Dispose();
            Stop();
        }

#pragma warning disable 618
        private StackTrace GetStackTrace(Thread targetThread)
        {
            StackTrace stackTrace = null;
            var ready = new ManualResetEventSlim();

            new Thread(() =>
            {
                // Backstop to release thread in case of deadlock:
                ready.Set();
                Thread.Sleep(200);
                try
                {
                    targetThread.Resume();
                }
                catch
                {
                    // ignored
                }
            }).Start();

            ready.Wait();
            targetThread.Suspend();

            try
            {
                stackTrace = new StackTrace(targetThread, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                /* Deadlock */
            }
            finally
            {
                try
                {
                    targetThread.Resume();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    stackTrace = null;  /* Deadlock */
                }
            }

            return stackTrace;
        }
#pragma warning restore 618
    }
}