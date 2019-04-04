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

namespace ImperialPlugins.UnturnedProfiler.Patches
{
    public static class MethodProfiling
    {
        public static void Register(string measurableType, MethodBase targetMethod)
        {
            if (targetMethod == null)
            {
                throw new ArgumentNullException(nameof(targetMethod));
            }

            MeasurableMethod o = new MeasurableMethod
            {
                MeasureType = measurableType,
                Measurements = new List<decimal>(),
                Method = targetMethod
            };

            HarmonyProfiling.RegisterMethod(o);

        }
        public static void Register(string measurableType, Type type, string target, params Type[] args)
        {
            var targetMethod = type.GetMethod(target, ReflectionExtensions.AllBindingFlags,
                null,
                CallingConventions.Any,
                args,
                null);

            Register(measurableType, targetMethod);
        }
    }
}