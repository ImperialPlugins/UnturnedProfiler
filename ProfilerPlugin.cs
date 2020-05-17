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
using System.Collections;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using ImperialPlugins.UnturnedProfiler.Configuration;
using ImperialPlugins.UnturnedProfiler.MonoProfiler;
using ImperialPlugins.UnturnedProfiler.Platform;
using ImperialPlugins.UnturnedProfiler.Watchdog;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ImperialPlugins.UnturnedProfiler
{
    public class ProfilerPlugin : RocketPlugin<ProfilerConfig>
    {
        public const string HarmonyInstanceId = "com.imperialplugins.unturnedprofiler";

        public static ProfilerPlugin Instance { get; private set; }
        public bool IsProfiling { get; internal set; }
        public Harmony Harmony { get; } = new Harmony(HarmonyInstanceId);

        private GameObject m_WatchdogGameObject;

        protected override void Load()
        {
            base.Load();
            Instance = this;

            var libraryPlatform = PlatformHelper.IsLinux ? (LibraryPlatform)new LinuxPlatform() : new WindowsPlatform();
            MonoAPI.Initialize(libraryPlatform);

            if (Configuration.Instance.EnableWatchdog)
            {
                Logger.Log("Installing watchdog...", ConsoleColor.Yellow);

                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                ThreadPool.QueueUserWorkItem(c =>
                {
                    DateTime startTime = DateTime.UtcNow;

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            StackTraceHelper.RegisterAssemblyForStacktracePatch(assembly);
                        }
                        catch (Exception e)
                        {
                            Logger.LogException(e, $"Could not load assembly for patching: {assembly.FullName}, watchdog will not work detect this assembly.");
                        }
                    }

                    TaskDispatcher.QueueOnMainThread(() => InstallWatchdog(DateTime.UtcNow - startTime));
                });
            }
        }

        private void InstallWatchdog(TimeSpan time)
        {
            m_WatchdogGameObject = new GameObject();
            DontDestroyOnLoad(m_WatchdogGameObject);

            m_WatchdogGameObject.SetActive(false);
            var watchdogComponent = m_WatchdogGameObject.AddComponent<UnityWatchdogComponent>();
            watchdogComponent.Timeout = TimeSpan.FromMilliseconds(Configuration.Instance.WatchdogTimeoutMilliSeconds);
            m_WatchdogGameObject.SetActive(true);
            Logger.Log($"Watchdog is ready after {time.TotalMilliseconds:####}ms.", ConsoleColor.Green);
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            StackTraceHelper.RegisterAssemblyForStacktracePatch(args.LoadedAssembly);
        }

        protected override void Unload()
        {
            base.Unload();
            Instance = null;

            if (m_WatchdogGameObject != null)
            {
                Destroy(m_WatchdogGameObject);
                m_WatchdogGameObject = null;
            }

            MonoAPI.Deinitialize();
        }
    }
}
