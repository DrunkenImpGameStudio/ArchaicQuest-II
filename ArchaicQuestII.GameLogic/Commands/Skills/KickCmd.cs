using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Skills
{
    public class KickCmd : SkillCore, ICommand
    {
        public KickCmd()
            : base()
        {
            Aliases = new[] { "kick", "kic" };
            Description =
                "Kicking allows the adventurer to receive an extra attack in combat, a powerful "
                + "kick. However, a failed kick may throw an unwary fighter off balance.";
            Usages = new[]
            {
                "Type: kick cow - kick the target, during combat only kick can be entered."
            };
            DeniedStatus = new[]
            {
                CharacterStatus.Status.Sleeping,
                CharacterStatus.Status.Resting,
                CharacterStatus.Status.Dead,
                CharacterStatus.Status.Mounted,
                CharacterStatus.Status.Stunned
            };
            Title = SkillName.Kick.ToString();
            UserRole = UserRole.Player;
        }

        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public string Title { get; }
        public CharacterStatus.Status[] DeniedStatus { get; }
        public UserRole UserRole { get; }

        public void Execute(Player player, Room room, string[] input)
        {
            if (!player.HasSkill(SkillName.Kick))
                return;

            var obj = input.ElementAtOrDefault(1)?.ToLower() ?? player.Target;
            if (string.IsNullOrEmpty(obj))
            {
                Services.Instance.Writer.WriteLine("Kick What!?.", player);
                return;
            }

            var target = FindTargetInRoom(obj, room, player);
            if (target == null)
            {
                return;
            }

            var textToTarget = string.Empty;
            var textToRoom = string.Empty;

            var skillSuccess = player.RollSkill(
                SkillName.Kick,
                true,
                "You miss your kick and stumble."
            );
            if (!skillSuccess)
            {
                textToTarget = $"{player.Name} tries to kick you but stumbles.";
                textToRoom = $"{player.Name} tries to kick {target.Name} but stumbles.";
                EmoteAction(textToTarget, textToRoom, target.Name, room, player);
                player.FailedSkill(SkillName.Kick, true);
                player.Lag += 1;
                return;
            }

            textToTarget = $"{player.Name} lashes out with a hard kick.";
            textToRoom = $"{player.Name} lands a strong kick to {target.Name}.";
            EmoteAction(textToTarget, textToRoom, target.Name, room, player);

            var damage =
                DiceBag.Roll(1, 1, 8) + player.Attributes.Attribute[EffectLocation.Strength] / 4;
            player.Lag += 1;

            DamagePlayer(SkillName.Kick.ToString(), damage, player, target, room);
        }
    }
}
