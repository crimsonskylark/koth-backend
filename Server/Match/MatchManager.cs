using CitizenFX.Core;
using Newtonsoft.Json;
using Server.Map;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        TeamJoin,
        TeamLeave,
        TeamPoint,
        HillCaptured,
        HillLost,
        HillContested,
        GameFinished
    }

    internal class MatchManager
    {
        private static readonly List<Team> Teams = new();
        private static SessionMap CurrentMap = new();

        private readonly int VICTORY_POINT_THRESHOLD = 300;

        private int TotalPoints = 0;
        private Team CurrentKing;
        private bool IsGameTied = false;
        private bool IsGameFinished = false;

        // Cached vector used in calculations.
        private static Vector3 AOVec3 = new();

        public Queue<string> StateQueue = new();

        public MatchManager(MapContainer MapCfg)
        {

            var randidx = new Random().Next(0, MapCfg.Maps.Count - 1);
            CurrentMap = MapCfg.Maps[randidx];

            var tid = 0;

            CurrentMap.Teams.ForEach((t) => { Teams.Add(new Team(tid, t.Name, t)); tid++; });

            AOVec3.X = CurrentMap.AO[0];
            AOVec3.Y = CurrentMap.AO[1];
            AOVec3.Z = CurrentMap.AO[2];

        }

        internal float[] GetCurrentAO() { return CurrentMap.AO; }
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
            return CurrentMap;
        }

        internal void AddFlagPointToTeam(Team team)
        {
            if (IsGameFinished)
                return;

            if (team.Points < VICTORY_POINT_THRESHOLD)
            {
                team.AddFlagPoint();
                StateQueue.Enqueue(JsonConvert.SerializeObject(new { type = "game_state_update", update_type = GameState.PlayerOnHill, teamId = team.Id, membersInZone = team.PlayersOnHill }));
            }
        }

        internal void RemoveFlagPointFromTeam(Team team)
        {

            if (IsGameFinished)
                return;

            var newFlagPointCount = team.PlayersOnHill - 1;
            if (newFlagPointCount >= 0)
            {
                team.RemoveFlagPoint();
            }
            else
            {
                team.SetFlagPoints(0);
            }
            StateQueue.Enqueue(JsonConvert.SerializeObject(new { type = "game_state_update", update_type = GameState.PlayerOffHill, teamId = team.Id, membersInZone = team.PlayersOnHill }));
        }

        //internal void AddDeathToPlayer(int netid)
        //{
        //    player.AddKill();
        //}

        //internal void AddMoneyToPlayer(int netid)
        //{
        //    player.AddKill();
        //}
    }
}
