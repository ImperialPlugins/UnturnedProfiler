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

namespace ImperialPlugins.UnturnedProfiler.MonoProfiler
{
    public enum MonoProfileFlags : int
    {
        MonoProfileNone = 0,
        MonoProfileAppdomainEvents = 1 << 0,
        MonoProfileAssemblyEvents = 1 << 1,
        MonoProfileModuleEvents = 1 << 2,
        MonoProfileClassEvents = 1 << 3,
        MonoProfileJitCompilation = 1 << 4,
        MonoProfileInlining = 1 << 5,
        MonoProfileExceptions = 1 << 6,
        MonoProfileAllocations = 1 << 7,
        MonoProfileGc = 1 << 8,
        MonoProfileThreads = 1 << 9,
        MonoProfileRemoting = 1 << 10,
        MonoProfileTransitions = 1 << 11,
        MonoProfileEnterLeave = 1 << 12,
        MonoProfileCoverage = 1 << 13,
        MonoProfileInsCoverage = 1 << 14,
        MonoProfileStatistical = 1 << 15,
        MonoProfileMethodEvents = 1 << 16,
        MonoProfileMonitorEvents = 1 << 17,
        MonoProfileIomapEvents = 1 << 18,
        MonoProfileGcMoves = 1 << 19,
        MonoProfileGcRoots = 1 << 20
    }
}