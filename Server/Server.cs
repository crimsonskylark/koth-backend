using CitizenFX.Core;
using koth_server.Map;
using Newtonsoft.Json;
using Server.User;
using Server.User.Classes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace Server
{
    enum StateUpdate : int
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

    class Server : BaseScript
    {
        readonly Dictionary<Player, KothPlayer> ServerPlayers = new();
        readonly List<KothTeam> Teams = new();
        readonly Map SessionMap = new();
        readonly uint DefaultWeapon = (uint)GetHashKey("weapon_compactrifle");
        readonly uint DefaultPed = (uint)GetHashKey("mp_m_freemode_01");
        public Server ( )
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");

            // All teams
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
        }

        #region GameEvents
        [EventHandler("playerJoining")]
        void OnPlayerJoining ( [FromSource] Player player, string _ )
        {
            Debug.WriteLine($"Player {player.Handle} has joined the server.");
            ServerPlayers[player] = new KothPlayer(player);
        }

        [EventHandler("playerDropped")]
        void OnPlayerDropped ( [FromSource] Player player, string reason )
        {
            if (ServerPlayers.TryGetValue(player, out KothPlayer p))
            {
                p.LeaveTime = DateTime.UtcNow;

                Debug.WriteLine($"Player {player.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");

                p.LeaveTeam();

                if (ServerPlayers.Remove(player))
                {
                    Debug.WriteLine($"Removed player {player.Handle} from player list.");
                }
                else
                {
                    Debug.WriteLine($"Failed to remove {player.Handle} from player list.");
                }
                /* TODO: Save player information in database */
            }
            else
            {
                /* Should never be reached in production. */
                Debug.WriteLine($"[!!!] Player not found.");
            }
        }

        [EventHandler("onResourceStart")]
        void OnResourceStart ( string name )
        {
            if (GetCurrentResourceName().Equals(name))
            {
                foreach (var p in Players)
                {
                    Debug.WriteLine($"Adding players to player list after restart.");
                    ServerPlayers.Add(p, new KothPlayer(p));
                }
            }
        }
        #endregion GameEvents

        #region BaseEvents

        [EventHandler("baseevents:onPlayerKilled")]
        private void OnPlayerKilled ( [FromSource] Player player, int killerType, ExpandoObject obj )
        {
            Debug.WriteLine("Player killed");
            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");
            }
        }

        #endregion BaseEvents

        #region KOTHEvents

        [EventHandler("koth:teamJoin")]
        private void OnTeamJoin ( [FromSource] Player player, string team_id )
        {
            var valid_team = int.TryParse(team_id, out int IntTeamId);

            if (string.IsNullOrEmpty(team_id) || !valid_team || IntTeamId < 0 || IntTeamId > 3)
            {
                Debug.WriteLine($"Invalid team: {IntTeamId}");
                return;
            }

            var team = Teams.Find((t) => t.Id == IntTeamId-1);

            if (ServerPlayers.TryGetValue(player, out KothPlayer _p) && _p.JoinTeam(team))
            {
                var teammates = (from p in team.Players
                                 where p != _p
                                 select NetworkGetEntityOwner(p.Base.Character.Handle)).ToArray();

                var spawn = _p.Team.Zone;

                player.TriggerEvent("koth:playerJoinedTeam", teammates, spawn.PlayerSpawnCoords, spawn.VehDealerCoords, spawn.VehDealerPropCoords, SessionMap.AO, DefaultPed);

                player.TriggerEvent("chat:addMessage", new { args = new[] { $"You are now part of team {team.Name}" } });
            } else
            {
                player.TriggerEvent("chat:addMessage", new { args = new[] { $"Failed to join team {team.Name}" } });
            }
        }

        [EventHandler("koth:playerFinishSetup")]
        void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            if (player != null && DoesEntityExist(player.Character.Handle) && IsPedAPlayer(player.Character.Handle))
            {
                var p = ServerPlayers[player];

                GiveWeaponToPed(player.Character.Handle,
                                DefaultWeapon,
                                350,
                                false,
                                true);
                SetPedArmour(player.Character.Handle, 100);

                var handle = p.Base.Character.Handle;
                var uniform = p.Team.Zone.Uniform;

                /* Mask */
                SetPedComponentVariation(handle, 1, uniform[0][0], uniform[0][1], 0);

                /* Gloves */
                SetPedComponentVariation(handle, 3, uniform[1][0], uniform[1][1], 0);

                /* Lower body */
                SetPedComponentVariation(handle, 4, uniform[2][0], uniform[2][1], 0);

                /* Shoes */
                SetPedComponentVariation(handle, 6, uniform[3][0], uniform[3][1], 0);

                /* Shirt */
                SetPedComponentVariation(handle, 8, uniform[4][0], uniform[4][1], 0);

                /* Jacket */
                SetPedComponentVariation(handle, 11, uniform[5][0], uniform[5][1], 0);
            }


        }

        [EventHandler("koth:playerInsideSafeZone")]
        void OnPlayerInsideSafeZone ( [FromSource] Player player )
        {
            Debug.WriteLine($"Player { player.Handle } inside safe zone.");
        }

        [EventHandler("koth:playerOutsideSafeZone")]
        void OnPlayerOutsideSafeZone ( [FromSource] Player player )
        {
            Debug.WriteLine($"Player { player.Handle } left safe zone.");
        }

        [EventHandler("koth:playerInsideCombatZone")]
        void OnPlayerInsideCombatZone ( [FromSource] Player player )
        {
            Debug.WriteLine($"Player { player.Handle } inside combat zone.");
        }

        [EventHandler("koth:playerOutsideCombatZone")]
        void OnPlayerOutsideCombatZone ( [FromSource] Player player )
        {
            Debug.WriteLine($"Player { player.Handle } left combat zone.");
        }

        #endregion KOTHEvents
    }
}
