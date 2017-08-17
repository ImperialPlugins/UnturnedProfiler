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
using System.Linq;
using System.Reflection;
using Harmony;
using Rocket.Core.Logging;

namespace UnturnedProfiler.Patches
{
    public abstract class UpdateBasePatch : Patch
    {
        public HarmonyMethod Prefix { get; }
        public HarmonyMethod Postfix { get; }

        protected UpdateBasePatch(HarmonyMethod prefix, HarmonyMethod postfix)
        {
            Prefix = prefix;
            Postfix = postfix;
        }

        public sealed override void PatchObject(object o, string target, params object[] args)
        {
            PatchObject(o.GetType(), target, args);
        }

        public sealed override void PatchObject(Type type, string target, params object[] args)
        {
            var targetMethod = type.GetMethod(target, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, args.Cast<Type>().ToArray(), null);

            MeasurableObject o = new MeasurableObject
            {
                Name = type.FullName,
                Type = type,
                MeasureType = MeasureType,
                Measures = new List<decimal>(),
                Method = targetMethod
            };

            ProfilerPlugin.Instance.Assemblies[type.Assembly][type].Add(o);
            Logger.Log(o.MeasureType + " found: " + type.FullName + "." + targetMethod.Name);

            ProfilerPlugin.Instance.Harmony.Patch(targetMethod, Prefix, Postfix);
        }
    }
}