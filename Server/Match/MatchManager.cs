using CitizenFX.Core;
using Newtonsoft.Json;
using Server.Map;
using Server.User;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    internal enum GameState : int
    {
        PlayerJoin,
        PlayerLeave,
        PlayerDeath,
        PlayerKill,
        PlayerOnHill,
        PlayerOffHill,
        PlayerTeamJoin,
        PlayerTeamLeave,
        TeamPoint,
        HillCaptured,
        HillLost,
        HillContested,
        GameFinished
    }


    internal class MatchManager
    {
        private const int VICTORY_POINT_THRESHOLD = 300;

        private readonly List<Team> Teams;
        private readonly SessionMap Map;

        private GameSession Session;

        private Team King = null;
        private bool Tied = false;
        private bool Finished = false;

        // Cached vector used in calculations.
        private Vector3 AOVec3;

        public Queue<string> State = new();

        public MatchManager(MapContainer _mapCfg, GameSession _gameSession)
        {
            var randidx = new Random().Next(0, _mapCfg.Maps.Count - 1);
            Map = _mapCfg.Maps[randidx];

            Teams = new(Map.Teams.Count());
            Session = _gameSession;

            var maxPlayerPerTeam = (int)Math.Ceiling(Session.CV_MAX_CLIENTS / (float)Map.Teams.Count);

            var tid = 0;

            Debug.WriteLine($"Capacity of teams: { Teams.Capacity }, maximum players per team { maxPlayerPerTeam }");

            Map.Teams.ForEach((t) => { Teams.Add(new Team(tid, t.Name, t, maxPlayerPerTeam)); tid++; });

            AOVec3 = new(Map.AO[0], Map.AO[1], Map.AO[2]);

        }

        internal float[] GetCurrentAO() { return Map.AO; }
        internal Vector3 GetCurrentAOVec3() { return AOVec3; }

        internal Team GetTeamById(int id)
        {
            return Teams.Find((t) => t.Id == id);
        }

        internal List<string> GetTeamNames()
        {
            return (from t in Teams select t.Name).ToList();
        }

        internal SessionMap GetCurrentMap()
        {
            return Map;
        }

        #region Team
        internal void AddFlagPointToTeam(Team team)
        {
            if (Finished)
                return;

            team.AddFlagPoint();

            QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.PlayerOnHill, teamId = team.Id, membersInZone = team.PlayersOnHill });

            if (King == null)
            {
                King = team;
                King.AddTeamPoint();

                QueueMatchUpdate(new object[2] {
                            new { type = "game_state_update",
                                    update_type = GameState.HillCaptured,
                                    by = King.Id },
                            new { type = "game_state_update",
                                    update_type = GameState.TeamPoint,
                                    teamId = King.Id,
                                    points = King.Points }
                        });
            }

            if (King.Id != team.Id)
            {
                if (team.PlayersOnHill == King.PlayersOnHill)
                {
                    Tied = true;
                    Debug.WriteLine($"Hill contested {team.Name} ({ team.PlayersOnHill }) - {King.Name} ({ King.PlayersOnHill })");
                    QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.HillContested, pretenders = team.Id });
                }
                else if (team.PlayersOnHill > King.PlayersOnHill)
                {
                    Debug.WriteLine($"New king: { team.Name } (old king: { King.Name })");
                    King = team;
                    King.AddTeamPoint();

                    QueueMatchUpdate(new object[2] {
                            new { type = "game_state_update",
                                    update_type = GameState.HillCaptured,
                                    by = King.Id },
                            new { type = "game_state_update",
                                    update_type = GameState.TeamPoint,
                                    teamId = King.Id,
                                    points = King.Points }
                        });
                }
            }
        }

        internal void RemoveFlagPointFromTeam(Team team)
        {
            if (Finished)
                return;

            var newFlagPointCount = team.PlayersOnHill - 1;

            if (newFlagPointCount >= 0)
            {
                team.RemoveFlagPoint();
                if (team == King)
                {
                    var currMaxFlagPoint = Teams.Max((t) => t.PlayersOnHill);

                    if (currMaxFlagPoint > team.PlayersOnHill)
                    {
                        var newKing = Teams.Where(t => t.PlayersOnHill > team.PlayersOnHill);
                        if (newKing.Count() == 1)
                        {
                            King = newKing.First();
                            King.AddTeamPoint();

                            if (King.Points >= VICTORY_POINT_THRESHOLD)
                            {
                                // todo handle victory
                            }

                            QueueMatchUpdate(new object[2] {
                                new { type = "game_state_update",
                                      update_type = GameState.HillCaptured,
                                      by = King.Id },
                                new { type = "game_state_update",
                                      update_type = GameState.TeamPoint,
                                      teamId = King.Id,
                                      points = King.Points }
                            });
                        }
                        else
                        {
                            Tied = true;
                            QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.HillContested, pretenders = newKing.ToArray() });
                        }
                    }
                }
            }
            else
            {
                team.SetFlagPoints(0);
            }

            QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.PlayerOffHill, teamId = team.Id, membersInZone = team.PlayersOnHill });
        }

        internal void AddPoinToTeam(Team t)
        {
            t.AddTeamPoint();
        }

        public bool JoinTeam(KothPlayer player, int teamId)
        {
            var team = GetTeamById(teamId);

            if (player.Team != null)
                player.Team.Leave(player);

            var status = team.Join(player);

            if (status)
            {
                QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.PlayerTeamJoin, teamId = team.Id });
            }

            return status;
        }

        public bool LeaveTeam(KothPlayer player)
        {
            var prevTeamId = player.Team.Id;
            var status = player.Team.Leave(player);
            if (status)
            {
                QueueMatchUpdate(new { type = "game_state_update", update_type = GameState.PlayerTeamLeave, teamId = prevTeamId });
            }
            return status;
        }

        #endregion Team

        internal void QueueMatchUpdate(object obj)
        {
            var _obj = JsonConvert.SerializeObject(obj);
            Debug.WriteLine(_obj);
            State.Enqueue(_obj);
        }

        internal void QueueMatchUpdate(object[] objs)
        {
            foreach (var obj in objs)
            {
                var _obj = JsonConvert.SerializeObject(obj);
                Debug.WriteLine(_obj);
                State.Enqueue(_obj);
            }
        }

        #region Player
        internal void AddDeathToPlayer(KothPlayer p)
        {
            p.AddDeath();
        }

        internal void AddKillToPlayer(KothPlayer p, int killReward = 800)
        {
            p.AddKill();
            AddMoneyToPlayer(p, killReward);
            AddPoinToTeam(p.Team);
        }

        internal void AddMoneyToPlayer(KothPlayer p, int amount)
        {
            p.AddMoney(amount);
        }

        internal void AddExperienceToPlayer(KothPlayer p, float amount)
        {
            p.AddExperience(amount);
        }

        internal void AddLevelToPlayer(KothPlayer p)
        {
            p.AddLevel();
        }

        internal void AddPointsToPlayer(KothPlayer p, int amount = 10)
        {
            p.AddPoints(amount);
        }
        #endregion Player
    }

}
