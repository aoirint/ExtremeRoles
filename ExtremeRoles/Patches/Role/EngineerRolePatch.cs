﻿using ExtremeRoles.GameMode;
using HarmonyLib;

namespace ExtremeRoles.Patches.Role
{
    [HarmonyPatch(typeof(EngineerRole), nameof(EngineerRole.FixedUpdate))]
    public static class EngineerRoleFixedUpdatePatch
    {
        public static bool Prefix(EngineerRole __instance)
        {
            return !ExtremeGameModeManager.Instance.ShipOption.EngineerUseImpostorVent;
        }
    }
}
