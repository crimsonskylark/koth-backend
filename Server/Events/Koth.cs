using System.Linq;
using CitizenFX.Core;

using static CitizenFX.Core.Native.API;

namespace Server.Events
{
    class Koth
    {

        static readonly uint DefaultPed = (uint)GetHashKey("mp_m_freemode_01");
        static readonly uint DefaultWeapon = (uint)GetHashKey("weapon_compactrifle");
        static readonly float[] CombatZone = Server.GetCurrentCombatZone();

        internal static void OnTeamJoin ( [FromSource] Player player, string team_id )
        {
            var valid_team = int.TryParse(team_id, out int ParsedTeamId);

            if (string.IsNullOrEmpty(team_id) || !valid_team || ParsedTeamId < 0 || ParsedTeamId > 3)
            {
                Debug.WriteLine($"Invalid team: {ParsedTeamId}");
                return;
            }

            var team = Server.GetTeamById(ParsedTeamId-1);
            var playerObj = Server.GetPlayerByPlayerObj(player);

            if (playerObj.JoinTeam(team))
            {
                var teammates = (from p in team.Players
                                 where p != playerObj
                                 select NetworkGetEntityOwner(p.Base.Character.Handle)).ToArray();

                var spawn = playerObj.Team.Zone;

                playerObj.Base.TriggerEvent("koth:playerJoinedTeam", teammates, spawn.PlayerSpawnCoords, spawn.VehDealerCoords, spawn.VehDealerPropCoords, CombatZone, DefaultPed);

                playerObj.Base.TriggerEvent("chat:addMessage", new { args = new[] { $"You are now part of team {team.Name}" } });
            }
            else
            {
                playerObj.Base.TriggerEvent("chat:addMessage", new { args = new[] { $"Failed to join team {team.Name}" } });
            }

        }

        internal static void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            var p = Server.GetPlayerByPlayerObj(player);

            var handle = p.Base.Character.Handle;

            GiveWeaponToPed(p.Base.Character.Handle,
                            DefaultWeapon,
                            350,
                            false,
                            true);

            SetPedArmour(handle, 100);

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
            Debug.WriteLine("Koth Inside Safe Zone");
        }

        internal static void OnPlayerOutsideSafeZone ( [FromSource] Player player )
        {
            Debug.WriteLine("Koth OnPlayerOutsideSafeZone");
        }

        internal static void OnPlayerInsideCombatZone ( [FromSource] Player player )
        {
            Debug.WriteLine("Koth OnPlayerInsideCombatZone");
        }

        internal static void OnPlayerOutsideCombatZone ( [FromSource] Player player )
        {
            Debug.WriteLine("Koth OnPlayerOutsideCombatZone");
        }
    }
}
