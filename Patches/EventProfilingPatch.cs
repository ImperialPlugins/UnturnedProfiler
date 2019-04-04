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
using System.Reflection;
using ImperialPlugins.UnturnedProfiler.Extensions;
using Rocket.Core.Logging;

namespace ImperialPlugins.UnturnedProfiler.Patches
{
    public static class EventProfiling
    {
        public static void Register(object instance, string target)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var type = instance.GetType();
            var fieldInfo = type.GetField(target, ReflectionExtensions.AllBindingFlags);
            Register(fieldInfo, instance);
        }

        public static void Register(Type type, string target)
        {
            var fieldInfo = type.GetField(target, ReflectionExtensions.AllBindingFlags);
            Register(fieldInfo, (object) null);
        }

        public static void Register(FieldInfo field, object instance)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            var eventDelegate = field.GetValue(instance) as MulticastDelegate;
            Register(field.Name, eventDelegate);
        }

        public static void Register(string eventName, Delegate eventDelegate)
        {
            if (eventDelegate == null)
            {
                throw new ArgumentNullException(nameof(eventDelegate));
            }

            foreach (var handler in eventDelegate.GetInvocationList())
            {
                var method = handler.Method;
                var dType = handler.Method.DeclaringType;

                if (dType == null)
                {
                    Logger.LogWarning("DeclaringType null for: " + method.Name);
                    continue;
                }

                MeasurableMethod o = new MeasurableMethod
                {
                    MeasureType = eventName,
                    Measurements = new List<decimal>(),
                    Method = method
                };

                HarmonyProfiling.RegisterMethod(o);
            }
        }
    }
}