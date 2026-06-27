using System;
using UnityEngine;

public class RoomMuteEnforcer : MonoBehaviour
{
	private void OnEnable()
	{
		RoomControls.OnPlayerMuteChanged += new Action<string, bool>(SetRoomMute);
		VRRigCache.OnRigActivated += SyncRoomMute;
		RoomControls.OnRoomStateLoaded += new Action(SyncAllRoomMutes);
	}

	private void OnDisable()
	{
		RoomControls.OnPlayerMuteChanged -= new Action<string, bool>(SetRoomMute);
		VRRigCache.OnRigActivated -= SyncRoomMute;
		RoomControls.OnRoomStateLoaded -= new Action(SyncAllRoomMutes);
	}

	private void SetRoomMute(string userId, bool muted)
	{
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			if (activeRigContainer.Creator?.UserId == userId)
			{
				activeRigContainer.SetMuted(RigContainer.MuteReason.Room, muted);
				break;
			}
		}
	}

	private void SyncRoomMute(RigContainer rig)
	{
		if (rig.Creator != null)
		{
			rig.SetMuted(RigContainer.MuteReason.Room, RoomControls.MutedPlayers.ContainsKey(rig.Creator.UserId));
		}
	}

	private void SyncAllRoomMutes()
	{
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			SyncRoomMute(activeRigContainer);
		}
	}
}
