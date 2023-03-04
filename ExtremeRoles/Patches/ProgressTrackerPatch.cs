﻿using System.Linq;

using UnityEngine;
using HarmonyLib;
using AmongUs.GameOptions;

using ExtremeRoles.Module.RoleAssign;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.Solo.Crewmate;


namespace ExtremeRoles.Patches
{
    [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
    public static class ProgressTrackerFixedUpdatePatch
    {
        public static void Postfix(ProgressTracker __instance)
        {
            if (!RoleAssignState.Instance.IsRoleSetUpEnd) { return; }

            Agency agency = ExtremeRoleManager.GetSafeCastedLocalPlayerRole<Agency>();

            if (agency == null) { return; }

            if (!__instance.TileParent.enabled)
            {
                __instance.TileParent.enabled = true;
            }

            GameData gameData = GameData.Instance;
            if (gameData && gameData.TotalTasks > 0)
            {
                __instance.gameObject.SetActive(true);
                int num = (DestroyableSingleton<TutorialManager>.InstanceExists ? 
                    1 : (gameData.PlayerCount - GameOptionsManager.Instance.CurrentGameOptions.GetInt(
                            Int32OptionNames.NumImpostors)));
                num -= gameData.AllPlayers.ToArray().ToList().Count(
                    (GameData.PlayerInfo p) => p.Disconnected);

                float curProgress = (float)gameData.CompletedTasks / (float)gameData.TotalTasks * (float)num;
                __instance.curValue = Mathf.Lerp(
                    __instance.curValue, curProgress, Time.fixedDeltaTime * 2f);
                __instance.TileParent.material.SetFloat("_Buckets", (float)num);
                __instance.TileParent.material.SetFloat("_FullBuckets", __instance.curValue);
            }
        }
    }
}
