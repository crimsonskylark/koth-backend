using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using koth_server.Map;
using Newtonsoft.Json;
using Server.User;
using static CitizenFX.Core.Native.API;

namespace Server
{
    internal enum StateUpdate : int
    {
        PlayerJoin,
        PlayerLeave,
        PlayerDeath,
        PlayerKill,
        PlayerOnHill,
        TeamJoin,
        TeamLeave,
        TeamPoint,
        HillCaptured,
        HillLost,
        HillContested
    }

    internal class Server : BaseScript
    {
        private static readonly Dictionary<Player, KothPlayer> KothPlayerList = new();
        private static readonly List<KothTeam> Teams = new();

        private static Map SessionMap = new();

        public Server ( )
        {
            Debug.WriteLine($"Creating KOTH environment...");

            var config = LoadResourceFile(GetCurrentResourceName(), "config/gamemode.json");

            var map_config = JsonConvert.DeserializeObject<MapContainer>(config);

            var r = new Random().Next(0, map_config.Maps.Count() - 1);
            SessionMap = map_config.Maps[r];

            int team_id = 0;

            foreach (var t in SessionMap.Teams)
            {
                Teams.Add(new KothTeam(team_id, t.Name, t));
                team_id += 1;
            }

            EventHandlers["playerDropped"] += new Action<Player, string>(Events.Game.OnPlayerDropped);
            EventHandlers["onServerResourceStart"] += new Action<string>(RebuildPlayerList);
            EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, ExpandoObject>(Events.Game.OnPlayerKilled);

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
            KothPlayerList.Add(p, new KothPlayer(p));
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
                    KothPlayerList.Add(p, new KothPlayer(p));
                }
            }
        }

        static internal KothPlayer GetPlayerByPlayerObj ( Player player )
        {
            return KothPlayerList.ContainsKey(player) ? KothPlayerList[player] : null;
        }

        static internal bool RemovePlayerFromPlayerList ( Player player )
        {
            return KothPlayerList.ContainsKey(player) && KothPlayerList.Remove(player);
        }

        static internal void BroadcastEvent ( string ev_name, params object[] args )
        {
            TriggerClientEvent(ev_name, args);
        }

        static internal KothTeam GetTeamById ( int id )
        {
            return Teams.Find(( t ) => t.Id == id);
        }

        static internal Map GetCurrentMap ( )
        {
            return SessionMap;
        }

        static internal float[] GetCurrentCombatZone ( )
        {
            return SessionMap.AO;
        }

        static private float lerp(int v0, int v1, float t)
        {
            return ( 1 - t ) * v0 + t * v1; 
        }

        [Tick]
        private async Task HealSafePlayers ()
        {
            foreach (var p in KothPlayerList.Values)
            {
                var playerHandle = p.Base.Character.Handle;
                if (p.IsInsideSafeZone && DoesEntityExist(playerHandle))
                {
                    var maxHealth = GetEntityMaxHealth(playerHandle);
                    var currHealth = GetEntityHealth(playerHandle);
                    if (currHealth < maxHealth)
                    {
                        p.Base.TriggerEvent("koth:safeHeal", (int)lerp(currHealth + 5, maxHealth, 0.0f));
                    }
                }
            }
            await Delay(500);
        }
    }
}