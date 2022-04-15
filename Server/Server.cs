using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Server.Map;
using Newtonsoft.Json;
using Server.User;
using static CitizenFX.Core.Native.API;

namespace Server
{

    internal class Server : BaseScript
    {
        private static readonly Dictionary<Player, ServerPlayer> PlayersInGame = new();
        public static MatchManager Match;

        public Server ( )
        {
            Debug.WriteLine($"Creating KOTH environment...");

            var config = LoadResourceFile(GetCurrentResourceName(), "config/gamemode.json");

            var mapCfg = JsonConvert.DeserializeObject<MapContainer>(config);

            Match = new(mapCfg);

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
            PlayersInGame.Add(p, new ServerPlayer(p));
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
                    PlayersInGame.Add(p, new ServerPlayer(p));
                }
            }
        }

        static internal ServerPlayer GetPlayerByPlayerObj ( Player player )
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

            var killerEnt = GetPedSourceOfDeath(player.Character.Handle);
            var killerPlayerObj = Players[NetworkGetNetworkIdFromEntity(killerEnt)];

            Debug.WriteLine($"({killerEnt}) Name: { killerPlayerObj.Name }, Type: { GetEntityType(killerEnt) }");

            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");

                if (v.Key == "killerinveh" && v.Value.ToString().Equals("True"))
                {
                    Debug.WriteLine("Player got run over.");
                }
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
                
                if (p.IsInsideSafeZone && DoesEntityExist(p.Base.Character.Handle))
                {
                    var pHandle = p.Base.Character.Handle;
                    var maxHealth = GetEntityMaxHealth(pHandle);
                    var currHealth = GetEntityHealth(pHandle);
                    if (currHealth < maxHealth)
                    {
                        p.Base.TriggerEvent("koth:safeHeal", (int)Utils.lerp(currHealth + 5, maxHealth, 0.1f));
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
                string data = Match.StateQueue.Dequeue();
                Debug.WriteLine($"Sending state update to client: { data }");
                BroadcastEvent("koth:StateUpdate", data);

            } catch (InvalidOperationException)
            {}

            await Delay(500);
        }
    }
}