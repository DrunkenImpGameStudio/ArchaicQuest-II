﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Item;
using MoonSharp.Interpreter;

namespace ArchaicQuestII.GameLogic.World.Room
{
    public class Room
    {
        public enum TerrainType
        {
            Inside, //no weather
            City,
            Field,
            Forest,
            Hills,
            Mountain,
            Water,
            Underwater,
            Air,
            Desert,
            Underground //no weather
        }

        public enum RoomType
        {
            Standard = 0,
            Shop = 1 << 1,
            Guild = 1 << 2,
            Town = 1 << 3,
            Water = 1 << 4,
            River = 1 << 5,
            Sea = 1 << 6,
            PointOfInterest = 1 << 7,
            Field = 1 << 8,
            Forest = 1 << 9,
            Desert = 1 << 10,
            Inside = 1 << 11,
            Underground = 1 << 12
        }

        public enum RoomFlag
        {
            Donation = 0,
            Healing,
        }

        public int Id { get; set; }
        public bool Deleted { get; set; }
        public int AreaId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// List of available exits
        /// North, East, West, South, Up, and Down
        /// </summary>
        public ExitDirections Exits { get; set; } = new();
        public Coordinates Coords { get; set; } = new();
        public List<Player> Players { get; set; } = new();
        public List<Player> Mobs { get; set; } = new();
        public ItemList Items { get; set; } = new();
        public RoomType? Type { get; set; } = RoomType.Standard;
        public TerrainType? Terrain { get; set; } = TerrainType.City;

        /// <summary>
        /// List of emotes that will be randomly played on tick
        /// </summary>
        public List<string> Emotes { get; set; } = new();

        /// <summary>
        /// Room descriptions will contain nouns which should be
        /// extended with a keyword so a player can examine 'noun' or
        /// look 'noun' for more information about an object mentioned
        /// in the room description
        /// </summary>
        public List<RoomObject> RoomObjects { get; set; } = new();

        /// <summary>
        /// Has the room been touched or not
        /// </summary>
        public bool? Clean { get; set; } = true;

        /// <summary>
        /// When room re-populates we may want to send
        /// an emote to any players in the room
        /// </summary>
        public string UpdateMessage { get; set; }

        /// <summary>
        /// Does this repop every tick
        /// </summary>
        public bool InstantRePop { get; set; }
        public bool IsLit { get; set; }

        /// <summary>
        /// Set flags for rooms
        /// </summary>
        public List<RoomFlag> RoomFlags { get; set; } = new List<RoomFlag>();
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public async Task PlayerEntered(Player player)
        {
            foreach (var mob in Mobs.ToList())
            {
                await Task.Delay(500);

                if (!string.IsNullOrEmpty(mob.Events.Enter))
                {
                    try
                    {
                        UserData.RegisterType<MobScripts>();

                        var script = new Script();

                        var obj = UserData.Create(Services.Instance.MobScripts);
                        script.Globals.Set("obj", obj);
                        UserData.RegisterProxyType<MyProxy, Room>(r => new MyProxy(this));
                        UserData.RegisterProxyType<ProxyPlayer, Player>(
                            r => new ProxyPlayer(player)
                        );

                        script.Globals["room"] = this;
                        script.Globals["player"] = player;
                        script.Globals["mob"] = mob;

                        var res = script.DoString(mob.Events.Enter);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("RoomActions.cs: " + ex);
                    }
                }

                if (mob.Aggro && mob.Status != CharacterStatus.Status.Fighting)
                {
                    Services.Instance.Writer.WriteLine($"{mob.Name} attacks you!", player);
                    Services.Instance.MobScripts.AttackPlayer(this, player, mob);
                }
            }
        }

        public void PlayerExited(Player player)
        {
            foreach (var mob in Mobs.Where(mob => !string.IsNullOrEmpty(mob.Events.Leave)))
            {
                UserData.RegisterType<MobScripts>();

                var script = new Script();

                var obj = UserData.Create(Services.Instance.MobScripts);
                script.Globals.Set("obj", obj);
                UserData.RegisterProxyType<MyProxy, Room>(r => new MyProxy(this));
                UserData.RegisterProxyType<ProxyPlayer, Player>(r => new ProxyPlayer(player));

                script.Globals["room"] = this;
                script.Globals["player"] = player;
                script.Globals["mob"] = mob;

                var res = script.DoString(mob.Events.Leave);
            }
        }
    }
}
