using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchaicQuestII.DataAccess;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Config;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ArchaicQuestII.GameLogic.Hubs
{
    public class GameHub : Hub
    {
        private readonly ILogger<GameHub> _logger;

        public GameHub(ILogger<GameHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Do action when user connects
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("SendMessage", "", "<p>Someone has entered the realm.</p>");
        }

        /// <summary>
        /// Do action when user disconnects
        /// </summary>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await Clients.All.SendAsync("SendMessage", "", "<p>Someone has left the realm.</p>");
        }

        /// <summary>
        /// get message from client
        /// </summary>
        /// <returns></returns>
        public void SendToServer(string message, string connectionId)
        {
            var player = Services.Instance.Cache.GetPlayer(connectionId);

            if (player == null)
            {
                Services.Instance.Hub.Clients
                    .Client(connectionId)
                    .SendAsync("SendMessage", "<p>Refresh the page to reconnect!</p>", "");
                return;
            }
            
            player.Buffer.Enqueue(message);
        }

        /// <summary>
        /// get content from client
        /// </summary>
        /// <returns></returns>
        public void CharContent(string message, string connectionId)
        {
            var player = Services.Instance.Cache.GetPlayer(connectionId);

            if (player == null)
            {
                Services.Instance.Hub.Clients
                    .Client(connectionId)
                    .SendAsync("SendMessage", "<p>Refresh the page to reconnect!</p>", "");
                return;
            }

            JObject json = JsonConvert.DeserializeObject<dynamic>(message);

            var contentType = json.GetValue("type")?.ToString();

            if (contentType == "book")
            {
                var book = new WriteBook()
                {
                    Description = json.GetValue("desc")?.ToString(),
                    PageNumber = int.Parse(json.GetValue("pageNumber")?.ToString()),
                    Title = json.GetValue("name")?.ToString()
                };

                var getBook = player.Inventory.FirstOrDefault(x => x.Name.Equals(book.Title));

                if (getBook == null)
                {
                    Services.Instance.Writer.WriteLine(
                        "<p>There's a puff of smoke and all your work is undone. Seek an Immortal</p>",
                        player
                    );
                    return;
                }

                getBook.Book.Pages[book.PageNumber] = book.Description;

                player.OpenedBook = getBook;
                Services.Instance.Writer.WriteLine(
                    "You have successfully written in your book.",
                    player
                );
            }

            if (contentType == "description")
            {
                player.Description = json.GetValue("desc")?.ToString();
                Services.Instance.Writer.WriteLine(
                    "You have successfully updated your description.",
                    player
                );
                Services.Instance.UpdateClient.UpdateScore(player);
            }
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <returns></returns>
        public async Task Send(string message)
        {
            _logger.LogInformation($"Player sent {message}");
            await Clients.All.SendAsync("SendMessage", "user x", message);
        }

        /// <summary>
        /// Send message to specific client
        /// </summary>
        /// <returns></returns>
        public async Task SendToClient(string message, string hubId)
        {
            _logger.LogInformation($"Player sent {message}");
            await Clients.Client(hubId).SendAsync("SendMessage", message);
        }

        public async Task CloseConnection(string message, string hubId)
        {
            await Clients.Client(hubId).SendAsync("Close", message);
        }

        public async void Welcome(string id)
        {
            // no longer used for ArchaicQuest but if you want to load an ascii MOTD uncomment
            //var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            //var directory = System.IO.Path.GetDirectoryName(location);
            //var motd = File.ReadAllText(directory + "/motd");

            var motd =
                "<img src='/assets/images/aqdragon.png' alt='ArchaicQuest Logo' class='motd' />";
            await SendToClient(motd, id);
        }

        public void CreateCharacter(string name = "Liam")
        {
            var newPlayer = new Player { Name = name };

            Services.Instance.PlayerDataBase.Save(newPlayer, PlayerDataBase.Collections.Players);
        }

        public async void AddCharacter(string hubId, Guid characterId)
        {
            var player = GetCharacter(hubId, characterId);
            if (!player.Skills.Any())
            {
                player.AddSkills(Enum.Parse<ClassName>(player.ClassName));
            }
            else
            {
                player.RemoveSkills(Enum.Parse<ClassName>(player.ClassName));
                player.UpdateSkillLevel(Enum.Parse<ClassName>(player.ClassName));
            }

            if (player.SubClassName != null && player.SubClassName != SubClassName.None.ToString())
            {
                if (!player.Skills.Any())
                {
                    player.AddSkills(Enum.Parse<ClassName>(player.SubClassName));
                }
                else
                {
                    player.RemoveSkills(Enum.Parse<ClassName>(player.SubClassName));
                    player.UpdateSkillLevel(Enum.Parse<ClassName>(player.SubClassName));
                }
            }

            player.Config ??= new PlayerConfig();

            foreach (var quest in player.QuestLog)
            {
                var updatedQuest = Services.Instance.Cache.GetQuest(quest.Id);
                quest.Type = updatedQuest.Type;
                quest.ItemsToGet = updatedQuest.ItemsToGet;
                quest.MobsToKill = updatedQuest.MobsToKill;
                quest.Title = updatedQuest.Title;
                quest.Description = updatedQuest.Description;
                quest.ExpGain = updatedQuest.ExpGain;
                quest.GoldGain = updatedQuest.GoldGain;
                quest.ItemGain = updatedQuest.ItemGain;
                quest.Area = updatedQuest.Area;
            }

            AddCharacterToCache(hubId, player);

            var playerExist = Services.Instance.Cache.PlayerAlreadyExists(player.Id);
            player.LastLoginTime = DateTime.Now;
            player.LastCommandTime = DateTime.Now;

            player.CommandLog.Add($"{DateTime.Now:f} - Logged in");
            var pcAccount = Services.Instance.PlayerDataBase.GetById<Account.Account>(
                player.AccountId,
                PlayerDataBase.Collections.Account
            );
            if (pcAccount != null)
            {
                pcAccount.DateLastPlayed = DateTime.Now;
                Services.Instance.PlayerDataBase.Save(
                    pcAccount,
                    PlayerDataBase.Collections.Account
                );
                var loginStats = new Account.AccountLoginStats
                {
                    AccountId = player.AccountId,
                    loginDate = DateTime.Now
                };

                Services.Instance.PlayerDataBase.Save(
                    loginStats,
                    PlayerDataBase.Collections.LoginStats
                );
            }

            await SendToClient($"<p>Welcome {player.Name}. Your adventure awaits you.</p>", hubId);

            if (playerExist != null)
            {
                Helpers.PostToDiscord($"{player.Name} has entered the realms.", "event");
                GetRoom(hubId, playerExist, playerExist.RoomId);
            }
            else
            {
                GetRoom(hubId, player);
            }
        }

        /// <summary>
        /// Find Character in DB and add to cache
        /// Check if player is already in cache
        /// if so kick em off
        /// </summary>
        /// <param name="hubId">string</param>
        /// <param name="characterId">guid</param>
        /// <returns>Player Character</returns>
        private Player GetCharacter(string hubId, Guid characterId)
        {
            var player = Services.Instance.PlayerDataBase.GetById<Player>(
                characterId,
                PlayerDataBase.Collections.Players
            );
            player.ConnectionId = hubId;
            player.LastCommandTime = DateTime.Now;
            player.LastLoginTime = DateTime.Now;
            player.Following = String.Empty;
            player.Followers = new List<Player>();
            player.Grouped = false;

            SetArmorRating(player);

            if (player.Attributes.Attribute[EffectLocation.Hitpoints] <= 0)
            {
                player.Attributes.Attribute[EffectLocation.Hitpoints] =
                    player.MaxAttributes.Attribute[EffectLocation.Hitpoints] / 4;
            }

            return player;
        }

        private void SetArmorRating(Player player)
        {
            player.ArmorRating.Armour = 0;
            player.ArmorRating.Magic = 0;
            foreach (var eq in player.Inventory.FindAll(x => x.Equipped.Equals(true)))
            {
                player.ArmorRating.Armour += eq.ArmourRating.Armour;
                player.ArmorRating.Magic += eq.ArmourRating.Magic;
            }
        }

        private async void AddCharacterToCache(string hubId, Player character)
        {
            var playerExist = Services.Instance.Cache.PlayerAlreadyExists(character.Id);

            if (playerExist != null)
            {
                // log char off
                await SendToClient(
                    $"<p style='color:red'>*** You have logged in elsewhere. ***</p>",
                    playerExist.ConnectionId
                );
                await this.CloseConnection(string.Empty, playerExist.ConnectionId);

                // remove from Services.Instance.Cache
                Services.Instance.Cache.RemovePlayer(playerExist.ConnectionId);

                playerExist.ConnectionId = hubId;
                Services.Instance.Cache.AddPlayer(hubId, playerExist);

                await SendToClient(
                    $"<p style='color:red'>*** You have been reconnected ***</p>",
                    hubId
                );
            }
            else
            {
                Services.Instance.Cache.AddPlayer(hubId, character);
            }
        }

        private void GetRoom(string hubId, Player character, string startingRoom = "")
        {
            //add to DB, configure from admin
            var roomid = !string.IsNullOrEmpty(startingRoom)
                ? startingRoom
                : Services.Instance.Config.StartingRoom;
            var room =
                Services.Instance.Cache.GetRoom(roomid)
                ?? Services.Instance.Cache.GetRoom(Services.Instance.Config.StartingRoom);
            character.RoomId = $"{room.AreaId}{room.Coords.X}{room.Coords.Y}{room.Coords.Z}";

            if (string.IsNullOrEmpty(character.RecallId))
            {
                var defaultRoom = Services.Instance.Config.StartingRoom;
                character.RecallId = defaultRoom;
            }

            var playerAlreadyInRoom =
                room.Players.ToList().FirstOrDefault(x => x.Id.Equals(character.Id)) != null;
            if (!playerAlreadyInRoom)
            {
                room.Players.Add(character);
                if (character.Pets.Any())
                {
                    foreach (var pet in character.Pets)
                    {
                        var petAlreadyInRoom =
                            room.Mobs.FirstOrDefault(x => x.Id.Equals(pet.Id)) != null;

                        if (!petAlreadyInRoom)
                        {
                            room.Mobs.Add(pet);
                            Services.Instance.Writer.WriteToOthersInRoom(
                                $"{pet.Name} suddenly appears.",
                                room,
                                character
                            );
                        }
                    }
                }

                Services.Instance.Writer.WriteToOthersInRoom(
                    $"{character.Name} suddenly appears.",
                    room,
                    character
                );
            }

            var rooms = Services.Instance.Cache.GetAllRoomsInArea(1);

            Services.Instance.UpdateClient.UpdateHP(character);
            Services.Instance.UpdateClient.UpdateMana(character);
            Services.Instance.UpdateClient.UpdateMoves(character);
            Services.Instance.UpdateClient.UpdateExp(character);
            Services.Instance.UpdateClient.UpdateEquipment(character);
            Services.Instance.UpdateClient.UpdateInventory(character);
            Services.Instance.UpdateClient.UpdateScore(character);
            Services.Instance.UpdateClient.UpdateQuest(character);
            Services.Instance.UpdateClient.UpdateAffects(character);
            Services.Instance.UpdateClient.UpdateTime(character);

            Services.Instance.UpdateClient.GetMap(
                character,
                Services.Instance.Cache.GetMap($"{room.AreaId}{room.Coords.Z}")
            );

            character.Buffer.Enqueue("look");

            foreach (
                var mob in room.Mobs.ToList().Where(mob => !string.IsNullOrEmpty(mob.Events.Enter))
            )
            {
                UserData.RegisterType<MobScripts>();

                var script = new Script();

                var obj = UserData.Create(Services.Instance.MobScripts);
                script.Globals.Set("obj", obj);
                UserData.RegisterProxyType<MyProxy, Room>(r => new MyProxy(room));
                UserData.RegisterProxyType<ProxyPlayer, Player>(r => new ProxyPlayer(character));

                script.Globals["room"] = room;
                script.Globals["player"] = character;
                script.Globals["mob"] = mob;

                var res = script.DoString(mob.Events.Enter);
            }

            //  return room;
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
