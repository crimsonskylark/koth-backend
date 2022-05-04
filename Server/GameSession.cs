using CitizenFX.Core;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Server.Map;
using Server.Models;
using Server.User;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace Server
{

    internal class GameSession : BaseScript
    {
        private static Dictionary<Player, KothPlayer> ConnectedPlayers;

        internal static MatchManager Match;
        internal static ILogger Logger = null;


        internal readonly int CV_MAX_CLIENTS = GetConvarInt("sv_maxclients", 64);
        internal readonly string MYSQL_CONNECTION_STRING = GetConvar("mysql_connection_string", "Server=localhost;Port=3306;Database=koth;Uid=admin;Pwd=admin;");

        internal static MySqlConnection MySqlConn;

        readonly dynamic CardData = JsonConvert.DeserializeObject<dynamic>(LoadResourceFile(GetCurrentResourceName(), "config/cards/payload.json"));

        public GameSession()
        {
            var config = LoadResourceFile(GetCurrentResourceName(), "config/gamemode.json");
            var mapCfg = JsonConvert.DeserializeObject<MapContainer>(config);

            ConnectedPlayers = new(CV_MAX_CLIENTS);

            Match = new(mapCfg, this);

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

            MySqlConn = new MySqlConnection(MYSQL_CONNECTION_STRING);

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

            Logger.Information("Successfuly started the King of the Hill game mode.");
        }

        private class KothLogSink : ILogEventSink
        {
            private readonly IFormatProvider _formatProvider;

            public KothLogSink(IFormatProvider formatProvider)
            {
                _formatProvider = formatProvider;
            }

            public void Emit(LogEvent logEvent)
            {
                var message = logEvent.RenderMessage(_formatProvider);
                if (logEvent.Level == LogEventLevel.Error)
                {
                    // TODO: Send to ElasticSearch
                }
                Debug.WriteLine($"{DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)} {message}");
            }
        }

        [EventHandler("playerJoining")]
        private void AddPlayerToPlayerList([FromSource] Player p)
        {
            Logger.Debug($"Added player to player list: { p.Handle }");

            ConnectedPlayers.Add(p, new KothPlayer(p));
            Match.QueueMatchUpdate(new
            {
                type = "game_state_update",
                update_type = GameState.PlayerJoinLeave,
                name = p.Name,
                joining = true
            });

        }

        [EventHandler("playerConnecting")]
        private async void OnPlayerConnecting([FromSource] Player p, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            deferrals.defer();

            await Delay(0);

            //CardData["body"][0]["text"] = "Conectando...";
            //CardData["body"][1]["columns"][1]["items"][0]["text"] = $"Bem-vindo, {p.Name}!\rChecaremos o estado da sua conta e você será redirecionado automaticamente em seguida.";

            //deferrals.presentCard(JsonConvert.SerializeObject(CardData),
            //    new Action<dynamic, string>((data, rawData) =>
            //    {
            //        Debug.WriteLine(rawData);
            //    }));

            await Delay(10000);

            if (p.Identifiers["license"] != null)
            {
                deferrals.update($"Bem-vindo, {p.Name}!Checaremos o estado da sua conta e você será redirecionado automaticamente em seguida.");

                var playerLicense = new { license = p.Identifiers["license"] };

                try
                {
                    var accBanStatus = await MySqlConn.QuerySingleAsync<BanStatus>(@"SELECT Banned,BanDuration FROM player WHERE license=@license", playerLicense);
                    await Delay(0);

                    if (accBanStatus.Banned != 0)
                    {
                        if (accBanStatus.BanDuration != -1)
                        {
                            var now = DateTime.UtcNow.ToBinary();
                            if (now >= accBanStatus.BanDuration)
                            {
                                deferrals.update($"A sua punição expirou em { DateTime.FromBinary(accBanStatus.BanDuration) }");

                                var status = await MySqlConn.ExecuteAsync(@"UPDATE player SET Banned = 0, BanDuration = 0 WHERE license=@license", playerLicense);
                                await Delay(0);

                                if (status == 0)
                                {
                                    deferrals.done("Falha em remover sua punição. Entre em contato com a administração pelo Discord.");
                                }

                            }
                            else
                            {
                                deferrals.done($"Você foi banido do servidor. A sua punição expira em: { DateTime.FromBinary(accBanStatus.BanDuration) }");
                            }
                        }
                        else
                        {
                            deferrals.done("Você foi banido permanentemente.");
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    Logger.Information($"Player does not have an account. Creating.");

                    await Delay(0);

                    deferrals.update($"Estamos criando a sua conta...");

                    var status = await MySqlConn.ExecuteAsync(@"INSERT INTO player VALUES(@license, 0, 0, 0, 0, 0, 0, 0, 0)", playerLicense);
                    await Delay(0);

                    if (status != 0)
                    {
                        deferrals.update("OK! Redirecionando para o servidor!");
                    }
                    else
                    {
                        deferrals.done("Não foi possível criar sua conta. Por favor, entre em contato com os administradores no Discord.");
                    }

                }
            }
            deferrals.done();
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
                    ConnectedPlayers.Add(p, new KothPlayer(p));
                }
            }
        }

        static internal KothPlayer GetKothPlayerByPlayerObj(Player player)
        {
            try
            {
                return ConnectedPlayers[player];
            }
            catch (KeyNotFoundException)
            {
                Logger.Error($"[{MethodBase.GetCurrentMethod().Name}] Attempted to retrieve an invalid player from the collection.");
            }
            catch (ArgumentNullException)
            {
                Logger.Error($"[{MethodBase.GetCurrentMethod().Name}] \"player\" argument was \"null\".");
            }
            return null;
        }

        static internal bool RemovePlayerFromPlayerList(Player player)
        {
            Logger.Debug($"Removing player object for \"{player.Name}\".");
            return ConnectedPlayers.Remove(player);
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
                        if (maxHealth == 200 && currHealth > 0 && currHealth < maxHealth)
                        {
                            Logger.Debug($"Healing \"{_p.Name}\" as they are in the safe zone.");
                            p.Citizen.TriggerEvent("koth:safeHeal", (int)Utils.lerp(currHealth + 5, maxHealth, 0.1f));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[{MethodBase.GetCurrentMethod().Name}] Exception: { ex.Message }");
            }

            await Delay(1000);
        }

        [Tick]
        private async Task MainGameLoop()
        {
            try
            {
                string data = Match.State.Dequeue();
                Debug.WriteLine(data);
                BroadcastEvent("koth:StateUpdate", data);
            }
            catch (InvalidOperationException)
            { }
            catch (Exception ex)
            {
                Logger.Error($"[{MethodBase.GetCurrentMethod().Name}] Exception: { ex.Message }");
            }

            await Delay(100);
        }

        internal static bool DbExecuteSync(string q, object param = null)
        {
            return MySqlConn.Execute(q, param) != 0;
        }

        internal static T DbQuerySingleSync<T>(string q, object param = null)
        {
            return MySqlConn.QuerySingle<T>(q, param);
        }

        internal static async Task<bool> DbExecuteAsync(string q, object param = null)
        {
            var query = await MySqlConn.ExecuteAsync(q, param);
            return query != 0;
        }

        internal static async Task<T> DbQuerySingleAsync<T>(string q, object param = null)
        {
            var query = await MySqlConn.QuerySingleAsync<T>(q, param);
            return query;
        }

        internal static void SavePlayer(KothPlayer p)
        {
            var sessionResults = new
            {
                TotalKills = p.TotalKills + p.SessionKills,
                TotalDeaths = p.TotalDeaths + p.SessionDeaths,
                TotalMoney = p.TotalMoney + p.SessionMoney,
                LastLogin = DateTime.UtcNow.ToBinary(),
                p.Experience,
                p.Level
            };
            try
            {
                var queryStatus = DbExecuteSync(@"
                        UPDATE player SET TotalKills=@TotalKills, TotalDeaths=@TotalDeaths, TotalMoney=@TotalMoney, LastLogin=@LastLogin, Experience=@Experience, Level=@Level", sessionResults);
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal($"Unable to save player statistics to database: { ex }");
            }
        }
    }
}