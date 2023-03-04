﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Performance;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Roles.API.Extension.State;

namespace ExtremeRoles.Roles.Solo.Crewmate
{
    public sealed class Resurrecter : 
        SingleRoleBase,
        IRoleAwake<RoleTypes>,
        IRoleResetMeeting,
        IRoleOnRevive
    {
        public override bool IsAssignGhostRole
        {
            get => false;
        }

        public bool IsAwake
        {
            get
            {
                return GameSystem.IsLobby || this.awakeRole;
            }
        }

        public RoleTypes NoneAwakeRole => RoleTypes.Crewmate;

        public enum ResurrecterOption
        {
            AwakeTaskGage,
            ResurrectTaskGage,
            ResurrectDelayTime,
            IsMeetingCoolResetOnResurrect,
            ResurrectMeetingCooltime,
            ResurrectTaskResetMeetingNum,
            ResurrectTaskResetGage,
            CanResurrectAfterDeath,
            CanResurrectOnExil,
        }

        public enum ResurrecterRpcOps : byte
        {
            UseResurrect,
            ResetFlash,
        }

        private bool awakeRole;
        private float awakeTaskGage;
        private bool awakeHasOtherVision;
        private float resurrectTaskGage;

        private bool canResurrect;
        private bool canResurrectAfterDeath;
        private bool canResurrectOnExil;
        private bool isResurrected;
        private bool isExild;

        private bool isActiveMeetingCount;
        private int meetingCounter;
        private int maxMeetingCount;

        private bool activateResurrectTimer;
        private float resurrectTimer;

        private bool isMeetingCoolResetOnResurrect;
        private float meetingCoolDown;

        private float resetTaskGage;
        private TMPro.TextMeshPro resurrectText;

        private static SpriteRenderer flash;

        public Resurrecter() : base(
            ExtremeRoleId.Resurrecter,
            ExtremeRoleType.Crewmate,
            ExtremeRoleId.Resurrecter.ToString(),
            ColorPalette.ResurrecterBlue,
            false, true, false, false)
        { }

        public static void RpcAbility(ref MessageReader reader)
        {
            ResurrecterRpcOps ops = (ResurrecterRpcOps)reader.ReadByte();
            byte resurrecterPlayerId = reader.ReadByte();

            switch (ops)
            {
                case ResurrecterRpcOps.UseResurrect:
                    Resurrecter resurrecter = ExtremeRoleManager.GetSafeCastedRole<Resurrecter>(
                        resurrecterPlayerId);
                    if (resurrecter == null) { return; }
                    UseResurrect(resurrecter);
                    break;
                case ResurrecterRpcOps.ResetFlash:
                    if (flash != null)
                    {
                        flash.enabled = false;
                    }
                    break;
                default:
                    break;
            }
        }

        public static void UseResurrect(Resurrecter resurrecter)
        {
            resurrecter.isResurrected = true;
            resurrecter.isActiveMeetingCount = true;
            resurrecter.activateResurrectTimer = false;
        }

        public void ResetOnMeetingStart()
        {
            if (this.isActiveMeetingCount)
            {
                ++this.meetingCounter;
            }

            if (this.resurrectText != null)
            {
                this.resurrectText.gameObject.SetActive(false);
            }

            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.ResurrecterRpc))
            {
                caller.WriteByte((byte)ResurrecterRpcOps.ResetFlash);
                caller.WriteByte(CachedPlayerControl.LocalPlayer.PlayerId);
            }

            if (flash != null)
            {
                flash.enabled = false;
            }
        }

        public void ResetOnMeetingEnd(GameData.PlayerInfo exiledPlayer = null)
        {
            if (this.isActiveMeetingCount &&
                this.meetingCounter >= this.maxMeetingCount)
            {
                this.isActiveMeetingCount = false;
                this.meetingCounter = 0;
                replaceTask(CachedPlayerControl.LocalPlayer);
            }
        }

        public void ReviveAction(PlayerControl player)
        {
            // リセット会議クールダウン
            if (this.isMeetingCoolResetOnResurrect)
            {
                CachedShipStatus.Instance.EmergencyCooldown = this.meetingCoolDown;
            }

            var role = ExtremeRoleManager.GetLocalPlayerRole();
            if (role.CanKill() && !role.IsCrewmate())
            {
                var hudManager = FastDestroyableSingleton<HudManager>.Instance;

                if (flash == null)
                {
                    flash = Object.Instantiate(
                         hudManager.FullScreen,
                         hudManager.transform);
                    flash.transform.localPosition = new Vector3(0f, 0f, 20f);
                    flash.gameObject.SetActive(true);
                }

                hudManager.StartCoroutine(
                    Effects.Lerp(1.0f, new System.Action<float>((p) =>
                    {
                        if (flash == null) { return; }
                        
                        if (p < 0.5)
                        {
                            flash.color = new Color(
                                this.NameColor.r, this.NameColor.g,
                                this.NameColor.b, Mathf.Clamp01(p * 2 * 0.75f));

                        }
                        else
                        {
                            flash.color = new Color(
                                this.NameColor.r, this.NameColor.g,
                                this.NameColor.b, Mathf.Clamp01((1 - p) * 2 * 0.75f));
                        }
                        if (p == 1f)
                        {
                            flash.enabled = false;
                        }
                    }))
                );
            }
        }

        public string GetFakeOptionString() => "";

        public void Update(PlayerControl rolePlayer)
        {

            if (rolePlayer.Data.IsDead && this.infoBlock())
            {
                FastDestroyableSingleton<HudManager>.Instance.Chat.gameObject.SetActive(false);
            }

            if (!rolePlayer.moveable ||
                MeetingHud.Instance ||
                ExileController.Instance ||
                CachedShipStatus.Instance == null ||
                !CachedShipStatus.Instance.enabled)
            {
                return;
            }

            if ((!this.awakeRole || 
                (!this.canResurrect && !this.isResurrected)) &&
                rolePlayer.myTasks.Count != 0)
            {
                float taskGage = Player.GetPlayerTaskGage(rolePlayer);

                if (taskGage >= this.awakeTaskGage && !this.awakeRole)
                {
                    this.awakeRole = true;
                    this.HasOtherVision = this.awakeHasOtherVision;
                }
                if (taskGage >= this.resurrectTaskGage && 
                    !this.canResurrect)
                {
                    if (this.canResurrectAfterDeath &&
                        rolePlayer.Data.IsDead)
                    {
                        revive(rolePlayer);
                    }
                    else
                    {
                        this.canResurrect = true;
                        this.isResurrected = false;
                    }
                }
            }

            if (this.isResurrected) { return; }

            if (rolePlayer.Data.IsDead &&
                this.activateResurrectTimer &&
                this.canResurrect)
            {
                if (this.resurrectText == null)
                {
                    this.resurrectText = Object.Instantiate(
                        FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
                        Camera.main.transform, false);
                    this.resurrectText.transform.localPosition = new Vector3(0.0f, 0.0f, -250.0f);
                    this.resurrectText.enableWordWrapping = false;
                }

                this.resurrectText.gameObject.SetActive(true);
                this.resurrectTimer -= Time.fixedDeltaTime;
                this.resurrectText.text = string.Format(
                    Translation.GetString("resurrectText"),
                    Mathf.CeilToInt(this.resurrectTimer));

                if (this.resurrectTimer <= 0.0f)
                {
                    this.activateResurrectTimer = false;
                    revive(rolePlayer);
                }
            }
        }

        public override string GetColoredRoleName(bool isTruthColor = false)
        {
            if (isTruthColor || IsAwake)
            {
                return base.GetColoredRoleName();
            }
            else
            {
                return Design.ColoedString(
                    Palette.White, Translation.GetString(RoleTypes.Crewmate.ToString()));
            }
        }
        public override string GetFullDescription()
        {
            if (IsAwake)
            {
                return Translation.GetString(
                    $"{this.Id}FullDescription");
            }
            else
            {
                return Translation.GetString(
                    $"{RoleTypes.Crewmate}FullDescription");
            }
        }

        public override string GetImportantText(bool isContainFakeTask = true)
        {
            if (IsAwake)
            {
                return base.GetImportantText(isContainFakeTask);

            }
            else
            {
                return Design.ColoedString(
                    Palette.White,
                    $"{this.GetColoredRoleName()}: {Translation.GetString("crewImportantText")}");
            }
        }

        public override string GetIntroDescription()
        {
            if (IsAwake)
            {
                return base.GetIntroDescription();
            }
            else
            {
                return Design.ColoedString(
                    Palette.CrewmateBlue,
                    CachedPlayerControl.LocalPlayer.Data.Role.Blurb);
            }
        }

        public override Color GetNameColor(bool isTruthColor = false)
        {
            if (isTruthColor || IsAwake)
            {
                return base.GetNameColor(isTruthColor);
            }
            else
            {
                return Palette.White;
            }
        }

        public override void ExiledAction(
            PlayerControl rolePlayer)
        {

            if (this.isResurrected) { return; }

            this.isExild = true;

            // 追放でオフ時は以下の処理を行わない
            if (!this.canResurrectOnExil) { return; }

            if (this.canResurrect)
            {
                this.activateResurrectTimer = true;
            }
            else if (!this.canResurrectAfterDeath)
            {
                this.isResurrected = true;
            }
        }

        public override void RolePlayerKilledAction(
            PlayerControl rolePlayer,
            PlayerControl killerPlayer)
        {
            if (this.isResurrected) { return; }

            this.isExild = false;
            
            if (this.canResurrect)
            {
                this.activateResurrectTimer = true;
            }
            else if (!this.canResurrectAfterDeath)
            {
                this.isResurrected = true;
            }
        }

        public override bool IsBlockShowMeetingRoleInfo() => this.infoBlock();

        public override bool IsBlockShowPlayingRoleInfo() => this.infoBlock();
             

        protected override void CreateSpecificOption(
            IOption parentOps)
        {
            CreateIntOption(
                ResurrecterOption.AwakeTaskGage,
                100, 0, 100, 10,
                parentOps,
                format: OptionUnit.Percentage);
            CreateIntOption(
                ResurrecterOption.ResurrectTaskGage,
                100, 50, 100, 10,
                parentOps,
                format: OptionUnit.Percentage);

            CreateFloatOption(
                ResurrecterOption.ResurrectDelayTime,
                5.0f, 4.0f, 60.0f, 0.1f,
                parentOps, format: OptionUnit.Second);

            var meetingResetOpt = CreateBoolOption(
                ResurrecterOption.IsMeetingCoolResetOnResurrect,
                true, parentOps);
             
            CreateFloatOption(
                ResurrecterOption.ResurrectMeetingCooltime,
                20.0f, 5.0f, 60.0f, 0.25f,
                meetingResetOpt,
                format: OptionUnit.Second,
                invert: true,
                enableCheckOption: parentOps);

            CreateIntOption(
                ResurrecterOption.ResurrectTaskResetMeetingNum,
                1, 1, 5, 1,
                parentOps);

            CreateIntOption(
                ResurrecterOption.ResurrectTaskResetGage,
                20, 10, 50, 5,
                parentOps,
                format: OptionUnit.Percentage);
            CreateBoolOption(
                ResurrecterOption.CanResurrectAfterDeath,
                false, parentOps);
            CreateBoolOption(
                ResurrecterOption.CanResurrectOnExil,
                false, parentOps);
        }

        protected override void RoleSpecificInit()
        {
            var allOpt = OptionHolder.AllOption;

            this.awakeTaskGage = (float)allOpt[
                GetRoleOptionId(ResurrecterOption.AwakeTaskGage)].GetValue() / 100.0f;
            this.resurrectTaskGage = (float)allOpt[
                GetRoleOptionId(ResurrecterOption.ResurrectTaskGage)].GetValue() / 100.0f;
            this.resetTaskGage = (float)allOpt[
                GetRoleOptionId(ResurrecterOption.ResurrectTaskResetGage)].GetValue() / 100.0f;

            this.resurrectTimer = allOpt[
                GetRoleOptionId(ResurrecterOption.ResurrectDelayTime)].GetValue();
            this.canResurrectAfterDeath = allOpt[
                GetRoleOptionId(ResurrecterOption.CanResurrectAfterDeath)].GetValue();
            this.canResurrectOnExil = allOpt[
                GetRoleOptionId(ResurrecterOption.CanResurrectOnExil)].GetValue();
            this.maxMeetingCount = allOpt[
                GetRoleOptionId(ResurrecterOption.ResurrectTaskResetMeetingNum)].GetValue();
            this.isMeetingCoolResetOnResurrect = allOpt[
                GetRoleOptionId(ResurrecterOption.IsMeetingCoolResetOnResurrect)].GetValue();
            this.meetingCoolDown = allOpt[
                GetRoleOptionId(ResurrecterOption.ResurrectMeetingCooltime)].GetValue();

            this.awakeHasOtherVision = this.HasOtherVision;
            this.canResurrect = false;
            this.isResurrected = false;
            this.activateResurrectTimer = false;

            if (this.awakeTaskGage <= 0.0f)
            {
                this.awakeRole = true;
                this.HasOtherVision = this.awakeHasOtherVision;
            }
            else
            {
                this.awakeRole = false;
                this.HasOtherVision = false;
            }
        }

        private bool infoBlock()
        {
            // ・詳細
            // 復活を使用後に死亡 => 常に見える
            // 非復活可能状態でキル、死亡後復活出来ない => 常に見える
            // 非復活可能状態でキル、死亡後復活出来る => 復活できるまで見えない
            // 非復活可能状態で追放、死亡後復活できる => 見えない
            // 非復活可能状態で追放、死亡後復活出来ない => 常に見える
            // 復活可能状態で死亡か追放 => 見えない

            if (this.isResurrected)
            {
                return false;
            }
            else if (!this.canResurrect || this.isExild)
            {
                return this.canResurrectAfterDeath || this.activateResurrectTimer;
            }
            else
            {
                return this.activateResurrectTimer;
            }
        }

        private void revive(PlayerControl rolePlayer)
        {
            if (rolePlayer == null) { return; }

            byte playerId = rolePlayer.PlayerId;

            Player.RpcUncheckRevive(playerId);

            if (rolePlayer.Data == null ||
                rolePlayer.Data.IsDead ||
                rolePlayer.Data.Disconnected) { return; }

            var allPlayer = GameData.Instance.AllPlayers;
            ShipStatus ship = CachedShipStatus.Instance;

            List<Vector2> randomPos = new List<Vector2>();

            if (ExtremeRolesPlugin.Compat.IsModMap)
            {
                randomPos = ExtremeRolesPlugin.Compat.ModMap.GetSpawnPos(
                    playerId);
            }
            else
            {
                switch (GameOptionsManager.Instance.CurrentGameOptions.GetByte(
                    ByteOptionNames.MapId))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        Vector2 baseVec = Vector2.up;
                        baseVec = baseVec.Rotate(
                            (float)(playerId - 1) * (360f / (float)allPlayer.Count));
                        Vector2 offset = baseVec * ship.SpawnRadius + new Vector2(0f, 0.3636f);
                        randomPos.Add(ship.InitialSpawnCenter + offset);
                        randomPos.Add(ship.MeetingSpawnCenter + offset);
                        break;
                    case 4:
                        randomPos.AddRange(GameSystem.GetAirShipRandomSpawn());
                        break;
                    default:
                        break;
                }
            }

            Player.RpcUncheckSnap(playerId, randomPos[
                RandomGenerator.Instance.Next(randomPos.Count)]);

            using (var caller = RPCOperator.CreateCaller(
                RPCOperator.Command.ResurrecterRpc))
            {
                caller.WriteByte((byte)ResurrecterRpcOps.UseResurrect);
                caller.WriteByte(playerId);
            }
            UseResurrect(this);

            FastDestroyableSingleton<HudManager>.Instance.Chat.chatBubPool.ReclaimAll();
            if (this.resurrectText != null)
            {
                this.resurrectText.gameObject.SetActive(false);
            }
        }

        private void replaceTask(PlayerControl rolePlayer)
        {
            GameData.PlayerInfo playerInfo = rolePlayer.Data;

            var shuffleTaskIndex = Enumerable.Range(
                0, playerInfo.Tasks.Count).ToList().OrderBy(
                    item => RandomGenerator.Instance.Next()).ToList();

            int replaceTaskNum = 0;
            int maxReplaceTaskNum = Mathf.CeilToInt(playerInfo.Tasks.Count * this.resetTaskGage);

            foreach (int i in shuffleTaskIndex)
            {
                if (replaceTaskNum >= maxReplaceTaskNum) { break; }

                if (playerInfo.Tasks[i].Complete)
                {
                    
                    int taskIndex;
                    int replaceTaskId = playerInfo.Tasks[i].TypeId;

                    if (CachedShipStatus.Instance.CommonTasks.FirstOrDefault(
                        (NormalPlayerTask t) => t.Index == replaceTaskId) != null)
                    {
                        taskIndex = GameSystem.GetRandomCommonTaskId();
                    }
                    else if (CachedShipStatus.Instance.LongTasks.FirstOrDefault(
                        (NormalPlayerTask t) => t.Index == replaceTaskId) != null)
                    {
                        taskIndex = GameSystem.GetRandomLongTask();
                    }
                    else if (CachedShipStatus.Instance.NormalTasks.FirstOrDefault(
                        (NormalPlayerTask t) => t.Index == replaceTaskId) != null)
                    {
                        taskIndex = GameSystem.GetRandomNormalTaskId();
                    }
                    else
                    {
                        continue;
                    }

                    GameSystem.RpcReplaceNewTask(
                        rolePlayer.PlayerId, i, taskIndex);
                    
                    ++replaceTaskNum;
                }
            }
        }
    }
}
