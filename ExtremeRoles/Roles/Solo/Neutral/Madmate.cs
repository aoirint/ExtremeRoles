﻿using System.Collections.Generic;

using UnityEngine;

using ExtremeRoles.Module;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;

namespace ExtremeRoles.Roles.Solo.Neutral
{
    public sealed class Madmate : 
        SingleRoleBase, 
        IRoleAbility, 
        IRoleUpdate, 
        IRoleSpecialSetUp, 
        IRoleWinPlayerModifier
    {
        public enum MadmateOption
        {
            IsDontCountAliveCrew,
            CanFixSabotage,
            CanUseVent,
            CanMoveVentToVent,
            HasTask,
            SeeImpostorTaskGage,
            CanSeeFromImpostor,
            CanSeeFromImpostorTaskGage,
        }

        private bool canMoveVentToVent = false;
        private bool canSeeFromImpostor = false;
        private bool isDontCountAliveCrew = false;

        private bool isSeeImpostorNow = false;
        private bool isUpdateMadmate = false;
        private float seeImpostorTaskGage;
        private float seeFromImpostorTaskGage;

        public ExtremeAbilityButton Button
        {
            get => this.madmateAbilityButton;
            set
            {
                this.madmateAbilityButton = value;
            }
        }

        public bool IsDontCountAliveCrew => this.isDontCountAliveCrew; 

        private ExtremeAbilityButton madmateAbilityButton;

        public Madmate() : base(
            ExtremeRoleId.Madmate,
            ExtremeRoleType.Neutral,
            ExtremeRoleId.Madmate.ToString(),
            Palette.ImpostorRed,
            false, false, false, false)
        { }

        public static void ToFakeImpostor(byte playerId)
        {

            Madmate madmate = ExtremeRoleManager.GetSafeCastedRole<Madmate>(playerId);
            if (madmate == null) { return; }

            madmate.FakeImposter = true;
        }

        public void CreateAbility()
        {
            this.CreateNormalAbilityButton(
                "selfKill", Loader.CreateSpriteFromResources(
                    Path.SucideSprite));
        }

        public bool UseAbility()
        {

            byte playerId = CachedPlayerControl.LocalPlayer.PlayerId;

            Helper.Player.RpcUncheckMurderPlayer(
                playerId, playerId, byte.MaxValue);
            return true;
        }

        public bool IsAbilityUse() => this.IsCommonUse();

        public void ResetOnMeetingStart()
        {
            return;
        }

        public void ResetOnMeetingEnd(GameData.PlayerInfo exiledPlayer = null)
        {
            return;
        }

        public void IntroBeginSetUp()
        {
            return;
        }

        public void IntroEndSetUp()
        {
            if (!this.UseVent || this.canMoveVentToVent) { return; }
            
            // 全てのベントリンクを解除
            foreach (Vent vent in CachedShipStatus.Instance.AllVents)
            {
                vent.Right = null;
                vent.Center = null;
                vent.Left = null;
            }
        }

        public void ModifiedWinPlayer(
            GameData.PlayerInfo rolePlayerInfo,
            GameOverReason reason,
            ref Il2CppSystem.Collections.Generic.List<WinningPlayerData> winner,
            ref List<GameData.PlayerInfo> pulsWinner)
        {
            switch (reason)
            {
                case GameOverReason.ImpostorByVote:
                case GameOverReason.ImpostorByKill:
                case GameOverReason.ImpostorBySabotage:
                case GameOverReason.ImpostorDisconnect:
                case GameOverReason.HideAndSeek_ByKills:
                case (GameOverReason)RoleGameOverReason.AssassinationMarin:
                    this.AddWinner(rolePlayerInfo, winner, pulsWinner);
                    break;
                default:
                    break;
            }
        }

        public void Update(PlayerControl rolePlayer)
        {
            if (!this.HasTask) { return; }

            float taskGage = Helper.Player.GetPlayerTaskGage(rolePlayer);
            if (taskGage >= this.seeImpostorTaskGage && !isSeeImpostorNow)
            {
                this.isSeeImpostorNow = true;
            }
            if (this.canSeeFromImpostor &&
                taskGage >= this.seeFromImpostorTaskGage &&
                !this.isUpdateMadmate)
            {
                this.isUpdateMadmate = true;

                using (var caller = RPCOperator.CreateCaller(
                    RPCOperator.Command.MadmateToFakeImpostor))
                {
                    caller.WriteByte(rolePlayer.PlayerId);
                }
                ToFakeImpostor(rolePlayer.PlayerId);
            }
        }

        public override Color GetTargetRoleSeeColor(
            SingleRoleBase targetRole, byte targetPlayerId)
        {
            if (this.isSeeImpostorNow && 
                (targetRole.IsImpostor() || targetRole.FakeImposter))
            {
                return Palette.ImpostorRed;
            }
            
            return base.GetTargetRoleSeeColor(targetRole, targetPlayerId);
        }

        protected override void CreateSpecificOption(
            IOption parentOps)
        {
            CreateBoolOption(
                MadmateOption.IsDontCountAliveCrew,
                false, parentOps);
            CreateBoolOption(
                MadmateOption.CanFixSabotage,
                false, parentOps);
            var ventUseOpt = CreateBoolOption(
                MadmateOption.CanUseVent,
                false, parentOps);
            CreateBoolOption(
                MadmateOption.CanMoveVentToVent,
                false, ventUseOpt);
            var taskOpt = CreateBoolOption(
                MadmateOption.HasTask,
                false, parentOps);
            CreateIntOption(
                MadmateOption.SeeImpostorTaskGage,
                70, 0, 100, 10,
                taskOpt,
                format: OptionUnit.Percentage);
            var impFromSeeOpt = CreateBoolOption(
                MadmateOption.CanSeeFromImpostor,
                false, taskOpt);
            CreateIntOption(
                MadmateOption.CanSeeFromImpostorTaskGage,
                70, 0, 100, 10,
                impFromSeeOpt,
                format: OptionUnit.Percentage);

            this.CreateCommonAbilityOption(parentOps);
        }

        protected override void RoleSpecificInit()
        {
            var allOpt = OptionHolder.AllOption;
            this.isSeeImpostorNow = false;
            this.isUpdateMadmate = false;
            this.FakeImposter = false;

            this.isDontCountAliveCrew = allOpt[
                GetRoleOptionId(MadmateOption.IsDontCountAliveCrew)].GetValue();

            this.CanRepairSabotage = allOpt[
                GetRoleOptionId(MadmateOption.CanFixSabotage)].GetValue();
            this.UseVent = allOpt[
                GetRoleOptionId(MadmateOption.CanUseVent)].GetValue();
            this.canMoveVentToVent = allOpt[
                GetRoleOptionId(MadmateOption.CanMoveVentToVent)].GetValue();
            this.HasTask = allOpt[
                GetRoleOptionId(MadmateOption.HasTask)].GetValue();
            this.seeImpostorTaskGage = (float)allOpt[
                GetRoleOptionId(MadmateOption.SeeImpostorTaskGage)].GetValue() / 100.0f;
            this.canSeeFromImpostor = allOpt[
                GetRoleOptionId(MadmateOption.CanSeeFromImpostor)].GetValue();
            this.seeFromImpostorTaskGage = (float)allOpt[
                GetRoleOptionId(MadmateOption.CanSeeFromImpostorTaskGage)].GetValue() / 100.0f;

            this.isSeeImpostorNow = 
                this.HasTask && 
                this.seeImpostorTaskGage <= 0.0f;
            this.isUpdateMadmate = 
                this.HasTask &&
                this.canSeeFromImpostor &&
                this.seeFromImpostorTaskGage <= 0.0f;

            this.FakeImposter = this.isUpdateMadmate;

            this.RoleAbilityInit();
        }
    }
}
