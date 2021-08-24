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

    enum Job : int
    {
        Infantry = 1,
        Medic
    }
    class Server : BaseScript
    {
        readonly Dictionary<Player, KothPlayer> ServerPlayers = new();
        readonly List<KothTeam> Teams = new();
        readonly MapContainer MapConfig = new();

        public Server ( )
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");

            // All teams
            var config = LoadResourceFile(GetCurrentResourceName(), "config/maps.json");

            MapConfig = JsonConvert.DeserializeObject<MapContainer>(config);

            var r = new Random().Next(0, MapConfig.Maps.Count() - 1);
            var session = MapConfig.Maps[r];

            int team_id = 0;

            foreach (var t in session.Teams)
            {
                Teams.Add(new KothTeam(team_id, t.Name, t, t.InfantryModel));
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
            Debug.WriteLine($"onResourceStart");
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

            if (ServerPlayers.TryGetValue(player, out KothPlayer _p))
            {
                _p.JoinTeam(team);
                var teammates = (from p in team.Players
                                 where p.Base != player
                                 select NetworkGetEntityOwner(p.Base.Character.Handle)).ToArray();

                var spawn = _p.CurrentTeam.Zone;

                player.TriggerEvent("koth:playerJoinedTeam", teammates, spawn.PlayerSpawnCoords, spawn.VehDealerCoords, spawn.VehDealerPropCoords, _p.Class.Model);
                player.TriggerEvent("chat:addMessage", new { args = new[] { $"You are now part of team {team.Name}" } });

                return;
            }

            player.TriggerEvent("chat:addMessage", new { args = new[] { $"Failed to join team {team.Name}" } });
        }

        [EventHandler("koth:playerFinishSetup")]
        void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            if (DoesEntityExist(player.Character.Handle) && IsPedAPlayer(player.Character.Handle))
            {
                GiveWeaponToPed(player.Character.Handle,
                                ServerPlayers[player].Class.DefaultWeapon,
                                300,
                                false,
                                true);
                SetPedArmour(player.Character.Handle, 100);
            }
        }

        #endregion KOTHEvents
    }
}
