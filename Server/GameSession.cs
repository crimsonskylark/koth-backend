using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Server.Map;
using Newtonsoft.Json;
using Server.User;
using static CitizenFX.Core.Native.API;

namespace Server
{

    internal class GameSession : BaseScript
    {
        private static Dictionary<Player, KothPlayer> PlayersInGame;
        public static MatchManager Match;

        internal readonly int CV_MAX_CLIENTS = GetConvarInt("sv_maxclients", 64);

        public GameSession ( )
        {
            Debug.WriteLine($"Creating KotH environment...");

            var config = LoadResourceFile(GetCurrentResourceName(), "config/gamemode.json");

            var mapCfg = JsonConvert.DeserializeObject<MapContainer>(config);

            PlayersInGame = new(CV_MAX_CLIENTS);

            Match = new(mapCfg, this);

            EventHandlers["playerDropped"] += new Action<Player, string>(Events.Game.OnPlayerDropped);
            EventHandlers["onServerResourceStart"] += new Action<string>(RebuildPlayerList);

            EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, ExpandoObject>(OnPlayerKilled);

            EventHandlers["koth:playerTeamJoin"] += new Action<Player, string>(Events.Koth.OnTeamJoin);
            
            EventHandlers["koth:playerFinishSetup"] += new Action<Player>(Events.Koth.OnPlayerFinishSetup);

            EventHandlers["koth:playerInsideSafeZone"] += new Action<Player>(Events.Koth.OnPlayerInsideSafeZone);
            EventHandlers["koth:playerOutsideSafeZone"] += new Action<Player>(Events.Koth.OnPlayerOutsideSafeZone);

            EventHandlers["koth:playerInsideCombatZone"] += new Action<Player>(Events.Koth.OnPlayerInsideCombatZone);
            EventHandlers["koth:playerOutsideCombatZone"] += new Action<Player>(Events.Koth.OnPlayerOutsideCombatZone);
        }

        [EventHandler("playerJoining")]
        private void AddPlayerToPlayerList ( [FromSource] Player p )
        {
            Debug.WriteLine($"Added player to player list: { p.Handle }");
            PlayersInGame.Add(p, new KothPlayer(p));
            Match.QueueMatchUpdate(new
            {
                type = "game_state_update",
                update_type = GameState.PlayerJoin,
                name = p.Name
            });
        }

        private void RebuildPlayerList ( string name )
        {
            /*
             * This is useful during development so it's unecessary to
             * leave and re-join the server after restarting the resource.
             */
            if (name.Equals(GetCurrentResourceName()))
            {
                foreach (var p in Players)
                {
                    PlayersInGame.Add(p, new KothPlayer(p));
                }
            }
        }

        static internal KothPlayer GetPlayerByPlayerObj ( Player player )
        {
            return PlayersInGame.ContainsKey(player) ? PlayersInGame[player] : null;
        }

        static internal bool RemovePlayerFromPlayerList ( Player player )
        {
            return PlayersInGame.ContainsKey(player) && PlayersInGame.Remove(player);
        }

        static internal void BroadcastEvent ( string ev_name, params object[] args )
        {
            TriggerClientEvent(ev_name, args);
        }

        internal void OnPlayerKilled([FromSource] Player player, int killerType, ExpandoObject obj)
        {
            Debug.WriteLine("Player killed");
            
            var killerEnt = Entity.FromHandle(GetPedSourceOfDeath(player.Character.Handle));

            if (killerEnt != null)
            {
                if (killerEnt.GetType() == typeof(Ped))
                {
                    
                    var killer = GetPlayerByPlayerObj(killerEnt.Owner);
                    var deceased = GetPlayerByPlayerObj(player);

                    if (killer.Team != deceased.Team)
                    {
                        Match.AddKillToPlayer(killer);
                        Match.AddDeathToPlayer(deceased);

                        Debug.WriteLine($"Added kill to { killer.CfxPlayer.Name }, total { killer.SessionKills } ($800)");
                        Debug.WriteLine($"Added death to { deceased.CfxPlayer.Name }, total { deceased.SessionDeaths }");

                        Match.QueueMatchUpdate(new 
                        { 
                            type = "game_state_update", 
                            update_type = GameState.PlayerKill, 
                            killer = killer.CfxPlayer.Name, 
                            deceased = deceased.CfxPlayer.Name 
                        });

                    }

                } else if (killerEnt.GetType() == typeof(Vehicle))
                {
                    Debug.WriteLine($"Player was killed by vehicle.");
                }
                Debug.WriteLine($"Killer entity: { killerEnt }");
            }

            //SettlePlayerDeath(player);
        }

        [Tick]
        private async Task HealSafePlayers ()
        {
            foreach (var _p in Players)
            {
                // still loading into the game
                if (_p.Character == null)
                {
                    continue;
                }

                var p = GetPlayerByPlayerObj(_p);
                
                if (p.IsInsideSafeZone && DoesEntityExist(p.CfxPlayer.Character.Handle))
                {
                    var pHandle = p.CfxPlayer.Character.Handle;
                    var maxHealth = GetEntityMaxHealth(pHandle);
                    var currHealth = GetEntityHealth(pHandle);
                    if (currHealth < maxHealth)
                    {
                        p.CfxPlayer.TriggerEvent("koth:safeHeal", (int)Utils.lerp(currHealth + 5, maxHealth, 0.1f));
                    }
                }
            }
            await Delay(500);
        }

        [Tick]
        private async Task MainGameLoop()
        {
            try
            {
                string data = Match.State.Dequeue();
                BroadcastEvent("koth:StateUpdate", data);


            } catch (InvalidOperationException)
            {}

            await Delay(250);
        }
    }
}