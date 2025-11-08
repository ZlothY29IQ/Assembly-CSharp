using System.Collections.Generic;
using GorillaLocomotion;
using GT_CustomMapSupportRuntime;
using JetBrains.Annotations;
using UnityEngine;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSTeleporter : CMSTrigger
{
	[Tooltip("Teleport points used to return the player to the map. Chosen at random.")]
	[SerializeField]
	[NotNull]
	public List<Transform> TeleportPoints = new List<Transform>();

	public bool matchTeleportPointRotation;

	public bool maintainVelocity;

	public override void CopyTriggerSettings(TriggerSettings settings)
	{
		if (settings.GetType() == typeof(TeleporterSettings))
		{
			TeleporterSettings teleporterSettings = (TeleporterSettings)settings;
			TeleportPoints = teleporterSettings.TeleportPoints;
			matchTeleportPointRotation = teleporterSettings.matchTeleportPointRotation;
			maintainVelocity = teleporterSettings.maintainVelocity;
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
		if (!originatedLocally || !GTPlayer.hasInstance)
		{
			return;
		}
		GTPlayer instance = GTPlayer.Instance;
		if (TeleportPoints.Count != 0)
		{
			Transform transform = TeleportPoints[Random.Range(0, TeleportPoints.Count)];
			if (transform != null)
			{
				instance.TeleportTo(transform, matchTeleportPointRotation, maintainVelocity);
			}
		}
	}
}
