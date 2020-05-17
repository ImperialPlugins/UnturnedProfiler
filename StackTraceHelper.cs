using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using ImperialPlugins.UnturnedProfiler.Commands;
using ImperialPlugins.UnturnedProfiler.Extensions;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ImperialPlugins.UnturnedProfiler
{
    public static class StackTraceHelper
    {
        private static readonly Dictionary<int, List<MethodBase>> s_Stack = new Dictionary<int, List<MethodBase>>();
        private static BindingFlags m_BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static readonly HarmonyMethod s_PrefixMethod;
        private static readonly HarmonyMethod s_PostfixMethod;
        private static readonly ThreadLocal<int> s_NoLogAreas = new ThreadLocal<int>(() => 0);

        static StackTraceHelper()
        {
            var preExecutionMethod = typeof(StackTraceHelper).GetMethod(nameof(OnPreExecution), BindingFlags.NonPublic | BindingFlags.Static);
            s_PrefixMethod = new HarmonyMethod(preExecutionMethod);
            var postExecutionMethod = typeof(StackTraceHelper).GetMethod(nameof(OnPostExecution), BindingFlags.NonPublic | BindingFlags.Static);
            s_PostfixMethod = new HarmonyMethod(postExecutionMethod);
        }

        public static IEnumerable<MethodBase> GetStackTrace()
        {
            return GetStackTrace(Thread.CurrentThread);
        }

        public static IEnumerable<MethodBase> GetStackTrace(Thread thread)
        {
            if (!s_Stack.ContainsKey(thread.ManagedThreadId))
            {
                return Enumerable.Empty<MethodBase>();
            }

            return s_Stack[thread.ManagedThreadId];
        }

        public static void EnterNoLogArea()
        {
            s_NoLogAreas.Value++;
        }

        public static void LeaveNoLogArea()
        {
            s_NoLogAreas.Value--;
        }

        public static void RegisterAssemblyForStacktracePatch(Assembly assembly)
        {
            if (ExcludedAssemblies
                .Concat(new[]{
                    "UnityEngine",
                    "Assembly-CSharp"
                })
                .Any(c => assembly.FullName.Contains(c)))
            {
                return;
            }

            ICollection<Type> types;
            try
            {

                types = assembly.GetAllTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            var selfAssembly = typeof(ProfilerPlugin).Assembly;
            foreach (var type in types)
            {
                // whitelist /freezeserver
                if (assembly == selfAssembly && type != typeof(CommandFreezeServer))
                {
                    continue;
                }

                foreach (var method in type.GetMethods(m_BindingFlags))
                {
                    RegisterMethod(assembly, method);
                }

                foreach (var constructor in type.GetConstructors(m_BindingFlags))
                {
                    RegisterMethod(assembly, constructor);
                }

                foreach (var property in type.GetProperties(m_BindingFlags))
                {
                    var getter = property.GetGetMethod(true);
                    if (getter != null)
                    {
                        RegisterMethod(assembly, getter);
                    }

                    var setter = property.GetGetMethod(true);
                    if (setter != null)
                    {
                        RegisterMethod(assembly, setter);
                    }
                }
            }
        }

        private static void RegisterMethod(Assembly asm, MethodBase method)
        {
            var pluginInstance = ProfilerPlugin.Instance;
            try
            {
                pluginInstance.Harmony.Patch(method, s_PrefixMethod, s_PostfixMethod);
            }
            catch
            {
                // ignored
            }
        }

        private static void OnPreExecution(MethodBase __originalMethod)
        {
            if (s_NoLogAreas.Value > 0)
            {
                return;
            }

            EnterNoLogArea();
            try
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;

                if (!s_Stack.ContainsKey(threadId))
                {
                    s_Stack.Add(threadId, new List<MethodBase>());
                }

                var stack = s_Stack[threadId];
                if (stack.Count >= 10)
                {
                    stack.RemoveAt(0);
                }

                stack.Add(__originalMethod);
            }
            finally
            {
                LeaveNoLogArea();
            }
        }

        private static void OnPostExecution(MethodBase __originalMethod)
        {
            if (s_NoLogAreas.Value > 0)
            {
                return;
            }

            EnterNoLogArea();
            try
            {
                if (!s_Stack.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    return;
                }

                var stack = s_Stack[Thread.CurrentThread.ManagedThreadId];
                var idx = stack.Count - 1;

                if (stack.Count == 0 || stack[idx] != __originalMethod)
                {
                    return;
                }

                stack.RemoveAt(idx);
            }
            finally
            {
                LeaveNoLogArea();
            }
        }

        public static string[] ExcludedAssemblies = {
            "Harmony",
            "System",
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
            "mscorlib"
        };
    }
}