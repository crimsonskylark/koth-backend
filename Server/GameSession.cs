using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Server.Map;
using Newtonsoft.Json;
using Server.User;
using System.Reflection;
using Serilog;

using static CitizenFX.Core.Native.API;
using Serilog.Core;
using Serilog.Events;

namespace Server
{

    internal class GameSession : BaseScript
    {
        private static Dictionary<Player, KothPlayer> PlayersInGame;
        public static MatchManager Match;
        public static ILogger Logger = null;

        internal readonly int CV_MAX_CLIENTS = GetConvarInt("sv_maxclients", 64);

        public GameSession()
        {
            var config = LoadResourceFile(GetCurrentResourceName(), "config/gamemode.json");

            var mapCfg = JsonConvert.DeserializeObject<MapContainer>(config);

            PlayersInGame = new(CV_MAX_CLIENTS);

            Match = new(mapCfg, this);

            EventHandlers["playerDropped"] += new Action<Player, string>(Events.Game.OnPlayerDropped);
            EventHandlers["onServerResourceStart"] += new Action<string>(RebuildPlayerList);

            EventHandlers["koth:playerKilledByPlayer"] += new Action<Player, int, int, bool>(Events.Koth.OnPlayerKilledByPlayer);

            //EventHandlers["baseevents:onPlayerKilled"] += new Action<Player, int, ExpandoObject>(OnPlayerKilled);

            EventHandlers["koth:playerTeamJoin"] += new Action<Player, string>(Events.Koth.OnTeamJoin);

            EventHandlers["koth:playerFinishSetup"] += new Action<Player>(Events.Koth.OnPlayerFinishSetup);

            EventHandlers["koth:playerInsideSafeZone"] += new Action<Player>(Events.Koth.OnPlayerInsideSafeZone);
            EventHandlers["koth:playerOutsideSafeZone"] += new Action<Player>(Events.Koth.OnPlayerOutsideSafeZone);

            EventHandlers["koth:playerInsideCombatZone"] += new Action<Player>(Events.Koth.OnPlayerInsideCombatZone);
            EventHandlers["koth:playerOutsideCombatZone"] += new Action<Player>(Events.Koth.OnPlayerOutsideCombatZone);

#if DEBUG
            var log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new KothLogSink(null))
                .CreateLogger();
#else
            var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(new KothLogSink(null))
                .CreateLogger();
#endif

            Log.Logger = log;
            Logger = Log.Logger;

            Logger.Information("Successfuly started the King of the Hill game mode.");
        }

        public class KothLogSink : ILogEventSink
        {
            private readonly IFormatProvider _formatProvider;

            public KothLogSink(IFormatProvider formatProvider)
            {
                _formatProvider = formatProvider;
            }

            public void Emit(LogEvent logEvent)
            {
                var message = logEvent.RenderMessage(_formatProvider);
                Debug.WriteLine($"{DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)} {message}");
            }
        }

        [EventHandler("playerJoining")]
        private void AddPlayerToPlayerList([FromSource] Player p)
        {
            Logger.Debug($"Added player to player list: { p.Handle }");
            PlayersInGame.Add(p, new KothPlayer(p));
            Match.QueueMatchUpdate(new
            {
                type = "game_state_update",
                update_type = GameState.PlayerJoin,
                name = p.Name
            });
        }

        private void RebuildPlayerList(string name)
        {
            /*
             * This is useful during development so it's unecessary to
             * leave and re-join the server after restarting the resource.
             */
            if (name.Equals(GetCurrentResourceName()))
            {
                foreach (var p in Players)
                {
                    Logger.Debug($"Adding { p.Name } to \"PlayersInGame\".");
                    PlayersInGame.Add(p, new KothPlayer(p));
                }
            }
        }

        static internal KothPlayer GetKothPlayerByPlayerObj(Player player)
        {
            return PlayersInGame.ContainsKey(player) ? PlayersInGame[player] : null;
        }

        static internal bool RemovePlayerFromPlayerList(Player player)
        {
            Logger.Debug($"Removing player object for \"{player.Name}\".");
            return PlayersInGame.Remove(player);
        }

        static internal void BroadcastEvent(string ev_name, params object[] args)
        {
            TriggerClientEvent(ev_name, args);
        }

        [Tick]
        private async Task HealSafePlayers()
        {
            try
            {
                foreach (var _p in Players)
                {
                    // still loading into the game
                    if (_p.Character == null)
                    {
                        continue;
                    }

                    var p = GetKothPlayerByPlayerObj(_p);

                    if (p.IsInsideSafeZone && DoesEntityExist(p.Citizen.Character.Handle))
                    {
                        var pHandle = p.Citizen.Character.Handle;
                        var maxHealth = GetEntityMaxHealth(pHandle);
                        var currHealth = GetEntityHealth(pHandle);
                        if (currHealth < maxHealth)
                        {
                            Logger.Debug($"Healing \"{_p.Name}\" as they are in the safe zone.");
                            p.Citizen.TriggerEvent("koth:safeHeal", (int)Utils.lerp(currHealth + 5, maxHealth, 0.1f));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{MethodBase.GetCurrentMethod().Name}] Exception: { ex.Message }");
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


            }
            catch (InvalidOperationException)
            { }

            await Delay(250);
        }
    }
}