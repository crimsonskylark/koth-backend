using System.Linq;
using CitizenFX.Core;
using Koth.Shared;
using Newtonsoft.Json;
using Serilog;
using static CitizenFX.Core.Native.API;

namespace Server.Events
{
    internal class Koth
    {
        private static readonly uint DefaultPed = (uint)GetHashKey("mp_m_freemode_01");
        private static readonly uint DefaultWeapon = (uint)GetHashKey("weapon_compactrifle");
        private static readonly float[] CombatZone = GameSession.Match.GetCurrentAO();

        internal static void OnTeamJoin ( [FromSource] Player player, string teamId )
        {
            if (string.IsNullOrEmpty(teamId) || !int.TryParse(teamId, out int ParsedTeamId) || ParsedTeamId < 0 || ParsedTeamId > 3)
            {
                Debug.WriteLine($"[{player.Identifiers["fivem"]}] Tentou entrar em um time inválido: {teamId}");
                return;
            }

            var p = GameSession.GetKothPlayerByPlayerObj(player);

            if (GameSession.Match.JoinTeam(p, ParsedTeamId - 1))
            {
                var teammates = (from other in p.Team.Members
                                 where p != other
                                 select NetworkGetEntityOwner(other.Citizen.Character.Handle)).ToArray();

                var ctxObj = JsonConvert.SerializeObject(new SpawnContext {
                    vehiclesDealerCoords = p.Team.Zone.VehDealerCoords,
                    vehiclesDealerPropCoords = p.Team.Zone.VehDealerPropCoords,
                    playerSpawnCoords = p.Team.Zone.PlayerSpawnCoords,
                    playerTeammates = teammates,
                    combatZone = CombatZone,
                    enemyTeamNames = GameSession.Match.GetTeamNames().Where((t) => t != p.Team.Name).ToArray(),
                    playerTeamName = p.Team.Name,
                    playerModel = DefaultPed
                }) ;

                Debug.WriteLine(ctxObj);

                p.Citizen.TriggerEvent("koth:playerJoinedTeam", ctxObj);

                foreach (var member in p.Team.Members)
                {
                    if (member != p)
                    {
                        member.Citizen.TriggerEvent("koth:newTeammate", NetworkGetEntityOwner(p.Citizen.Character.Handle));
                    }
                }

            }
            else
            {
                p.Citizen.TriggerEvent("chat:addMessage", new { args = new[] { $"Falha ao tentar entrar no time ~g~{p.Team.Name}" } });
            }
        }

        internal static void OnPlayerKilledByPlayer([FromSource] Player p, int killerNetId, int victimNetId, bool killedByHeadshot)
        {
            var kEnt = Entity.FromNetworkId(killerNetId);
            var vEnt = Entity.FromNetworkId(victimNetId);

            if (kEnt == null || vEnt == null || kEnt is not Ped kp || vEnt is not Ped vp)
                return;

            var kObj = GameSession.GetKothPlayerByPlayerObj(kp.Owner);
            var vObj = GameSession.GetKothPlayerByPlayerObj(vp.Owner);

            if (kObj.Team != vObj.Team)
            {
                Log.Logger.Debug($"Player { kObj.Citizen.Name } killed enemy { (killedByHeadshot ? "with a headshot" : "with bodyshots.")}");

                GameSession.Match.AddDeathToPlayer(vObj);
                GameSession.Match.AddKillToPlayer(kObj, killedByHeadshot ? 1600 : 800);
            }

        }

        internal static void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);

            var handle = p.Citizen.Character.Handle;

            GiveWeaponToPed(p.Citizen.Character.Handle,
                            DefaultWeapon,
                            350,
                            false,
                            true);

            SetPedArmour(handle, 50);

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

        internal static void OnPlayerInsideSafeZone ( [FromSource] Player player )
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);
            p.IsInsideSafeZone = true;
            Log.Logger.Debug($"\"{ p.Citizen.Name }\" triggered \"OnPlayerInsideSafeZone\".");
        }

        internal static void OnPlayerOutsideSafeZone ( [FromSource] Player player )
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);
            p.IsInsideSafeZone = false;
            Log.Logger.Debug($"\"{ p.Citizen.Name }\" triggered \"OnPlayerOutsideSafeZone\".");
        }

        internal static void OnPlayerInsideCombatZone ( [FromSource] Player player )
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);
            p.IsInsideAO = true;
            GameSession.Match.AddFlagPointToTeam(p.Team);
            Log.Logger.Debug($"\"{ p.Citizen.Name }\" triggered \"OnPlayerInsideCombatZone\".");
        }

        internal static void OnPlayerOutsideCombatZone ( [FromSource] Player player )
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);
            p.IsInsideAO = false;
            GameSession.Match.RemoveFlagPointFromTeam(p.Team);
            Log.Logger.Debug($"\"{ p.Citizen.Name }\" triggered \"OnPlayerOutsideCombatZone\".");
        }
    }
}