﻿using System.Collections.Generic;

using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.GameMode;
using ExtremeRoles.GameMode.RoleSelector;
using ExtremeRoles.Helper;
using ExtremeRoles.Module.Interface;

namespace ExtremeRoles.Module.RoleAssign
{
    public sealed class RoleSpawnDataManager : ISpawnDataManager
    {
        public Dictionary<ExtremeRoleType, int> MaxRoleNum { get; private set; }

        public Dictionary<ExtremeRoleType, Dictionary<int, SingleRoleSpawnData>> CurrentSingleRoleSpawnData
        { get; private set; }
        public Dictionary<byte, CombinationRoleSpawnData> CurrentCombRoleSpawnData { get; private set; }

        public List<(CombinationRoleType, GhostAndAliveCombinationRoleManagerBase)> UseGhostCombRole 
        { get; private set; }

        public RoleSpawnDataManager()
        {
            UseGhostCombRole = new List<(CombinationRoleType, GhostAndAliveCombinationRoleManagerBase)>();
            CurrentCombRoleSpawnData = new Dictionary<byte, CombinationRoleSpawnData>();

            CurrentSingleRoleSpawnData = new Dictionary<ExtremeRoleType, Dictionary<int, SingleRoleSpawnData>>
            {
                { ExtremeRoleType.Crewmate, new Dictionary<int, SingleRoleSpawnData>() },
                { ExtremeRoleType.Impostor, new Dictionary<int, SingleRoleSpawnData>() },
                { ExtremeRoleType.Neutral , new Dictionary<int, SingleRoleSpawnData>() },
            };

            MaxRoleNum = new Dictionary<ExtremeRoleType, int>
            {
                {
                    ExtremeRoleType.Crewmate,
                    ISpawnDataManager.ComputeSpawnNum(
                        RoleGlobalOption.MinCrewmateRoles,
                        RoleGlobalOption.MaxCrewmateRoles)
                },
                {
                    ExtremeRoleType.Neutral,
                    ISpawnDataManager.ComputeSpawnNum(
                        RoleGlobalOption.MinNeutralRoles,
                        RoleGlobalOption.MaxNeutralRoles)
                },
                {
                    ExtremeRoleType.Impostor,
                    ISpawnDataManager.ComputeSpawnNum(
                        RoleGlobalOption.MinImpostorRoles,
                        RoleGlobalOption.MaxImpostorRoles)
                },
            };

            var allOption = OptionHolder.AllOption;

            foreach (var roleId in ExtremeGameModeManager.Instance.RoleSelector.UseCombRoleType)
            {
                byte combType = (byte)roleId;
                var role = ExtremeRoleManager.CombRole[combType];
                int spawnRate = ISpawnDataManager.ComputePercentage(allOption[
                    role.GetRoleOptionId(RoleCommonOption.SpawnRate)]);
                int roleSet = allOption[
                    role.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();
                bool isMultiAssign = allOption[
                    role.GetRoleOptionId(CombinationRoleCommonOption.IsMultiAssign)].GetValue();

                Logging.Debug($"Role:{role}    SpawnRate:{spawnRate}   RoleSet:{roleSet}");

                if (roleSet <= 0 || spawnRate <= 0.0)
                {
                    continue;
                }

                CurrentCombRoleSpawnData.Add(
                    combType,
                    new CombinationRoleSpawnData(
                        role: role,
                        spawnSetNum: roleSet,
                        spawnRate: spawnRate,
                        isMultiAssign: isMultiAssign));

                if (role is GhostAndAliveCombinationRoleManagerBase ghostComb)
                {
                    this.UseGhostCombRole.Add(((CombinationRoleType)combType, ghostComb));
                }
            }

            foreach (var roleId in ExtremeGameModeManager.Instance.RoleSelector.UseNormalRoleId)
            {
                int intedRoleId = (int)roleId;
                SingleRoleBase role = ExtremeRoleManager.NormalRole[intedRoleId];

                int spawnRate = ISpawnDataManager.ComputePercentage(
                    allOption[role.GetRoleOptionId(RoleCommonOption.SpawnRate)]);
                int roleNum = allOption[
                    role.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                Logging.Debug(
                    $"Role Name:{role.RoleName}  SpawnRate:{spawnRate}   RoleNum:{roleNum}");

                if (roleNum <= 0 || spawnRate <= 0.0)
                {
                    continue;
                }

                CurrentSingleRoleSpawnData[role.Team].Add(
                    intedRoleId, new SingleRoleSpawnData(roleNum, spawnRate));
            }
        }

        public bool IsCanSpawnTeam(ExtremeRoleType roleType, int reduceNum = 1)
        {
            return
                this.MaxRoleNum.TryGetValue(roleType, out int maxNum) &&
                maxNum - reduceNum >= 0;
        }

        public void ReduceSpawnLimit(ExtremeRoleType roleType, int reduceNum = 1)
        {
            this.MaxRoleNum[roleType] = this.MaxRoleNum[roleType] - reduceNum;
        }
    }
}
