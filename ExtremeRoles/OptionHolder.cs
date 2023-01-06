﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using AmongUs.GameOptions;

using Hazel;
using BepInEx.Configuration;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;

namespace ExtremeRoles
{
    public static class OptionHolder
    {
        public const int VanillaMaxPlayerNum = 15;
        public const int MaxImposterNum = 14;

        private const int singleRoleOptionStartOffset = 100;
        private const int combRoleOptionStartOffset = 5000;
        private const int ghostRoleOptionStartOffset = 10000;
        private const int chunkSize = 50;

        public static readonly string[] SpawnRate = new string[] {
            "0%", "10%", "20%", "30%", "40%",
            "50%", "60%", "70%", "80%", "90%", "100%" };

        public static readonly string[] Range = new string[] { "short", "middle", "long"};

        public static string ConfigPreset
        {
            get => $"Preset:{selectedPreset}";
        }

        public static int OptionsPage = 1;

        public static Dictionary<int, IOption> AllOption = new Dictionary<int, IOption>();
        
        private static readonly string[] optionPreset = new string[] {
            "preset1", "preset2", "preset3", "preset4", "preset5",
            "preset6", "preset7", "preset8", "preset9", "preset10" };

        private static readonly string[] prngAlgorithm = new string[]
        {
            "Pcg32XshRr", "Pcg64RxsMXs",
            "Xorshift64", "Xorshift128",
            "Xorshiro256StarStar",
            "Xorshiro512StarStar",
            "RomuMono", "RomuTrio", "RomuQuad",
            "Seiran128", "Shioi128", "JFT32",
        };

        private static int selectedPreset = 0;
        private static bool isBlockShare = false;

        private static IRegionInfo[] defaultRegion;

        public enum CommonOptionKey
        {
            PresetSelection = 0,

            UseStrongRandomGen,
            UsePrngAlgorithm,

            MinCrewmateRoles,
            MaxCrewmateRoles,
            MinNeutralRoles,
            MaxNeutralRoles,
            MinImpostorRoles,
            MaxImpostorRoles,

            MinCrewmateGhostRoles,
            MaxCrewmateGhostRoles,
            MinNeutralGhostRoles,
            MaxNeutralGhostRoles,
            MinImpostorGhostRoles,
            MaxImpostorGhostRoles,

            UseXion,

            NumMeating,
            ChangeMeetingVoteAreaSort,
            FixedMeetingPlayerLevel,
            DisableSkipInEmergencyMeeting,
            DisableSelfVote,
            DesableVent,
            EngineerUseImpostorVent,
            CanKillVentInPlayer,
            ParallelMedBayScans,
            IsAutoSelectRandomSpawn,
            
            IsRemoveAdmin,
            AirShipEnableAdmin,
            EnableAdminLimit,
            AdminLimitTime,

            IsRemoveSecurity,
            EnableSecurityLimit,
            SecurityLimitTime,

            IsRemoveVital,
            EnableVitalLimit,
            VitalLimitTime,

            RandomMap,
            
            DisableTaskWinWhenNoneTaskCrew,
            DisableTaskWin,
            IsSameNeutralSameWin,
            DisableNeutralSpecialForceEnd,

            IsAssignNeutralToVanillaCrewGhostRole,
            IsRemoveAngleIcon,
            IsBlockGAAbilityReport,
        }

        public enum AirShipAdminMode
        {
            ModeBoth,
            ModeCockpitOnly,
            ModeArchiveOnly
        }

        public static void ExecuteWithBlockOptionShare(Action func)
        {
            isBlockShare = true;
            try
            {
                func();
            }
            catch (Exception e)
            {
                ExtremeRolesPlugin.Logger.LogInfo($"BlockShareExcuteFailed!!:{e}");
            }
            isBlockShare = false;
        }

        public static void Create()
        {

            defaultRegion = ServerManager.DefaultRegions;

            createConfigOption();

            Roles.ExtremeRoleManager.GameRole.Clear();
            AllOption.Clear();

            new SelectionCustomOption(
                (int)CommonOptionKey.PresetSelection, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.PresetSelection.ToString()),
                optionPreset, null, true);

            var strongGen = new BoolCustomOption(
                (int)CommonOptionKey.UseStrongRandomGen, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.UseStrongRandomGen.ToString()), true);
            new SelectionCustomOption(
                (int)CommonOptionKey.UsePrngAlgorithm, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.UsePrngAlgorithm.ToString()),
                prngAlgorithm, strongGen,
                invert: true);

            createExtremeRoleGlobalSpawnOption();
            createExtremeGhostRoleGlobalSpawnOption();

            new BoolCustomOption(
                (int)CommonOptionKey.UseXion,
                Design.ColoedString(
                    ColorPalette.XionBlue,
                    CommonOptionKey.UseXion.ToString()),
                false, null, true);

            createShipGlobalOption();

            Roles.ExtremeRoleManager.CreateNormalRoleOptions(
                singleRoleOptionStartOffset);

            Roles.ExtremeRoleManager.CreateCombinationRoleOptions(
                combRoleOptionStartOffset);

            GhostRoles.ExtremeGhostRoleManager.CreateGhostRoleOption(
                ghostRoleOptionStartOffset);
        }

        public static void Load()
        {
            // 不具合等が発生しないようにブロック機能を有効化する
            isBlockShare = false;

            // This is HotFix for HideNSeekMode
            bool isClassic = GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
            dynamic GetValue(CommonOptionKey key)
            {
                IOption option = AllOption[(int)key];
                return isClassic ? option.GetValue() : option.GetDefault();
            }

            Ship.MaxNumberOfMeeting = GetValue(CommonOptionKey.NumMeating);
            Ship.ChangeMeetingVoteAreaSort = GetValue(CommonOptionKey.ChangeMeetingVoteAreaSort);
            Ship.FixedMeetingPlayerLevel = GetValue(CommonOptionKey.FixedMeetingPlayerLevel);

            Ship.AllowParallelMedBayScan = GetValue(CommonOptionKey.ParallelMedBayScans);
            Ship.BlockSkippingInEmergencyMeeting = GetValue(CommonOptionKey.DisableSkipInEmergencyMeeting);
            Ship.DisableVent = GetValue(CommonOptionKey.DesableVent);
            Ship.CanKillVentInPlayer = GetValue(CommonOptionKey.CanKillVentInPlayer);
            Ship.EngineerUseImpostorVent = GetValue(CommonOptionKey.EngineerUseImpostorVent);
            Ship.DisableSelfVote = GetValue(CommonOptionKey.DisableSelfVote);
            Ship.DisableTaskWinWhenNoneTaskCrew = GetValue(CommonOptionKey.DisableTaskWinWhenNoneTaskCrew);
            Ship.DisableTaskWin = GetValue(CommonOptionKey.DisableTaskWin);
            Ship.IsSameNeutralSameWin = GetValue(CommonOptionKey.IsSameNeutralSameWin);
            Ship.DisableNeutralSpecialForceEnd = GetValue(CommonOptionKey.DisableNeutralSpecialForceEnd);

            Ship.IsAssignNeutralToVanillaCrewGhostRole = GetValue(
                CommonOptionKey.IsAssignNeutralToVanillaCrewGhostRole);
            Ship.IsRemoveAngleIcon = GetValue(CommonOptionKey.IsRemoveAngleIcon);
            Ship.IsBlockGAAbilityReport = GetValue(CommonOptionKey.IsBlockGAAbilityReport);

            Ship.IsAutoSelectRandomSpawn = GetValue(CommonOptionKey.IsAutoSelectRandomSpawn);

            Ship.IsRemoveAdmin = GetValue(CommonOptionKey.IsRemoveAdmin);
            Ship.AirShipEnable = (AirShipAdminMode)GetValue(CommonOptionKey.AirShipEnableAdmin);
            Ship.EnableAdminLimit = GetValue(CommonOptionKey.EnableAdminLimit);
            Ship.AdminLimitTime = GetValue(CommonOptionKey.AdminLimitTime);


            Ship.IsRemoveSecurity = GetValue(CommonOptionKey.IsRemoveSecurity);
            Ship.EnableSecurityLimit = GetValue(CommonOptionKey.EnableSecurityLimit);
            Ship.SecurityLimitTime = GetValue(CommonOptionKey.SecurityLimitTime);

            Ship.IsRemoveVital = GetValue(CommonOptionKey.IsRemoveVital);
            Ship.EnableVitalLimit = GetValue(CommonOptionKey.EnableVitalLimit);
            Ship.VitalLimitTime = GetValue(CommonOptionKey.VitalLimitTime);

            Client.GhostsSeeRole = ConfigParser.GhostsSeeRoles.Value;
            Client.GhostsSeeTask = ConfigParser.GhostsSeeTasks.Value;
            Client.GhostsSeeVote = ConfigParser.GhostsSeeVotes.Value;
            Client.ShowRoleSummary = ConfigParser.ShowRoleSummary.Value;
            Client.HideNamePlate = ConfigParser.HideNamePlate.Value;
        }


        public static void SwitchPreset(int newPreset)
        {
            selectedPreset = newPreset;

            // オプションの共有でネットワーク帯域とサーバーに負荷をかけて人が落ちたりするので共有を一時的に無効化して実行
            ExecuteWithBlockOptionShare(
                () =>
                {
                    foreach (IOption option in AllOption.Values)
                    {
                        if (option.Id == 0) { continue; }
                        option.SwitchPreset();
                    }
                });
            ShareOptionSelections();
        }

        public static void ShareOptionSelections()
        {
            if (isBlockShare) { return; }

            if (PlayerControl.AllPlayerControls.Count <= 1 ||
                AmongUsClient.Instance?.AmHost == false &&
                PlayerControl.LocalPlayer == null) { return; }

            var splitOption = AllOption.Select((x, i) =>
                new { data = x, indexgroup = i / chunkSize })
                .GroupBy(x => x.indexgroup, x => x.data)
                .Select(y => y.Select(x => x));

            foreach (var chunkedOption in splitOption)
            {
                using (var caller = RPCOperator.CreateCaller(
                    RPCOperator.Command.ShareOption))
                {
                    caller.WriteByte((byte)chunkedOption.Count());
                    foreach (var (id, option) in chunkedOption)
                    {
                        caller.WritePackedInt(id);
                        caller.WritePackedInt(option.CurSelection);
                    }
                }
            }
        }
        public static void ShareOption(int numberOfOptions, MessageReader reader)
        {
            try
            {
                for (int i = 0; i < numberOfOptions; i++)
                {
                    int optionId = reader.ReadPackedInt32();
                    int selection = reader.ReadPackedInt32();
                    lock (AllOption)
                    {
                        AllOption[optionId].UpdateSelection(selection);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error($"Error while deserializing options:{e.Message}");
            }
        }

        public static void UpdateRegion()
        {
            ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
            IRegionInfo[] regions = defaultRegion;

            IRegionInfo CustomRegion = null;
            if (ConfigParser.Ip.Value.Contains("://"))
            {
                try
                {
                    var serverUri = new Uri(ConfigParser.Ip.Value);
                    var serverHost = serverUri.Host;

                    var serverInfo = new ServerInfo("custom", ConfigParser.Ip.Value, ConfigParser.Port.Value, false);
                    CustomRegion = new StaticHttpRegionInfo(
                        "custom",
                        StringNames.NoTranslation,
                        serverHost,
                        new ServerInfo[1] { serverInfo }).Cast<IRegionInfo>();
                } catch (UriFormatException)
                {
                    CustomRegion = new DnsRegionInfo(
                        ConfigParser.Ip.Value,
                        "custom",
                        StringNames.NoTranslation,
                        ConfigParser.Ip.Value,
                        ConfigParser.Port.Value,
                        false).Cast<IRegionInfo>();
                }
            } else
            {
                CustomRegion = new DnsRegionInfo(
                    ConfigParser.Ip.Value,
                    "custom",
                    StringNames.NoTranslation,
                    ConfigParser.Ip.Value,
                    ConfigParser.Port.Value,
                    false).Cast<IRegionInfo>();
            }
            regions = regions.Concat(new IRegionInfo[] { CustomRegion }).ToArray();
            ServerManager.DefaultRegions = regions;
            serverManager.AvailableRegions = regions;
        }

        private static void createConfigOption()
        {
            var config = ExtremeRolesPlugin.Instance.Config;

            ConfigParser.GhostsSeeTasks = config.Bind(
                "ClientOption", "GhostCanSeeRemainingTasks", true);
            ConfigParser.GhostsSeeRoles = config.Bind(
                "ClientOption", "GhostCanSeeRoles", true);
            ConfigParser.GhostsSeeVotes = config.Bind(
                "ClientOption", "GhostCanSeeVotes", true);
            ConfigParser.ShowRoleSummary = config.Bind(
                "ClientOption", "IsShowRoleSummary", true);
            ConfigParser.HideNamePlate = config.Bind(
                "ClientOption", "IsHideNamePlate", false);

            ConfigParser.StreamerModeReplacementText = config.Bind(
                "ClientOption",
                "ReplacementRoomCodeText",
                "Playing with Extreme Roles");

            ConfigParser.Ip = config.Bind(
                "ClientOption", "CustomServerIP", "127.0.0.1");
            ConfigParser.Port = config.Bind(
                "ClientOption", "CustomServerPort", (ushort)22023);
        }

        private static void createExtremeRoleGlobalSpawnOption()
        {
            new IntCustomOption(
                (int)CommonOptionKey.MinCrewmateRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinCrewmateRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 1) * 2, 1, null, true);
            new IntCustomOption(
                (int)CommonOptionKey.MaxCrewmateRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxCrewmateRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 1) * 2, 1);

            new IntCustomOption(
                (int)CommonOptionKey.MinNeutralRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinNeutralRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 2) * 2, 1);
            new IntCustomOption(
                (int)CommonOptionKey.MaxNeutralRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxNeutralRoles.ToString()),
                0, 0, (VanillaMaxPlayerNum - 2) * 2, 1);

            new IntCustomOption(
                (int)CommonOptionKey.MinImpostorRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinImpostorRoles.ToString()),
                0, 0, MaxImposterNum * 2, 1);
            new IntCustomOption(
                (int)CommonOptionKey.MaxImpostorRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxImpostorRoles.ToString()),
                0, 0, MaxImposterNum * 2, 1);
        }

        private static void createExtremeGhostRoleGlobalSpawnOption()
        {
            new IntCustomOption(
                (int)CommonOptionKey.MinCrewmateGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinCrewmateGhostRoles.ToString()),
                0, 0, VanillaMaxPlayerNum - 1, 1, null, true);
            new IntCustomOption(
                (int)CommonOptionKey.MaxCrewmateGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxCrewmateGhostRoles.ToString()),
                0, 0, VanillaMaxPlayerNum - 1, 1);

            new IntCustomOption(
                (int)CommonOptionKey.MinNeutralGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinNeutralGhostRoles.ToString()),
                0, 0, VanillaMaxPlayerNum - 2, 1);
            new IntCustomOption(
                (int)CommonOptionKey.MaxNeutralGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxNeutralGhostRoles.ToString()),
                0, 0, VanillaMaxPlayerNum - 2, 1);

            new IntCustomOption(
                (int)CommonOptionKey.MinImpostorGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MinImpostorGhostRoles.ToString()),
                0, 0, MaxImposterNum, 1);
            new IntCustomOption(
                (int)CommonOptionKey.MaxImpostorGhostRoles, Design.ColoedString(
                    new Color(204f / 255f, 204f / 255f, 0, 1f),
                    CommonOptionKey.MaxImpostorGhostRoles.ToString()),
                0, 0, MaxImposterNum, 1);
        }


        private static void createShipGlobalOption()
        {
            new IntCustomOption(
                (int)CommonOptionKey.NumMeating,
                CommonOptionKey.NumMeating.ToString(),
                10, 0, 100, 1, null, true);
            new BoolCustomOption(
              (int)CommonOptionKey.ChangeMeetingVoteAreaSort,
              CommonOptionKey.ChangeMeetingVoteAreaSort.ToString(),
              false);
            new BoolCustomOption(
               (int)CommonOptionKey.FixedMeetingPlayerLevel,
               CommonOptionKey.FixedMeetingPlayerLevel.ToString(),
               false);
            new BoolCustomOption(
                (int)CommonOptionKey.DisableSkipInEmergencyMeeting,
                CommonOptionKey.DisableSkipInEmergencyMeeting.ToString(),
                false);
            new BoolCustomOption(
                (int)CommonOptionKey.DisableSelfVote,
                CommonOptionKey.DisableSelfVote.ToString(),
                false);


            var ventOption = new BoolCustomOption(
               (int)CommonOptionKey.DesableVent,
               CommonOptionKey.DesableVent.ToString(),
               false);
            new BoolCustomOption(
                (int)CommonOptionKey.CanKillVentInPlayer,
                CommonOptionKey.CanKillVentInPlayer.ToString(),
                false, ventOption, invert: true);
            new BoolCustomOption(
                (int)CommonOptionKey.EngineerUseImpostorVent,
                CommonOptionKey.EngineerUseImpostorVent.ToString(),
                false, ventOption, invert: true);

            new BoolCustomOption(
                (int)CommonOptionKey.ParallelMedBayScans,
                CommonOptionKey.ParallelMedBayScans.ToString(), false);

            new BoolCustomOption(
                (int)CommonOptionKey.IsAutoSelectRandomSpawn,
                CommonOptionKey.IsAutoSelectRandomSpawn.ToString(), false);

            var adminOpt = new BoolCustomOption(
                (int)CommonOptionKey.IsRemoveAdmin,
                CommonOptionKey.IsRemoveAdmin.ToString(),
                false);
            new SelectionCustomOption(
                (int)CommonOptionKey.AirShipEnableAdmin,
                CommonOptionKey.AirShipEnableAdmin.ToString(),
                new string[]
                {
                    AirShipAdminMode.ModeBoth.ToString(),
                    AirShipAdminMode.ModeCockpitOnly.ToString(),
                    AirShipAdminMode.ModeArchiveOnly.ToString(),
                },
                adminOpt,
                invert: true);
            var adminLimitOpt = new BoolCustomOption(
                (int)CommonOptionKey.EnableAdminLimit,
                CommonOptionKey.EnableAdminLimit.ToString(),
                false, adminOpt,
                invert: true);
            new FloatCustomOption(
                (int)CommonOptionKey.AdminLimitTime,
                CommonOptionKey.AdminLimitTime.ToString(),
                30.0f, 5.0f, 120.0f, 0.5f, adminLimitOpt,
                format: OptionUnit.Second,
                invert: true,
                enableCheckOption: adminLimitOpt);

            var secOpt = new BoolCustomOption(
                (int)CommonOptionKey.IsRemoveSecurity,
                CommonOptionKey.IsRemoveSecurity.ToString(),
                false);
            var secLimitOpt = new BoolCustomOption(
                (int)CommonOptionKey.EnableSecurityLimit,
                CommonOptionKey.EnableSecurityLimit.ToString(),
                false, secOpt,
                invert: true);
            new FloatCustomOption(
                (int)CommonOptionKey.SecurityLimitTime,
                CommonOptionKey.SecurityLimitTime.ToString(),
                30.0f, 5.0f, 120.0f, 0.5f, secLimitOpt,
                format: OptionUnit.Second,
                invert: true,
                enableCheckOption: secLimitOpt);

            var vitalOpt = new BoolCustomOption(
                (int)CommonOptionKey.IsRemoveVital,
                CommonOptionKey.IsRemoveVital.ToString(),
                false);
            var vitalLimitOpt = new BoolCustomOption(
                (int)CommonOptionKey.EnableVitalLimit,
                CommonOptionKey.EnableVitalLimit.ToString(),
                false, vitalOpt,
                invert: true);
            new FloatCustomOption(
                (int)CommonOptionKey.VitalLimitTime,
                CommonOptionKey.VitalLimitTime.ToString(),
                30.0f, 5.0f, 120.0f, 0.5f, vitalLimitOpt,
                format: OptionUnit.Second,
                invert: true,
                enableCheckOption: vitalLimitOpt);


            new BoolCustomOption(
                (int)CommonOptionKey.RandomMap,
                CommonOptionKey.RandomMap.ToString(), false);

            var taskDisableOpt = new BoolCustomOption(
                (int)CommonOptionKey.DisableTaskWinWhenNoneTaskCrew,
                CommonOptionKey.DisableTaskWinWhenNoneTaskCrew.ToString(),
                false);
            new BoolCustomOption(
                (int)CommonOptionKey.DisableTaskWin,
                CommonOptionKey.DisableTaskWin.ToString(),
                false, taskDisableOpt);


            new BoolCustomOption(
                (int)CommonOptionKey.IsSameNeutralSameWin,
                CommonOptionKey.IsSameNeutralSameWin.ToString(),
                true);
            new BoolCustomOption(
                (int)CommonOptionKey.DisableNeutralSpecialForceEnd,
                CommonOptionKey.DisableNeutralSpecialForceEnd.ToString(),
                false);


            new BoolCustomOption(
                (int)CommonOptionKey.IsAssignNeutralToVanillaCrewGhostRole,
                CommonOptionKey.IsAssignNeutralToVanillaCrewGhostRole.ToString(),
                true);
            new BoolCustomOption(
                (int)CommonOptionKey.IsRemoveAngleIcon,
                CommonOptionKey.IsRemoveAngleIcon.ToString(),
                false);
            new BoolCustomOption(
                (int)CommonOptionKey.IsBlockGAAbilityReport,
                CommonOptionKey.IsBlockGAAbilityReport.ToString(),
                false);
        }


        public static class ConfigParser
        {
            public static ConfigEntry<string> StreamerModeReplacementText { get; set; }
            public static ConfigEntry<bool> GhostsSeeTasks { get; set; }
            public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
            public static ConfigEntry<bool> GhostsSeeVotes { get; set; }
            public static ConfigEntry<bool> ShowRoleSummary { get; set; }
            public static ConfigEntry<bool> HideNamePlate { get; set; }
            public static ConfigEntry<string> Ip { get; set; }
            public static ConfigEntry<ushort> Port { get; set; }
        }

        public static class Client
        {
            public static bool GhostsSeeRole = true;
            public static bool GhostsSeeTask = true;
            public static bool GhostsSeeVote = true;
            public static bool ShowRoleSummary = true;
            public static bool HideNamePlate = false;
        }

        public static class Ship
        {
            public const int SameNeutralGameControlId = int.MaxValue;

            public static int MaxNumberOfMeeting = 100;

            public static bool ChangeMeetingVoteAreaSort = true;
            public static bool FixedMeetingPlayerLevel = false;
            public static bool AllowParallelMedBayScan = false;
            public static bool BlockSkippingInEmergencyMeeting = false;
            
            public static bool DisableVent = false;
            public static bool EngineerUseImpostorVent = false;
            public static bool CanKillVentInPlayer = false;

            public static bool IsAutoSelectRandomSpawn = false;

            public static bool IsRemoveAdmin = false;
            public static AirShipAdminMode AirShipEnable = AirShipAdminMode.ModeBoth;
            public static bool EnableAdminLimit = false;
            public static float AdminLimitTime = 0.0f;

            public static bool IsRemoveSecurity = false;
            public static bool EnableSecurityLimit = false;
            public static float SecurityLimitTime = 0.0f;

            public static bool IsRemoveVital = false;
            public static bool EnableVitalLimit = false;
            public static float VitalLimitTime = 0.0f;

            public static bool DisableSelfVote = false;

            public static bool DisableTaskWinWhenNoneTaskCrew = false;
            public static bool DisableTaskWin = false;
            public static bool IsSameNeutralSameWin = true;
            public static bool DisableNeutralSpecialForceEnd = false;

            public static bool IsAssignNeutralToVanillaCrewGhostRole = true;
            public static bool IsRemoveAngleIcon = false;
            public static bool IsBlockGAAbilityReport = false;
        }
    }
}
