﻿using System;
using ArchaicQuestII.GameLogic.Character;
using System.Web;
using ArchaicQuestII.GameLogic.Commands;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ArchaicQuestII.GameLogic.Loops
{
	public class HintLoop : ILoop
	{
        public int TickDelay => 120000;
        public bool ConfigureAwait => true;

        private ICore _core;
        private List<Player> _players;
        private List<string> _hints;

        public void Init(ICore core, ICommandHandler commandHandler)
        {
            _core = core;

            _hints = new List<string>()
            {
               "If you get lost, enter recall to return to the starting room.",
               "If you need help use newbie to send a message. newbie help me",
               "ArchaicQuest is a new game so might be quite, join the discord to chat to others https://discord.gg/QVF6Uutt",
               "To communicate enter say then the message to speak. such as say hello there"
            };
        }

        public void PreTick()
        {
            _players = _core.Cache.GetPlayerCache().Values.Where(x => x.Config.Hints == true).ToList();
        }

        public void Tick()
        {
            foreach(var player in _players)
            {
                _core.Writer.WriteLine(
                        $"<span style='color:lawngreen'>[Hint]</span> {HttpUtility.HtmlEncode(_hints[DiceBag.Roll(1, 0, _hints.Count)])}",
                        player.ConnectionId);
            }
        }

        public void PostTick()
        {
            _players.Clear();
        }
    }
}
