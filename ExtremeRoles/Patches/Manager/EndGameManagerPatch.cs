﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using HarmonyLib;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Roles;

namespace ExtremeRoles.Patches.Manager
{

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    public class EndGameManagerSetUpPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
            setPlayerNameAndRole(__instance);
            setWinBonusText(__instance);
        }

        private static void setPlayerNameAndRole(
            EndGameManager manager)
        {
            // Delete and readd PoolablePlayers always showing the name and role of the player
            foreach (PoolablePlayer pb in manager.transform.GetComponentsInChildren<PoolablePlayer>())
            {
                UnityEngine.Object.Destroy(pb.gameObject);
            }
            int num = Mathf.CeilToInt(7.5f);
            List<WinningPlayerData> winnerList = TempData.winners.ToArray().ToList().OrderBy(
                delegate (WinningPlayerData b)
                {
                    if (!b.IsYou)
                    {
                        return 0;
                    }
                    return -1;
                }
                ).ToList<WinningPlayerData>();

            for (int i = 0; i < winnerList.Count; i++)
            {
                WinningPlayerData winningPlayerData = winnerList[i];
                int num2 = (i % 2 == 0) ? -1 : 1;
                int num3 = (i + 1) / 2;
                float num4 = (float)num3 / (float)num;
                float num5 = Mathf.Lerp(1f, 0.75f, num4);
                float num6 = (float)((i == 0) ? -8 : -1);

                PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate<PoolablePlayer>(
                    manager.PlayerPrefab, manager.transform);
                poolablePlayer.transform.localPosition = new Vector3(
                    1f * (float)num2 * (float)num3 * num5,
                    FloatRange.SpreadToEdges(-1.125f, 0f, num3, num),
                    num6 + (float)num3 * 0.01f) * 0.9f;

                float num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
                Vector3 vector = new Vector3(num7, num7, 1f);

                poolablePlayer.transform.localScale = vector;
                poolablePlayer.UpdateFromPlayerOutfit(winningPlayerData, winningPlayerData.IsDead);

                if (winningPlayerData.IsDead)
                {
                    poolablePlayer.Body.sprite = manager.GhostSprite;
                    poolablePlayer.SetDeadFlipX(i % 2 == 0);
                }
                else
                {
                    poolablePlayer.SetFlipX(i % 2 == 0);
                }

                poolablePlayer.NameText.color = Color.white;
                poolablePlayer.NameText.lineSpacing *= 0.7f;
                poolablePlayer.NameText.transform.localScale = new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z);
                poolablePlayer.NameText.transform.localPosition = new Vector3(
                    poolablePlayer.NameText.transform.localPosition.x,
                    poolablePlayer.NameText.transform.localPosition.y, -15f);
                poolablePlayer.NameText.text = winningPlayerData.PlayerName;

                foreach (var data in GameDataContainer.EndGamePlayerInfo)
                {
                    if (data.PlayerName != winningPlayerData.PlayerName) { continue; }
                    poolablePlayer.NameText.text +=
                        $"\n\n<size=80%>{string.Join("\n", data.Roles.GetColoredRoleName())}</size>";
                }
            }
        }

        private static void setRoleSummary(EndGameManager manager)
        {
            if (!MapOption.ShowRoleSummary) { return; }

            var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            GameObject roleSummary = UnityEngine.Object.Instantiate(
                manager.WinText.gameObject);
            roleSummary.transform.position = new Vector3(
                manager.Navigation.ExitButton.transform.position.x + 0.1f,
                position.y - 0.1f, -14f);
            roleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            var roleSummaryText = new StringBuilder();
            roleSummaryText.AppendLine(Translation.GetString("roleSummaryText"));
            /*
            AdditionalTempData.playerRoles.Sort((x, y) =>
            {
                RoleInfo roleX = x.Roles.FirstOrDefault();
                RoleInfo roleY = y.Roles.FirstOrDefault();
                RoleId idX = roleX == null ? RoleId.NoRole : roleX.roleId;
                RoleId idY = roleY == null ? RoleId.NoRole : roleY.roleId;

                if (x.Status == y.Status)
                {
                    if (idX == idY)
                    {
                        return x.PlayerName.CompareTo(y.PlayerName);
                    }
                    return idX.CompareTo(idY);
                }
                return x.Status.CompareTo(y.Status);

            });

            foreach (var data in AdditionalTempData.playerRoles)
            {
                var taskInfo = data.TasksTotal > 0 ? $"<color=#FAD934FF>{data.TasksCompleted}/{data.TasksTotal}</color>" : "";
                string aliveDead = ModTranslation.getString("roleSummary" + data.Status.ToString(), def: "-");
                roleSummaryText.AppendLine($"{data.PlayerName}<pos=18.5%>{taskInfo}<pos=25%>{aliveDead}<pos=34%>{data.RoleString}");
            }
            */
            TMPro.TMP_Text roleSummaryTextMesh = roleSummary.GetComponent<TMPro.TMP_Text>();
            roleSummaryTextMesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
            roleSummaryTextMesh.color = Color.white;
            roleSummaryTextMesh.outlineWidth *= 1.2f;
            roleSummaryTextMesh.fontSizeMin = 1.25f;
            roleSummaryTextMesh.fontSizeMax = 1.25f;
            roleSummaryTextMesh.fontSize = 1.25f;

            var roleSummaryTextMeshRectTransform = roleSummaryTextMesh.GetComponent<RectTransform>();
            roleSummaryTextMeshRectTransform.anchoredPosition = new Vector2(position.x + 3.5f, position.y - 0.1f);
            roleSummaryTextMesh.text = roleSummaryText.ToString();
        }

        private static void setWinBonusText(
            EndGameManager manager)
        {

            GameObject bonusTextObject = UnityEngine.Object.Instantiate(manager.WinText.gameObject);
            bonusTextObject.transform.position = new Vector3(
                manager.WinText.transform.position.x,
                manager.WinText.transform.position.y - 0.8f,
                manager.WinText.transform.position.z);
            bonusTextObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            TMPro.TMP_Text textRenderer = bonusTextObject.GetComponent<TMPro.TMP_Text>();
            textRenderer.text = "";

            string bonusText = "";


            switch (GameDataContainer.EndReason)
            {
                case GameOverReason.HumansByTask:
                case GameOverReason.HumansByVote:
                    bonusText = "crewWin";
                    textRenderer.color = Palette.White;
                    break;
                case GameOverReason.ImpostorByKill:
                case GameOverReason.ImpostorByVote:
                case GameOverReason.ImpostorBySabotage:
                case (GameOverReason)RoleGameOverReason.AssassinationMarin:
                    bonusText = "impostorWin";
                    textRenderer.color = Palette.ImpostorRed;
                    break;
                case (GameOverReason)RoleGameOverReason.AliceKilledByImposter:
                case (GameOverReason)RoleGameOverReason.AliceKillAllOthers:
                    bonusText = "aliceWin";
                    textRenderer.color = ColorPalette.AliceGold;
                    manager.BackgroundBar.material.SetColor("_Color", ColorPalette.AliceGold);
                    break;
                case (GameOverReason)RoleGameOverReason.JackalKillAllOthers:
                    bonusText = "jackalWin";
                    textRenderer.color = ColorPalette.JackalBlue;
                    manager.BackgroundBar.material.SetColor("_Color", ColorPalette.JackalBlue);
                    break;
                default:
                    break;
            }
            string extraText = "";

            if (extraText.Length > 0)
            {
                textRenderer.text = string.Format(Translation.GetString(bonusText + "Extra"), extraText);
            }
            else
            {
                textRenderer.text = Translation.GetString(bonusText);
            }
        }

    }
}
