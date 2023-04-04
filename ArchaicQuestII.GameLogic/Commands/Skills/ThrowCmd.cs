using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Skill.Model;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Skills
{
    public class ThrowCmd :  SkillCore, ICommand
    {
        public ThrowCmd(ICore core): base (core)
        {
            Aliases = new[] { "throw" };
            Description = "Throw an item at your target.";
            Usages = new[] { "Type: throw sword bob, throw potion bob" };
            DeniedStatus = new [] { 
                CharacterStatus.Status.Sleeping, 
                CharacterStatus.Status.Resting, 
                CharacterStatus.Status.Dead, 
                CharacterStatus.Status.Mounted, 
                CharacterStatus.Status.Stunned };
            Title = DefineSkill.Throw().Name;
            UserRole = UserRole.Player;
            Core = core;
        }

        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public string Title { get; }
        public CharacterStatus.Status[] DeniedStatus { get; }
        public UserRole UserRole { get; }
        public ICore Core { get; }

        public void Execute(Player player, Room room, string[] input)
        {
            var canDoSkill = CanPerformSkill(DefineSkill.Throw(), player);

            if (!canDoSkill)
            { 
                return;
            }

            var obj = input.ElementAtOrDefault(1);

            if (string.IsNullOrEmpty(obj))
            {
                Core.Writer.WriteLine("<p>Throw what?</p>", player.ConnectionId);
                return;
            }
            
            var findNth = Helpers.findNth(obj);
            var thrownObj = Helpers.findObjectInInventory(findNth, player);

            if (thrownObj == null)
            {
                Core.Writer.WriteLine("<p>You can't find that.</p>", player.ConnectionId);
                return;
            }
          
            var target = FindTargetInRoom(input.ElementAtOrDefault(2)?.ToLower() ?? player.Target, room, player);

            if (target == null)
            {
                return;
            }
            
            var textToTarget = string.Empty;
            var textToRoom = string.Empty;

            player.Inventory.Remove(thrownObj);
            Core.UpdateClient.UpdateInventory(player);

            room.Items.Add(thrownObj);

            if (SkillSuccess(player, DefineSkill.Throw()) && DexterityAndLevelCheck(player, target) == true)
            {
                Core.Writer.WriteLine($"You throw {thrownObj.Name} at {target.Name}!", player.ConnectionId);
                textToTarget = $"{player.Name} throws {thrownObj.Name} at you!";
                textToRoom = $"{player.Name} throws {thrownObj.Name} at {target.Name}!";
                
                EmoteAction(textToTarget, textToRoom, target.Name, room, player);

                if(thrownObj.ItemType == Item.Item.ItemTypes.Potion)
                {
                    room.Items.Remove(thrownObj);
                    //TODO: Fix this
                    //_castSpell.CastSpell(thrownObj.SpellName, string.Empty, target, thrownObj.SpellName, dummyPlayer, room, false);
                }
                else
                {
                    var str = player.Attributes.Attribute[EffectLocation.Strength];
                    var damage = DiceBag.Roll(1, 1, 6) + str / 5;
                }
            }
            else
            {
                Core.Writer.WriteLine($"You try to throw {thrownObj.Name} but {target.Name} dodges it in time.", player.ConnectionId);
                textToTarget = $"You dodge {player.Name} thrown {thrownObj.Name}.";
                textToRoom = $"{target.Name} dodges {player.Name} thrown {thrownObj.Name}!";

                EmoteAction(textToTarget, textToRoom, target.Name, room, player);
                Core.Writer.WriteLine(Helpers.SkillLearnMistakes(player, DefineSkill.Throw().Name, Core.Gain), player.ConnectionId);
            }
            
            player.Lag += 1;

            updateCombat(player, target, room);
            
        }
    }

}