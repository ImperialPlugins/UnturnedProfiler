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
using System.IO;
using System.Linq;
using System.Text;
using ImperialPlugins.UnturnedProfiler.Extensions;
using ImperialPlugins.UnturnedProfiler.Patches;
using Rocket.API;
using Rocket.Unturned.Chat;
using UnityEngine;

namespace ImperialPlugins.UnturnedProfiler.Commands
{
    public class CommandStopProfiling : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var pluginInstance = ProfilerPlugin.Instance;
            var registrations = HarmonyProfiling.GetAllRegistrations();

            if (!pluginInstance.IsProfiling)
            {
                UnturnedChat.Say(caller, "Profiling is not running", Color.red);
                return;
            }

            string fileName = "Profiler-" + DateTime.Now.Ticks + ".log";

            pluginInstance.IsProfiling = false;
            using (var logger = new StreamWriter(fileName))
            {
                var measureTypes = registrations.Values.Where(d => d.Measurements.Count > 0).Select(c => c.MeasureType).Distinct();

                foreach (var measureType in measureTypes)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"{measureType}:");

                    bool anyCallsMeasured = false;
                    foreach (var measurableMethod in registrations.Values.Where(d => d.MeasureType.Equals(measureType, StringComparison.OrdinalIgnoreCase)))
                    {
                        var assemblyName = measurableMethod.Method.DeclaringType?.Assembly?.GetName()?.Name?.StripUtf8() ?? "<unknown>";
                        var measurements = measurableMethod.Measurements;
                        if (!measurements.Any())
                        {
                            continue;
                        }

                        string methodName = measurableMethod.Method.GetFullName();

                        //calculate & log averages
                        sb.AppendLine($"\t {assemblyName} {methodName} (avg: {measurements.Average():0}ms, min: {measurements.Min():0}ms, max: {measurements.Max():0}ms, calls: {measurements.Count})");
                        anyCallsMeasured = true;
                    }

                    if (anyCallsMeasured)
                    {
                        logger.Write(sb.ToString());
                        logger.WriteLine();
                    }
                }

                logger.Close();
            }

            HarmonyProfiling.ClearRegistrations();
            UnturnedChat.Say(caller, $"Profiling stopped. Saved as {fileName}.");
        }

        public AllowedCaller AllowedCaller { get; } = AllowedCaller.Both;
        public string Name { get; } = "StopProfiling";
        public string Help { get; } = "Stops profiling.";
        public string Syntax { get; } = "";
        public List<string> Aliases { get; } = new List<string> { "stopp" };
        public List<string> Permissions { get; }
    }
}