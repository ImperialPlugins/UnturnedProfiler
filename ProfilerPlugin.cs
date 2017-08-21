/*
 *  Unturned Profiler - A plugin to profile Unturned servers for analyzing lag causes
 *  Copyright (C) 2017 Trojaner <trojaner25@gmail.com>
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using HighlightingSystem;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using UnityEngine;
using UnturnedProfiler.Patches;
using UnturnedProfiler.Patches.EventImpl;
using UnturnedProfiler.Patches.UpdateImpl;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedProfiler
{
    // ReSharper disable InconsistentNaming
    public class ProfilerPlugin : RocketPlugin<ProfilerConfig>
    {
        public static ProfilerPlugin Instance { get; private set; }
        protected override void Load()
        {
            base.Load();
            Instance = this;
        }

        protected override void Unload()
        {
            base.Unload();
            Instance = null;
        }

        public static bool IsProfiling { get; private set; }
        public readonly Dictionary<Assembly, Dictionary<Type, List<MeasurableObject>>> Assemblies = new Dictionary<Assembly, Dictionary<Type, List<MeasurableObject>>>();
        public readonly Dictionary<string, List<MeasurableObject>> Events = new Dictionary<string, List<MeasurableObject>>();

        public HarmonyInstance Harmony { get; } = HarmonyInstance.Create("de.static-interface.unturnedprofiler");

        private bool _patchedPlugins;
        private bool _patchedUnturned;
        private bool _patchedRocket;
        private bool _patchedEvents;

        private readonly UpdatePatch UpdatePatch = new UpdatePatch();
        private readonly FixedUpdatePatch FixedUpdatePatch = new FixedUpdatePatch();
        private readonly LateUpdatePatch LateUpdatePatch = new LateUpdatePatch();

        [RocketCommandAlias("Startp")]
        [RocketCommand("StartProfiling", "Starts profiling")]
        public void StartProfiling(IRocketPlayer caller, string[] args)
        {
            if (IsProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is already running", Color.red);
                return;
            }

            IsProfiling = true;
            UnturnedChat.Say(caller, "Profiling started");

            if (Configuration.Instance.ProfilePlugins && !_patchedPlugins)
            {
                Logger.Log("Patching plugins...");

                foreach (var pl in R.Plugins.GetPlugins())
                {
                    try
                    {
                        var assembly = pl.GetType().Assembly;
                        PatchAssembly(assembly);
                    }
                    catch (Exception e)
                    {
                        UnturnedChat.Say(caller,
                            "Could not load plugin: " + pl +
                            ", profiling will be disabled for this plugin. See console for details", Color.red);
                        Logger.LogException(e, "Could not load types from plugin: " + pl.Name);
                    }
                }

                _patchedPlugins = true;
            }

            if (Configuration.Instance.ProfileUnturned && !_patchedUnturned)
            {
                PatchAssembly(typeof(SDG.Unturned.Player).Assembly); //Assembly-CSharp.dll
                PatchAssembly(typeof(Highlighter).Assembly); //Assembly-CSharp-firstpass.dll
                _patchedUnturned = true;
            }

            if (Configuration.Instance.ProfileRocketMod && !_patchedRocket)
            {
                PatchAssembly(typeof(R).Assembly); //Rocket.Core.dll
                PatchAssembly(typeof(U).Assembly); //Rocket.Unturned.dll
                PatchAssembly(typeof(RocketPlayer).Assembly); //Rocket.API.dll
                _patchedRocket = true;
            }

            if (Configuration.Instance.ProfileEvents && !_patchedEvents)
            {
                PatchEvents();
                _patchedEvents = true;
            }
        }

        private void PatchEvents()
        {
            EventPatch patch = new OnBeforePlayerConnectedPatch();
            patch.PatchObject(U.Events, nameof(U.Events.OnBeforePlayerConnected));

            patch = new OnPlayerConnectedPatch();
            patch.PatchObject(U.Events, nameof(U.Events.OnPlayerConnected));

            patch = new OnPlayerDisconnectedPatch();
            patch.PatchObject(U.Events, nameof(U.Events.OnPlayerDisconnected));
            
            patch = new OnPlayerRevivePatch();
            patch.PatchObject(typeof(UnturnedPlayerEvents), nameof(UnturnedPlayerEvents.OnPlayerRevive));

            patch = new OnPlayerDeadPatch();
            patch.PatchObject(typeof(UnturnedPlayerEvents), nameof(UnturnedPlayerEvents.OnPlayerDead));

            patch = new OnPlayerDeathPatch();
            patch.PatchObject(typeof(UnturnedPlayerEvents), nameof(UnturnedPlayerEvents.OnPlayerDeath));
        }

        private void PatchAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly, new Dictionary<Type, List<MeasurableObject>>());
            List<Type> types = ReflectionUtils.GetTypes(assembly);
            bool measurable = false;
            foreach (var type in types)
            {
                if (!typeof(Component).IsAssignableFrom(type))
                    continue;

                if (Patchtype(type))
                    measurable = true;
            }

            if (!measurable)
                Assemblies.Remove(assembly);
        }

        private bool Patchtype(Type type)
        {
            Assemblies[type.Assembly].Add(type, new List<MeasurableObject>());

            bool measurable = false;
            var updateMethod = ReflectionUtils.FindMethod(type, "update", StringComparison.OrdinalIgnoreCase);
            if (updateMethod != null)
            {
                UpdatePatch.PatchObject(type, updateMethod.Name);
                measurable = true;
            }

            var fixedUpdateMethod = ReflectionUtils.FindMethod(type, "fixedupdate", StringComparison.OrdinalIgnoreCase);
            if (fixedUpdateMethod != null)
            {
                FixedUpdatePatch.PatchObject(type, fixedUpdateMethod.Name);
                measurable = true;
            }

            var lateUpdateMethod = ReflectionUtils.FindMethod(type, "lateupdate", StringComparison.OrdinalIgnoreCase);
            if (lateUpdateMethod != null)
            {
                LateUpdatePatch.PatchObject(type, lateUpdateMethod.Name);
                measurable = true;
            }

            if (!measurable)
            {
                Assemblies[type.Assembly].Remove(type);
            }

            return measurable;
        }

        [RocketCommandAlias("Stopp")]
        [RocketCommand("StopProfiling", "Stops profiling")]
        public void StopProfiling(IRocketPlayer caller, string[] args)
        {
            if (!IsProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is not running", Color.red);
                return;
            }

            IsProfiling = false;
            using (var logger = new StreamWriter(Configuration.Instance.LogFile))
            {
                foreach (var assembly in Assemblies.Keys)
                {
                    List<decimal> updateFrames = new List<decimal>();
                    List<decimal> fixedUpdateFrames = new List<decimal>();
                    List<decimal> lateUpdateFrame = new List<decimal>();

                    //fill data for lists above
                    foreach (var type in Assemblies[assembly].Keys)
                    {
                        updateFrames.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameUpdate)?.Measures ?? new List<decimal>());
                        fixedUpdateFrames.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameFixedUpdate)?.Measures ?? new List<decimal>());
                        lateUpdateFrame.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameLateUpdate)?.Measures ?? new List<decimal>());

                    }

                    //calculate & log averages
                    logger.WriteLine("Module {0} (avg. update: {1}, avg. fixed update: {2}, avg. late update: {3})", Path.GetFileName(assembly.Location),
                        updateFrames.Count > 0 ? updateFrames.Average().ToString("0") + "ms" : "<not measured>",
                        fixedUpdateFrames.Count > 0 ? fixedUpdateFrames.Average() + "ms" : "<not measured>",
                        lateUpdateFrame.Count > 0 ? lateUpdateFrame.Average() + "ms" : "<not measured>");

                    foreach (var type in Assemblies[assembly].Keys)
                    {
                        if (Assemblies[assembly][type].Count == 0 || Assemblies[assembly][type].All(c => c.Measures.Count == 0))
                            continue;

                        logger.WriteLine("\t{0} [{1}]", type.FullName, type.BaseType?.Name ?? "none");

                        foreach (var o in Assemblies[assembly][type])
                        {
                            if (o.Measures.Count == 0)
                                continue;
                            logger.WriteLine("\t\t{0}.{1}() (avg. {2:0}ms, min. {3}ms, max. {4}ms, frame count {5})", o.Type.FullName, o.Method.Name, o.Measures.Average(), o.Measures.Min(), o.Measures.Max(), o.Measures.Count);
                        }
                    }
                }

                logger.WriteLine();
                logger.WriteLine("Events:");

                foreach (var @event in Events.Keys)
                {
                    if(Events[@event].Sum(o => o.Measures.Count) == 0)
                        continue;

                    logger.WriteLine("\t{0}()", @event);

                    foreach (var o in Events[@event])
                    {
                        if (o.Measures.Count == 0)
                            continue;
                        logger.WriteLine("\t\t{0}.{1}() (avg. {2:0}ms, min. {3}ms, max. {4}ms, event trigger count {5})", o.Type.FullName, o.Method.Name, o.Measures.Average(), o.Measures.Min(), o.Measures.Max(), o.Measures.Count);
                    }
                }

                Assemblies.Clear();
                Events.Clear();
                logger.Close();
            }

            UnturnedChat.Say(caller, "Profiling stopped");
        }

        [RocketCommandAlias("Md")]
        [RocketCommandAlias("Dump")]
        [RocketCommand("MemoryDump", "Dumps memory")]
        public void MemoryDump(IRocketPlayer caller, string[] args) //dangerous command, can crash server
        {
            if (!System.IO.Directory.Exists("Profiler"))
                System.IO.Directory.CreateDirectory("Profiler");
            UnityHeapDump.Create("Profiler/Dump");
            UnturnedChat.Say(caller, "Dumped memory at Profiler/Dump");
        }
    }
}
