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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Harmony;

namespace ImperialPlugins.UnturnedProfiler.Patches
{
    public static class HarmonyProfiling
    {
        private static readonly HarmonyMethod s_PrefixMethod;
        private static readonly HarmonyMethod s_PostfixMethod;
        private static readonly Dictionary<MethodBase, MeasurableMethod> s_Registrations = new Dictionary<MethodBase, MeasurableMethod>();

        public static Dictionary<MethodBase, MeasurableMethod> GetAllRegistrations()
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
            if (measurableMethod == null)
            {
                throw new ArgumentNullException(nameof(measurableMethod));
            }

            var method = measurableMethod.Method;

            var pluginInstance = ProfilerPlugin.Instance;
            pluginInstance.Harmony.Patch(method, s_PrefixMethod, s_PostfixMethod);

            if (!s_Registrations.ContainsKey(method))
            {
                s_Registrations.Add(method, measurableMethod);
            }
            else
            {
                //throw new Exception("Already registered method: " + method.GetFullName());
                s_Registrations[method] = measurableMethod;
            }
        }

        public static void ClearRegistrations()
        {
            foreach (var reg in s_Registrations.Keys)
            {
                var pluginInstance = ProfilerPlugin.Instance;
                pluginInstance.Harmony.Unpatch(reg, s_PrefixMethod.method);
                pluginInstance.Harmony.Unpatch(reg, s_PostfixMethod.method);
            }

            s_Registrations.Clear();
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

            if (!s_Registrations.ContainsKey(__originalMethod))
            {
                return;
            }
            
            MeasurableMethod o = s_Registrations[__originalMethod];
            if (ProfilerPlugin.Instance != null && o.Measurements.Count > ProfilerPlugin.Instance.Configuration.Instance.MaxFrameCount)
            {
                // we might do profiling during a reload, so we need to check if instance is null
                return;
            }

            o.Measurements.Add(time);
        }
    }
}