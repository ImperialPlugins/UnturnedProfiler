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

using Harmony;

namespace UnturnedProfiler.Patches.UpdateImpl
{
    public class UpdatePatch : UpdateBasePatch
    {
        public UpdatePatch() : base(new HarmonyMethod(typeof(UpdatePatch), nameof(HijackPrefix)), new HarmonyMethod(typeof(UpdatePatch), nameof(HijackPostfix)))
        {
        }

        public static void HijackPrefix(object __instance, ref object __state)
        {
            DoPrefix(__instance, ref __state);
        }

        public static void HijackPostfix(object __instance, object __state)
        {
            DoPostfix(__instance, __state, MeasurableObjectType.FrameUpdate);
        }
        
        public override MeasurableObjectType MeasureType
            => MeasurableObjectType.FrameUpdate;
    }
}