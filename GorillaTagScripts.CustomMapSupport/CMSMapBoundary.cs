using System.Collections.Generic;
using GorillaGameModes;
using GorillaLocomotion;
using GT_CustomMapSupportRuntime;
using JetBrains.Annotations;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSMapBoundary : CMSTrigger
{
	[Tooltip("Teleport points used to return the player to the map. Chosen at random.")]
	[SerializeField]
	[NotNull]
	public List<Transform> TeleportPoints = new List<Transform>();

	public bool ShouldTagPlayer = true;

	public override void CopyTriggerSettings(TriggerSettings settings)
	{
		if (settings.GetType() == typeof(MapBoundarySettings))
		{
			MapBoundarySettings mapBoundarySettings = (MapBoundarySettings)settings;
			TeleportPoints = mapBoundarySettings.TeleportPoints;
			ShouldTagPlayer = mapBoundarySettings.ShouldTagPlayer;
		}
		for (int num = TeleportPoints.Count - 1; num >= 0; num--)
		{
			if (TeleportPoints[num] == null)
			{
				TeleportPoints.RemoveAt(num);
			}
		}
		base.CopyTriggerSettings(settings);
	}

	public override void Trigger(double triggerTime = -1.0, bool originatedLocally = false, bool ignoreTriggerCount = false)
	{
		base.Trigger(triggerTime, originatedLocally, ignoreTriggerCount);
		if (originatedLocally && GTPlayer.hasInstance)
		{
			GTPlayer instance = GTPlayer.Instance;
			Transform transform = CustomMapLoader.GetCustomMapsDefaultSpawnLocation();
			if (TeleportPoints.Count != 0)
			{
				transform = TeleportPoints[Random.Range(0, TeleportPoints.Count)];
			}
			if (transform != null)
			{
				instance.TeleportTo(transform, matchDestinationRotation: true, maintainVelocity: false);
			}
			if (ShouldTagPlayer)
			{
				GameMode.ReportHit();
			}
		}
	}
}
