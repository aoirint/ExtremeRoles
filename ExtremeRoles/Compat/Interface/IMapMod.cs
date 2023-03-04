﻿using System.Collections.Generic;
using UnityEngine;

namespace ExtremeRoles.Compat.Interface
{
	public enum SystemConsoleType : byte
	{
		Admin,
		SecurityCamera,
		Vital,
		EmergencyButton
	}

	public enum MapRpcCall : byte
    {
		RepairAllSabo,
		RepairCustomSaboType
    }

	public enum CustomMonoBehaviourType
    {
		MovableFloorBehaviour
    }

	public interface IMapMod
    {
		public const byte RpcCallType = 1;
		public ShipStatus.MapType MapType { get; }
		public bool CanPlaceCamera { get; }
		public bool IsCustomCalculateLightRadius { get; }
		public void Awake(ShipStatus map);
		public void Destroy();
		public float CalculateLightRadius(GameData.PlayerInfo player, bool neutral, bool neutralImpostor);
		public float CalculateLightRadius(GameData.PlayerInfo player, float visionMod, bool applayVisionEffects = true);
		public bool IsCustomSabotageNow();
		public bool IsCustomSabotageTask(TaskTypes saboTask);
		public bool IsCustomVentUse(Vent vent);
		public (float, bool, bool) IsCustomVentUseResult(
			Vent vent, GameData.PlayerInfo player, bool isVentUse);
		public void RpcRepairCustomSabotage();
		public void RpcRepairCustomSabotage(TaskTypes saboTask);
		public void RepairCustomSabotage();
		public void RepairCustomSabotage(TaskTypes saboTask);
		public Console GetConsole(TaskTypes task);
		public HashSet<string> GetSystemObjectName(SystemConsoleType sysConsole);
		public SystemConsole GetSystemConsole(SystemConsoleType sysConsole);
		public List<Vector2> GetSpawnPos(byte playerId);
		public void AddCustomComponent(
			GameObject addObject, CustomMonoBehaviourType customMonoType);

		public void SetUpNewCamera(SurvCamera camera);

	}
}
