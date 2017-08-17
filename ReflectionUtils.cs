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
using Mono.Cecil;
using Rocket.Core.Logging;

namespace UnturnedProfiler
{
    public static class ReflectionUtils
    {
        /* Workaround with Mono.Cecil for optional dependencies */
        public static List<Type> GetTypes(Assembly assembly)
        {
            AssemblyDefinition asm = AssemblyFactory.GetAssembly(assembly.Location);
            List<Type> types = new List<Type>();
            foreach (ModuleDefinition module in asm.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    try
                    {
                        var t = assembly.GetType(type.FullName);
                        if (t == null)
                            continue;
                        types.Add(t);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e, "Failed to load type: " + type.FullName);
                    }
                }
            }

            return types;
        }

        /* Find method based on string comparison */
        public static MethodBase FindMethod(Type type, string methodName, StringComparison comparison = StringComparison.Ordinal)
        {
            var methods =
                type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .ToList();

            methods.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic));
            return methods.FirstOrDefault(c => c.Name.Equals(methodName, comparison));
        }
    }
}