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
using System.Linq;
using System.Reflection;
using System.Threading;
using ImperialPlugins.UnturnedProfiler.Extensions;
using ImperialPlugins.UnturnedProfiler.Patches;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ImperialPlugins.UnturnedProfiler.Commands
{
    public class CommandStartProfiling : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = ProfilerPlugin.Instance;

            if (pluginInstance.IsProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is already running", Color.red);
                return;
            }

            pluginInstance.IsProfiling = true;

            UnturnedChat.Say(caller, "Starting Profiling...");
            ThreadPool.QueueUserWorkItem(c =>
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        RegisterAssembly(assembly);
                    }
                    catch (Exception e)
                    {
                        string message =
                            $"Could not load assembly for patching: {assembly.FullName}, profiling will be disabled for this assembly. See console for details.";

                        UnturnedChat.Say(caller, message, Color.red);
                        Logger.LogException(e, message);
                    }
                }

                RegisterEvents();
                UnturnedChat.Say(caller, "Profiling started.");
            });
        }

        private void RegisterEvents()
        {
            RegisterEvents(U.Events);
        }


        private void RegisterEvents(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            RegisterEvents(instance.GetType(), instance);

            foreach (var client in Provider.clients)
            {

            }
        }

        private void RegisterEvents(Type type, object instance = null)
        {
            if (type.ContainsGenericParameters)
            {
                return;
            }

            try
            {
                foreach (var field in type.GetFields(ReflectionExtensions.AllBindingFlags))
                {
                    if (instance == null && !field.IsStatic)
                    {
                        continue;
                    }

                    if (field.Name.Contains("<>"))
                    {
                        continue;
                    }
                    
                    if (!typeof(Delegate).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    EventProfiling.Register(field, instance);
                }
            }
            catch 
            {
                // ignored
            }
        }

        private void RegisterAssembly(Assembly assembly)
        {
            string[] excludedAssemblies = {
                "Harmony",
                "System",
                "UnturnedProfiler",
                "Mono",
                "Microsoft",
                "Newtonsoft",
                "Pathfinding",
                "UnityEngine.UI",
                "UnityEngine.Video",
                "UnityEngine.Networking",
                "UnityEngine.Timeline",
                "UnityEngine.PostProcessing",
                "AstarPath",
            };

            if (excludedAssemblies.Any(c => assembly.FullName.Contains(c)))
            {
                return;
            }

            List<Type> types = assembly.GetAllTypes();
            foreach (var type in types)
            {
                RegisterEvents(type);

                if (!typeof(Component).IsAssignableFrom(type))
                    continue;

                RegisterUnityEvents(type);
            }
        }

        private void RegisterUnityEvents(Type type)
        {
            string[] unityEvents =
            {
                "Update",
                "FixedUpdate",
                "LateUpdate",
                "OnGUI",
                "OnEnable",
                "OnDisable",
                "Awake",
                "OnDestroy",
                "OnCollisionEnter",
                "OnCollisionExit",
                "OnCollisionStay",
                "OnTriggerEnter",
                "OnTriggerExit",
                "OnTriggerStay",
                "Start",
                "Reset"
            };

            var methods = type.GetMethods(ReflectionExtensions.AllBindingFlags);

            foreach (var unityEventName in unityEvents)
            {
                var method = methods.FirstOrDefault(c => c.Name.Equals(unityEventName, StringComparison.OrdinalIgnoreCase) && c.GetParameters().Length == 0);
                if (method == null || method.IsAbstract || method.GetMethodBody() == null)
                {
                    continue;
                }

                MethodProfiling.Register(unityEventName, method);
            }

            //if (!typeof(MonoBehaviour).IsAssignableFrom(type))
            //{
            //    return;
            //}

            //var components = UnityEngine.Object.FindObjectsOfType(type);
            //if (components != null && components.Length > 0)
            //{
            //    foreach (var component in components.Cast<MonoBehaviour>())
            //    {
            //        foreach (var method in methods)
            //        {
            //            if (component.IsInvoking(method.Name))
            //            {
            //                MethodProfiling.Register("Coroutine", method);
            //            }
            //        }
            //    }
            //}
        }

        public AllowedCaller AllowedCaller { get; } = AllowedCaller.Both;
        public string Name { get; } = "StartProfiling";
        public string Help { get; } = "Starts profiling.";
        public string Syntax { get; } = "";
        public List<string> Aliases { get; } = new List<string> { "startp" };
        public List<string> Permissions { get; }
    }
}