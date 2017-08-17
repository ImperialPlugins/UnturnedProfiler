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
using System.Reflection;
using Harmony;
using Rocket.Core.Logging;

namespace UnturnedProfiler.Patches
{
    public abstract class EventPatch : Patch
    {
        protected HarmonyMethod Prefix { get; }
        protected HarmonyMethod Postfix { get; }

        public override MeasurableObjectType MeasureType { get; }

        protected EventPatch(MeasurableObjectType @event, HarmonyMethod prefix, HarmonyMethod postfix)
        {
            Prefix = prefix;
            Postfix = postfix;
            MeasureType = @event;
        }

        public override void PatchObject(object instance, string target, params object[] args)
        {
            PatchObject(instance, instance.GetType(), target);
        }

        public override void PatchObject(Type type, string target, params object[] args)
        {
            PatchObject(null, type, target);
        }

        private void PatchObject(object instance, Type type, string target)
        {
            Logger.Log("Patching event: " + target);
            var eventDelegate = (MulticastDelegate)type.GetField(target, BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).GetValue(instance);

            if (eventDelegate == null)
            {
                Logger.LogWarning("eventDelegate null for " + target + ", will not be measured");
                return;
            }

            foreach (var handler in eventDelegate.GetInvocationList())
            {
                var method = handler.Method;
                if (method == null)
                {
                    Logger.LogWarning("method null for: " + handler.GetType().Name);
                    continue;
                }

                var dType = handler.Method.DeclaringType;

                if (dType == null)
                {
                    Logger.LogWarning("dtype null for: " + method.Name);
                    continue;
                }

                Logger.Log("Patching method: " + dType.FullName + "." + method.Name + " for event: " + target);

                MeasurableObject o = new MeasurableObject
                {
                    Name = target,
                    Type = dType,
                    MeasureType = MeasureType,
                    Measures = new List<decimal>(),
                    Method = method
                };

                var asm = dType.Assembly;
                if (!ProfilerPlugin.Instance.Assemblies.ContainsKey(asm))
                    ProfilerPlugin.Instance.Assemblies.Add(asm, new Dictionary<Type, List<MeasurableObject>>());

                if (!ProfilerPlugin.Instance.Assemblies[asm].ContainsKey(dType))
                    ProfilerPlugin.Instance.Assemblies[asm].Add(dType, new List<MeasurableObject>());
                
                ProfilerPlugin.Instance.Assemblies[asm][dType].Add(o);

                if (!ProfilerPlugin.Instance.Events.ContainsKey(target))
                    ProfilerPlugin.Instance.Events.Add(target, new List<MeasurableObject>());


                ProfilerPlugin.Instance.Events[target].Add(o);
                ProfilerPlugin.Instance.Harmony.Patch(method, Prefix, Postfix);
            }
        }
    }
}