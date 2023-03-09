using System;
using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Communication;

public class SayToCmd : ICommand
{
    public SayToCmd(ICore core)
    {
        Aliases = new[] {"sayto", ">"};
        Description = "Says something directed to a player. This is useful in a room full of people.";
        Usages = new[] {"Type: sayto john what ever you want"};
        Title = "";
        DeniedStatus = new[]
        {
            CharacterStatus.Status.Busy,
            CharacterStatus.Status.Dead,
            CharacterStatus.Status.Fleeing,
            CharacterStatus.Status.Incapacitated,
            CharacterStatus.Status.Sleeping,
            CharacterStatus.Status.Stunned
        };
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
        if (string.IsNullOrEmpty(input.ElementAtOrDefault(1)))
        {
            Core.Writer.WriteLine("<p>Say what?</p>", player.ConnectionId);
            return;
        }
        
        var text = string.Join(" ", input.Skip(2));

        //find target
        var sayTo = room.Players.FirstOrDefault(x => x.Name.StartsWith(input[1], StringComparison.CurrentCultureIgnoreCase));

        if (sayTo == null)
        {
            Core.Writer.WriteLine("<p>They are not here.</p>", player.ConnectionId);
            return;
        }

        Core.Writer.WriteLine($"<p class='say'>You say to {sayTo.Name}, {text}</p>", player.ConnectionId);
        Core.UpdateClient.UpdateCommunication(player, $"<p class='say'>You say to {sayTo.Name}, {text}</p>", "room");
        
        foreach (var pc in room.Players.Where(pc => pc.Name != player.Name))
        {
            if (pc.Name == sayTo.Name)
            {
                Core.Writer.WriteLine($"<p class='say'>{player.Name} says to you, {text}</p>", pc.ConnectionId);
                Core.UpdateClient.UpdateCommunication(pc, $"<p class='say'>{player.Name} says to you, {text}</p>", "room");
            }
            else
            {
                Core.Writer.WriteLine($"<p class='say'>{player.Name} says to {sayTo.Name}, {text}</p>", pc.ConnectionId);
                Core.UpdateClient.UpdateCommunication(pc, $"<p class='say'>{player.Name} says to {sayTo.Name}, {text}</p>", "room");
            }
        }
    }
}