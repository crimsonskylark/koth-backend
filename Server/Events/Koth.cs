using System.Linq;
using CitizenFX.Core;
using Koth.Shared;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace Server.Events
{
    internal class Koth : BaseScript
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

            var p = GameSession.GetPlayerByPlayerObj(player);

            if (GameSession.Match.JoinTeam(p, ParsedTeamId - 1))
            {
                var teammates = (from other in p.Team.Members
                                 where p != other
                                 select NetworkGetEntityOwner(other.CfxPlayer.Character.Handle)).ToArray();

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

                p.CfxPlayer.TriggerEvent("koth:playerJoinedTeam", ctxObj);

                foreach (var member in p.Team.Members)
                {
                    if (member != p)
                    {
                        member.CfxPlayer.TriggerEvent("koth:newTeammate", NetworkGetEntityOwner(p.CfxPlayer.Character.Handle));
                    }
                }

            }
            else
            {
                p.CfxPlayer.TriggerEvent("chat:addMessage", new { args = new[] { $"Falha ao tentar entrar no time ~g~{p.Team.Name}" } });
            }
        }

        internal static void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            var p = GameSession.GetPlayerByPlayerObj(player);

            var handle = p.CfxPlayer.Character.Handle;

            GiveWeaponToPed(p.CfxPlayer.Character.Handle,
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
            var p = GameSession.GetPlayerByPlayerObj(player);
            p.IsInsideSafeZone = true;
            Debug.WriteLine("Koth OnPlayerInsideSafeZone");
        }

        internal static void OnPlayerOutsideSafeZone ( [FromSource] Player player )
        {
            var p = GameSession.GetPlayerByPlayerObj(player);
            p.IsInsideSafeZone = false;
            Debug.WriteLine("Koth OnPlayerOutsideSafeZone");
        }

        internal static void OnPlayerInsideCombatZone ( [FromSource] Player player )
        {
            var p = GameSession.GetPlayerByPlayerObj(player);
            p.IsInsideAO = true;
            GameSession.Match.AddFlagPointToTeam(p.Team);
            Debug.WriteLine("Koth OnPlayerInsideCombatZone");
        }

        internal static void OnPlayerOutsideCombatZone ( [FromSource] Player player )
        {
            var p = GameSession.GetPlayerByPlayerObj(player);
            p.IsInsideAO = false;
            GameSession.Match.RemoveFlagPointFromTeam(p.Team);
            Debug.WriteLine("Koth OnPlayerOutsideCombatZone");
        }
    }
}