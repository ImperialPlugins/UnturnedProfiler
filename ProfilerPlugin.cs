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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedProfiler
{
    // ReSharper disable InconsistentNaming
    public class ProfilerPlugin : RocketPlugin<ProfilerConfig>
    {
        private static bool _isProfiling;
        private static readonly Dictionary<Assembly, Dictionary<Type, List<MeasurableObject>>> Assemblies = new Dictionary<Assembly, Dictionary<Type, List<MeasurableObject>>>();
        private static readonly HarmonyInstance Harmony = HarmonyInstance.Create("de.static-interface.unturnedprofiler");

        private bool _patchedPlugins;
        private bool _patchedUnturned;

        [RocketCommandAlias("Startp")]
        [RocketCommand("StartProfiling", "Starts profiling")]
        public void StartProfiling(IRocketPlayer caller, string[] args)
        {
            if (_isProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is already running", Color.red);
                return;
            }

            _isProfiling = true;
            UnturnedChat.Say(caller, "Profiling started");

            if (Configuration.Instance.ProfilePlugins && !_patchedPlugins)
            {
                Logger.Log("Patching plugins...");

                foreach (var pl in R.Plugins.GetPlugins())
                {
                    try
                    {
                        var assembly = pl.GetType().Assembly;
                        Assemblies.Add(assembly, new Dictionary<Type, List<MeasurableObject>>());
                        UnturnedChat.Say(caller, "Patching: " + assembly.FullName);
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
                _patchedUnturned = true;
            }
        }

        private readonly HarmonyMethod _prefixMethod = new HarmonyMethod(typeof(ProfilerPlugin), nameof(HijackPrefix));
        private readonly HarmonyMethod _postUpdate = new HarmonyMethod(typeof(ProfilerPlugin), nameof(HijackUpdatePostfix));
        private readonly HarmonyMethod _postFixedUpdate = new HarmonyMethod(typeof(ProfilerPlugin), nameof(HijackFixedUpdatePostfix));
        private readonly HarmonyMethod _postLateUpdate = new HarmonyMethod(typeof(ProfilerPlugin), nameof(HijackLateUpdatePostfix));

        private bool Patchtype(Type type)
        {
            Assemblies[type.Assembly].Add(type, new List<MeasurableObject>());

            bool measurable = false;
            Logger.Log("Looking for update methods: " + type.FullName);
            var updateMethod = ReflectionUtils.FindMethod(type, "update", StringComparison.OrdinalIgnoreCase);
            if (updateMethod != null)
            {
                MeasurableObject o = new MeasurableObject
                {
                    Name = type.FullName,
                    Type = type,
                    MeasureType = MeasurableObjectType.FrameUpdate,
                    Measures = new List<decimal>(),
                    Method = updateMethod
                };
                Assemblies[type.Assembly][type].Add(o);
                Logger.Log("UpdateMethod found: " + type.FullName + "." + updateMethod.Name);
                Harmony.Patch(updateMethod, _prefixMethod, _postUpdate);
                measurable = true;
            }

            var fixedUpdateMethod = ReflectionUtils.FindMethod(type, "fixedupdate", StringComparison.OrdinalIgnoreCase);
            if (fixedUpdateMethod != null)
            {
                MeasurableObject o = new MeasurableObject
                {
                    Name = type.FullName,
                    Type = type,
                    MeasureType = MeasurableObjectType.FrameFixedUpdate,
                    Measures = new List<decimal>(),
                    Method = fixedUpdateMethod
                };
                Assemblies[type.Assembly][type].Add(o);
                Logger.Log("FixedUpdate found: " + type.FullName + "." + fixedUpdateMethod.Name);
                Harmony.Patch(fixedUpdateMethod, _prefixMethod, _postFixedUpdate);
                measurable = true;
            }

            var lateUpdateMethod = ReflectionUtils.FindMethod(type, "lateupdate", StringComparison.OrdinalIgnoreCase);
            if (lateUpdateMethod != null)
            {
                MeasurableObject o = new MeasurableObject
                {
                    Name = type.FullName,
                    Type = type,
                    MeasureType = MeasurableObjectType.FrameLateUpdate,
                    Measures = new List<decimal>(),
                    Method= lateUpdateMethod
                };
                Assemblies[type.Assembly][type].Add(o);
                Logger.Log("LateUpdate found: " + type.FullName + "." + lateUpdateMethod.Name);
                Harmony.Patch(lateUpdateMethod, _prefixMethod, _postLateUpdate);
                measurable = true;
            }

            if (!measurable)
            {
                Assemblies[type.Assembly].Remove(type);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        public static void HijackPrefix(object __instance, ref object __state)
        {
            if (!_isProfiling)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            __state = sw;
        }

        [HarmonyPostfix]
        public static void HijackUpdatePostfix(object __instance, object __state)
        {
            DoPostfix(__instance, MeasurableObjectType.FrameUpdate, (Stopwatch)__state);
        }

        [HarmonyPostfix]
        public static void HijackFixedUpdatePostfix(object __instance, object __state)
        {
            DoPostfix(__instance, MeasurableObjectType.FrameFixedUpdate, (Stopwatch)__state);
        }

        [HarmonyPostfix]
        public static void HijackLateUpdatePostfix(object __instance, object __state)
        {
            DoPostfix(__instance, MeasurableObjectType.FrameLateUpdate, (Stopwatch)__state);
        }

        private static void DoPostfix(object instance, MeasurableObjectType type, Stopwatch sw)
        {
            if (!_isProfiling)
                return;

            long time = sw.ElapsedMilliseconds;
            sw.Reset();

            MeasurableObject o = Assemblies[instance.GetType().Assembly][instance.GetType()].First(c => c.MeasureType == type);
            o.Measures.Add(time);
        }
        
        [RocketCommandAlias("Stopp")]
        [RocketCommand("StopProfiling", "Stops profiling")]
        public void StopProfiling(IRocketPlayer caller, string[] args)
        {
            if (!_isProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is not running", Color.red);
                return;
            }

            _isProfiling = false;
            using (var logger = new StreamWriter(Configuration.Instance.LogFile))
            {
                foreach (var assembly in Assemblies.Keys)
                {
                    List<decimal> updateFrames = new List<decimal>();
                    List<decimal> fixedUpdateFrames = new List<decimal>();
                    List<decimal> lateUpdateFrame = new List<decimal>();

                    foreach (var type in Assemblies[assembly].Keys)
                    {
                        updateFrames.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameUpdate)?.Measures ?? new List<decimal>());
                        fixedUpdateFrames.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameFixedUpdate)?.Measures ?? new List<decimal>());
                        lateUpdateFrame.AddRange(Assemblies[assembly][type].FirstOrDefault(c => c.MeasureType == MeasurableObjectType.FrameLateUpdate)?.Measures ?? new List<decimal>());
                    }

                    logger.WriteLine("Module {0} (avg. update: {1}, avg. fixed update: {2}, avg. late update: {3})", Path.GetFileName(assembly.Location),
                        updateFrames.Count > 0 ? updateFrames.Average().ToString("0") + "ms" : "<not measured>",
                        fixedUpdateFrames.Count > 0 ? fixedUpdateFrames.Average() + "ms" : "<not measured>", 
                        lateUpdateFrame.Count > 0 ? lateUpdateFrame.Average() + "ms" : "<not measured>");

                    foreach (var type in Assemblies[assembly].Keys)
                    {
                        logger.WriteLine("\t{0} [{1}]", type.FullName, type.BaseType.Name);

                        foreach (var o in Assemblies[assembly][type])
                        {
                            if(o.Measures.Count == 0)
                                continue;
                            logger.WriteLine("\t\t{0}.{1}() (avg. {2:0}ms, min. {3}ms, max. {4}ms, frame count {5})", o.Name, o.Method.Name, o.Measures.Average(), o.Measures.Min(), o.Measures.Max(), o.Measures.Count);
                        }
                    }
                }
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
            UnturnedChat.Say(caller, "Dumped memory at profiler/dumb");
        }
    }
}
