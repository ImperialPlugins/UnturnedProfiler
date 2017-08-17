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
using System.Diagnostics;
using System.Linq;
using Rocket.Core.Logging;

namespace UnturnedProfiler.Patches
{
    public abstract class Patch
    {
        public abstract void PatchObject(Type type, string target, params object[] args);
        public abstract void PatchObject(Object instance, string target, params object[] args);

        public abstract MeasurableObjectType MeasureType { get; }

        protected static void DoPostfix(object __instance, object __state, MeasurableObjectType type)
        {
            if (!ProfilerPlugin.IsProfiling)
                return;

            var sw = __state as Stopwatch;
            if (sw == null)
                return;

            long time = sw.ElapsedMilliseconds;
            sw.Reset();

            MeasurableObject o = ProfilerPlugin.Instance.Assemblies[__instance.GetType().Assembly][__instance.GetType()].First(c => c.MeasureType == type);

            if (ProfilerPlugin.Instance != null && o.Measures.Count > ProfilerPlugin.Instance.Configuration.Instance.MaxFrameCount)
            {
                // we might do profiling during a reload, so we need to check if instance is null
                return;
            }

            o.Measures.Add(time);
        }

        protected static void DoPrefix(object __instance, ref object __state)
        {
            if (!ProfilerPlugin.IsProfiling)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            __state = sw;
        }
    }
}