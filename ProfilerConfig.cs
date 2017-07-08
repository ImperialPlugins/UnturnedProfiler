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
using Rocket.API;

namespace UnturnedProfiler
{
    public class ProfilerConfig : IRocketPluginConfiguration
    {
        public string LogFile { get; set; } = "Profiler.log";
        public bool EnableBinaryLog { get; set; } = true;
        public bool ProfileUnturned { get; set; } = false;
        public bool ProfilePlugins { get; set; } = true;

        public void LoadDefaults()
        {

        }
    }
}