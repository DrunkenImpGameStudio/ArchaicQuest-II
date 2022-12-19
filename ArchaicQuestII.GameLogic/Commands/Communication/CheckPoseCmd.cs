using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Communication
{
    public class CheckPoseCmd : ICommand
    {
        public CheckPoseCmd(ICore core)
        {
            Aliases = new[] {"checkpose"};
            Description = "Shows you what your characters current pose is";
            Usages = new[] {"Type: checkpose"};
            DeniedStatus = new[]
            {
                CharacterStatus.Status.Busy,
                CharacterStatus.Status.Dead,
                CharacterStatus.Status.Fighting,
                CharacterStatus.Status.Ghost,
                CharacterStatus.Status.Fleeing,
                CharacterStatus.Status.Incapacitated,
                CharacterStatus.Status.Sleeping,
                CharacterStatus.Status.Stunned,
            };
            UserRole = UserRole.Player;
            Core = core;
        }
        
        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public CharacterStatus.Status[] DeniedStatus { get; }
        public UserRole UserRole { get; }
        public ICore Core { get; }


        public void Execute(Player player, Room room, string[] input)
        {
            var poseText = string.Empty;

            poseText = string.IsNullOrEmpty(player.LongName) ? $"{ player.Name}" : $"{ player.Name} {player.LongName}";

            if (!string.IsNullOrEmpty(player.Mounted.Name))
            {
                poseText += $", is riding {player.Mounted.Name}";
            }
            else if (string.IsNullOrEmpty(player.LongName))
            {
                poseText += " is here";

            }

            poseText += player.Pose;

            Core.Writer.WriteLine(poseText, player.ConnectionId);
        }
    }
}