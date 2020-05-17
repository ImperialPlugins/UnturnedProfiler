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

using System.Collections.Generic;
using System.Threading;
using Rocket.API;
using Rocket.API.Extensions;
using Rocket.Unturned.Chat;

namespace ImperialPlugins.UnturnedProfiler.Commands
{
    public class CommandFreezeServer : IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1)
            {
                UnturnedChat.Say(caller, "Usage: /freezeserver " + Syntax);
            }

            int ms = command.GetInt32Parameter(0).Value;
            UnturnedChat.Say(caller, "Freezing server for " + ms + "ms.");
            Thread.Sleep(ms);
            UnturnedChat.Say(caller, "Freezing done.");
        }

        public AllowedCaller AllowedCaller { get; } = AllowedCaller.Both;
        public string Name { get; } = "FreezeServer";
        public string Help { get; } = "Freezes the server";
        public string Syntax { get; } = "<milli seconds>";
        public List<string> Aliases { get; } = new List<string>();
        public List<string> Permissions { get; } = new List<string>();
    }
}