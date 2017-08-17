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
using Harmony;

namespace UnturnedProfiler.Patches.EventImpl
{
    public class OnPlayerConnectedPatch : EventPatch
    {
        public override void PatchObject(Type type, string target, params object[] args)
        {
            throw new NotSupportedException("Only instance patching is supported");
        }

        public OnPlayerConnectedPatch() : base(MeasurableObjectType.PlayerConnectedEvent, new HarmonyMethod(typeof(OnPlayerConnectedPatch), nameof(HijackPrefix)), new HarmonyMethod(typeof(OnPlayerConnectedPatch), nameof(HijackPostfix)))
        {

        }

        public static void HijackPrefix(object __instance, ref object __state)
        {
            DoPrefix(__instance, ref __state);
        }

        public static void HijackPostfix(object __instance, object __state)
        {
            DoPostfix(__instance, __state, MeasurableObjectType.PlayerConnectedEvent);
        }
    }
}