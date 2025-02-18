using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Character
{
    public class TrainCmd : ICommand
    {
        public TrainCmd()
        {
            Aliases = new[] { "train" };
            Description =
                "Train increases one of your attributes.  When you start the game, your\ncharacter has standard attributes based on your class, and several initial\ntraining sessions.  You can increase your attributes by using these sessions\nat a trainer.<br /><br /> It takes one training session to "
                + "improve an attribute by 1, or to increase mana or hp by 1-6.<br /><br />You receive one session per level.  The best attributes to train first are WIS and CON.<br /><br />WIS gives you more practice when you gain a level.  CON gives you more hit\npoints.  In the long run, your character will be most powerful if you train\nWIS and CON both to their maximum values before practicing or training";
            Usages = new[]
            {
                "Type: train <stat>, valid stats to train: str, int, wis, dex, con, hp, mana"
            };
            Title = "";
            DeniedStatus = new[]
            {
                CharacterStatus.Status.Busy,
                CharacterStatus.Status.Dead,
                CharacterStatus.Status.Fighting,
                CharacterStatus.Status.Ghost,
                CharacterStatus.Status.Fleeing,
                CharacterStatus.Status.Incapacitated,
                CharacterStatus.Status.Sleeping,
                CharacterStatus.Status.Stunned
            };
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
            var stat = input.ElementAtOrDefault(1);

            if (room.Mobs.Find(x => x.Trainer) == null)
            {
                Services.Instance.Writer.WriteLine("<p>You can't do that here.</p>", player);
                return;
            }

            if (player.Trains <= 0)
            {
                Services.Instance.Writer.WriteLine(
                    "<p>You have no training sessions left.</p>",
                    player
                );
                return;
            }

            if (string.IsNullOrEmpty(stat) || stat == "train")
            {
                Services.Instance.Writer.WriteLine(
                    $"<p>You have {player.Trains} training session{(player.Trains > 1 ? "s" : "")} remaining.<br />You can train: str dex con int wis cha hp mana move.</p>",
                    player
                );
            }
            else
            {
                var statName = Helpers.GetStatName(stat);
                if (string.IsNullOrEmpty(statName.Item1))
                {
                    Services.Instance.Writer.WriteLine(
                        $"<p>{stat} not found. Please choose from the following. <br /> You can train: str dex con int wis cha hp mana move.</p>",
                        player
                    );
                    return;
                }

                player.Trains -= 1;
                if (player.Trains < 0)
                {
                    player.Trains = 0;
                }

                if (statName.Item1 is "hit points" or "moves" or "mana")
                {
                    var hitDie = player.GetClass();
                    var roll = DiceBag.Roll(hitDie.HitDice);

                    player.MaxAttributes.Attribute[statName.Item2] += roll;
                    player.Attributes.Attribute[statName.Item2] += roll;

                    Services.Instance.Writer.WriteLine(
                        $"<p class='gain'>Your {statName.Item1} increases by {roll}.</p>",
                        player
                    );

                    Services.Instance.UpdateClient.UpdateHP(player);
                    Services.Instance.UpdateClient.UpdateMana(player);
                    Services.Instance.UpdateClient.UpdateMoves(player);
                }
                else
                {
                    player.MaxAttributes.Attribute[statName.Item2] += 1;
                    player.Attributes.Attribute[statName.Item2] += 1;

                    Services.Instance.Writer.WriteLine(
                        $"<p class='gain'>Your {statName.Item1} increases by 1.</p>",
                        player
                    );
                }

                Services.Instance.UpdateClient.UpdateScore(player);
            }
        }
    }
}
