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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Harmony;
using Rocket.Core.Logging;

namespace ImperialPlugins.UnturnedProfiler.Patches
{
    public static class HarmonyProfiling
    {
        private static readonly HarmonyMethod s_PrefixMethod;
        private static readonly HarmonyMethod s_PostfixMethod;
        private static readonly Dictionary<string, List<MeasurableMethod>> s_Registrations = new Dictionary<string, List<MeasurableMethod>>();

        public static Dictionary<string, List<MeasurableMethod>> GetAllRegistrations()
        {
            return s_Registrations;
        }

        static HarmonyProfiling()
        {
            s_PrefixMethod = new HarmonyMethod(typeof(HarmonyProfiling), nameof(OnPreExecution));
            s_PostfixMethod = new HarmonyMethod(typeof(HarmonyProfiling), nameof(OnPostExecution));
        }

        public static void RegisterMethod(MeasurableMethod measurableMethod)
        {
            var methodBase = measurableMethod.Method;

            var pluginInstance = ProfilerPlugin.Instance;
            pluginInstance.Harmony.Patch(methodBase, s_PrefixMethod, s_PostfixMethod);

            var declaringType = methodBase.DeclaringType;
            if (declaringType == null)
            {
                Logger.LogWarning("DeclaringType null for: " + methodBase.Name);
                return;
            }

            var declaringAssembly = declaringType.Assembly;
            var measureType = measurableMethod.MeasureType;

            if (!s_Registrations.ContainsKey(measureType))
            {
                s_Registrations.Add(measureType, new List<MeasurableMethod>());
            }

            s_Registrations[measureType].Add(measurableMethod);
        }

        public static void OnPreExecution(ref object __state, MethodBase __originalMethod)
        {
            if (!ProfilerPlugin.Instance.IsProfiling)
            {
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            __state = sw;
        }

        public static void OnPostExecution(object __state, MethodBase __originalMethod)
        {
            if (!ProfilerPlugin.Instance.IsProfiling)
            {
                return;
            }

            var sw = __state as Stopwatch;
            if (sw == null)
            {
                return;
            }

            sw.Stop();
            long time = sw.ElapsedMilliseconds;

            if (s_Registrations == null)
            {
                return;
            }

            var type = __originalMethod.DeclaringType;
            if (type == null)
            {
                return;
            }

            MeasurableMethod o = null;
            foreach (var measureType in s_Registrations.Values)
            {
                o = measureType.FirstOrDefault(c => c.Method == __originalMethod);
                if (o != null)
                {
                    break;
                }
            }

            if (o == null)
            {
                return;
            }

            if (ProfilerPlugin.Instance != null && o.Measurements.Count > ProfilerPlugin.Instance.Configuration.Instance.MaxFrameCount)
            {
                // we might do profiling during a reload, so we need to check if instance is null
                return;
            }

            o.Measurements.Add(time);
        }
    }
}