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