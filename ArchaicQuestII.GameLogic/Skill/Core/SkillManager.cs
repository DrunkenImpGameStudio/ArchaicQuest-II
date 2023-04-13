﻿using System;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Client;
using ArchaicQuestII.GameLogic.Combat;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Spell;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Skill.Core
{
    public class SkillManager : ISkillManager
    {
        private readonly IWriteToClient _writer;
        private readonly IUpdateClientUI _updateClientUi;
        private readonly IDamage _damage;
        private readonly ICombat _fight;

        public SkillManager(
            IWriteToClient writer,
            IUpdateClientUI updateClientUi,
            IDamage damage,
            ICombat fight
        )
        {
            _writer = writer;
            _updateClientUi = updateClientUi;
            _damage = damage;
            _fight = fight;
        }

        public void updateCombat(Player player, Player target, Room room)
        {
            if (target != null)
            {
                if (target.IsAlive())
                {
                    _fight.InitFightStatus(player, target);
                }
            }
        }

        public string ReplacePlaceholders(string str, Player player, bool isTarget)
        {
            var newString = String.Empty;
            if (isTarget)
            {
                newString = str.Replace("#target#", "You");

                return newString;
            }

            newString = str.Replace("#target#", player.Name);

            return newString;
        }

        public void DamagePlayer(
            string spellName,
            int damage,
            Player player,
            Player target,
            Room room
        )
        {
            if (target.IsAlive())
            {
                var totalDam = _fight.CalculateSkillDamage(player, target, damage);

                _writer.WriteLine(
                    $"<p>Your {spellName} {_damage.DamageText(totalDam).Value} {target.Name}  <span class='damage'>[{damage}]</span></p>",
                    player
                );
                _writer.WriteLine(
                    $"<p>{player.Name}'s {spellName} {_damage.DamageText(totalDam).Value} you!  <span class='damage'>[{damage}]</span></p>",
                    target
                );

                foreach (var pc in room.Players)
                {
                    if (
                        pc.ConnectionId.Equals(player.ConnectionId)
                        || pc.ConnectionId.Equals(target.ConnectionId)
                    )
                    {
                        continue;
                    }

                    _writer.WriteLine(
                        $"<p>{player.Name}'s {spellName} {_damage.DamageText(totalDam).Value} {target.Name}  <span class='damage'>[{damage}]</span></p>",
                        pc
                    );
                }

                target.Attributes.Attribute[EffectLocation.Hitpoints] -= totalDam;

                if (!target.IsAlive())
                {
                    _fight.TargetKilled(player, target, room);

                    _updateClientUi.UpdateHP(target);
                    return;
                    //TODO: create corpse, refactor fight method from combat.cs
                }

                //update UI
                _updateClientUi.UpdateHP(target);

                target.AddToCombat();
                player.AddToCombat();
            }
        }

        public void UpdateClientUI(Player player)
        {
            //update UI
            _updateClientUi.UpdateHP(player);
            _updateClientUi.UpdateMana(player);
            _updateClientUi.UpdateMoves(player);
            _updateClientUi.UpdateScore(player);
        }

        public void EmoteAction(Player player, Player target, Room room, SkillMessage emote)
        {
            if (target.ConnectionId == player.ConnectionId)
            {
                _writer.WriteLine(
                    $"<p>{ReplacePlaceholders(emote.Hit.ToPlayer, target, true)}</p>",
                    target
                );
            }
            else
            {
                _writer.WriteLine(
                    $"<p>{ReplacePlaceholders(emote.Hit.ToPlayer, target, false)}</p>",
                    player
                );
            }

            if (!string.IsNullOrEmpty(emote.Hit.ToTarget))
            {
                _writer.WriteLine($"<p>{emote.Hit.ToTarget}</p>", target);
            }

            foreach (var pc in room.Players)
            {
                if (
                    pc.ConnectionId.Equals(player.ConnectionId)
                    || pc.ConnectionId.Equals(target.ConnectionId)
                )
                {
                    continue;
                }

                _writer.WriteLine(
                    $"<p>{ReplacePlaceholders(emote.Hit.ToRoom, target, false)}</p>",
                    pc
                );
            }
        }
    }
}
