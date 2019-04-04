﻿#region Copyright
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
using System.Linq;
using System.Reflection;
using Rocket.Core.Utils;

namespace ImperialPlugins.UnturnedProfiler.Extensions
{
    public static class ReflectionExtensions
    {
        public const BindingFlags AllBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static List<Type> GetAllTypes(this Assembly assembly)
        {
            return RocketHelper.GetTypes(new List<Assembly> {assembly});
        }

        public static string GetFullName(this MethodBase method)
        {
            return $"{method.ReflectedType?.FullName ?? "<unknown>"}.{method.Name}({string.Join(", ", method.GetParameters().Select(o => $"{o.ParameterType} {o.Name}").ToArray())})";
        }
    }
}