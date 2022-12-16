﻿using System;
using System.Collections.Generic;
using System.Linq;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands
{
    /// <summary>
    /// Handles all incoming player input
    /// </summary>
    public class CommandHandler : ICommandHandler
    {
        public ICore Core { get; }

        public CommandHandler(ICore core)
        {
            Core = core;
            
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICommand).IsAssignableFrom(p) && !p.IsInterface);

            foreach (var t in commandTypes)
            {
                var command = (ICommand)Activator.CreateInstance(t, Core);

                if (command == null) continue;

                foreach (var alias in command.Aliases)
                {
                    if (Core.Cache.IsCommand(alias))
                        Core.ErrorLog.Write("CommandHandler.cs", "Duplicate Alias", ErrorLog.Priority.Low);
                    else
                        Core.Cache.AddCommand(alias, command);
                }
            }
        }

        /// <summary>
        /// Checks and processes commands
        /// </summary>
        /// <param name="input"></param>
        /// <param name="player"></param>
        /// <param name="room"></param>
        public void HandleCommand(Player player, Room room, string input)
        {
            var commandInput = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            commandInput[0] = commandInput[0].ToLower();

            var command = Core.Cache.GetCommand(commandInput[0]);

            if (command == null)
            {
                Core.Writer.WriteLine("<p>{yellow}That is not a command.{yellow}</p>", player.ConnectionId);
                return;
            }

            if (player.UserRole < command.UserRole)
            {
                Core.Writer.WriteLine("<p>{red}You dont have the required role to use that command.{/red}</p>", player.ConnectionId);
                return;
            }
            
            if (CheckStatus(player, command.DeniedStatus))
            {
                command.Execute(player, room, commandInput);
            }
        }

        /// <summary>
        /// Checks if the player can use the command with their current status
        /// </summary>
        /// <param name="player"></param>
        /// <param name="deniedlist"></param>
        /// <returns></returns>
        private bool CheckStatus(Player player, IEnumerable<CharacterStatus.Status> deniedlist)
        {
            if (!deniedlist.Contains(player.Status)) return true;
            
            switch (player.Status)
            {
                case CharacterStatus.Status.Standing:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while standing.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Sitting:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while sitting.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Sleeping:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while sleeping.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Fighting:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while fighting.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Resting:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while while.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Incapacitated:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while incapacitated.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Dead:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while dead.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Ghost:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while a ghost.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Busy:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while busy.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Floating:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while floating.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Mounted:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while mounted.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Stunned:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while stunned.{/yellow}</p>", player.ConnectionId);
                    break;
                case CharacterStatus.Status.Fleeing:
                    Core.Writer.WriteLine("<p>{yellow}You can't do that while fleeing.{/yellow}</p>", player.ConnectionId);
                    break;
            }

            return false;
        }
    }
}
