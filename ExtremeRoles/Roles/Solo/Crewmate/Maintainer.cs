﻿using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;
using ExtremeRoles.Performance.Il2Cpp;


namespace ExtremeRoles.Roles.Solo.Crewmate
{
    public sealed class Maintainer : SingleRoleBase, IRoleAbility
    {
        public ExtremeAbilityButton Button
        {
            get => this.maintenanceButton;
            set
            {
                this.maintenanceButton = value;
            }
        }

        private ExtremeAbilityButton maintenanceButton;

        public Maintainer() : base(
            ExtremeRoleId.Maintainer,
            ExtremeRoleType.Crewmate,
            ExtremeRoleId.Maintainer.ToString(),
            ColorPalette.MaintainerBlue,
            false, true, false, false)
        { }

        public void CreateAbility()
        {
            this.CreateAbilityCountButton(
                "maintenance",
                Loader.CreateSpriteFromResources(
                    Path.MaintainerRepair));
            this.Button.SetLabelToCrewmate();
        }

        public bool UseAbility()
        {
            GameSystem.RpcRepairAllSabotage();

            foreach (PlainDoor door in CachedShipStatus.Instance.AllDoors)
            {
                DeconControl decon = door.GetComponentInChildren<DeconControl>();
                if (decon != null) { continue; }

                CachedShipStatus.Instance.RpcRepairSystem(
                    SystemTypes.Doors, door.Id | 64);
                door.SetDoorway(true);
            }

            return true;
        }

        public bool IsAbilityUse()
        {
            bool sabotageActive = false;
            foreach (PlayerTask task in 
                CachedPlayerControl.LocalPlayer.PlayerControl.myTasks.GetFastEnumerator())
            {
                if (task == null) { continue; }

                TaskTypes taskType = task.TaskType;
                if (ExtremeRolesPlugin.Compat.IsModMap)
                {
                    if (ExtremeRolesPlugin.Compat.ModMap.IsCustomSabotageTask(taskType))
                    {
                        sabotageActive = true;
                        break;
                    }
                }

                if (GameSystem.SaboTask.Contains(taskType))
                {
                    sabotageActive = true;
                    break;
                }
            }

            return sabotageActive && this.IsCommonUse();

        }

        public void ResetOnMeetingStart()
        {
            return;
        }

        public void ResetOnMeetingEnd(GameData.PlayerInfo exiledPlayer = null)
        {
            return;
        }

        protected override void CreateSpecificOption(
            IOption parentOps)
        {
            this.CreateAbilityCountOption(
                parentOps, 2, 10);
        }

        protected override void RoleSpecificInit()
        {
            this.RoleAbilityInit();
        }
    }
}
