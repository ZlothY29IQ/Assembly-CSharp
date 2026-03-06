using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

internal class VirtualStumpTeleporterSerializer : GorillaSerializer
{
	[SerializeField]
	public List<VirtualStumpTeleporter> teleporters = new List<VirtualStumpTeleporter>();

	[SerializeField]
	public List<ParticleSystem> teleporterVFX = new List<ParticleSystem>();

	[SerializeField]
	public List<ParticleSystem> returnVFX = new List<ParticleSystem>();

	[SerializeField]
	public List<AudioSource> teleportAudioSource = new List<AudioSource>();

	[SerializeField]
	public List<AudioClip> teleportingPlayerSoundClips = new List<AudioClip>();

	[SerializeField]
	public List<AudioClip> observerSoundClips = new List<AudioClip>();

	public void NotifyPlayerTeleporting(short teleporterIdx, AudioSource localPlayerTeleporterAudioSource)
	{
		if (teleporterIdx < teleporters.Count && PhotonNetwork.InRoom)
		{
			SendRPC("ActivateTeleportVFX", true, false, teleporterIdx);
		}
	}

	public void NotifyPlayerReturning(short teleporterIdx)
	{
		if (teleporterIdx < teleporters.Count)
		{
			Debug.Log($"[VRTeleporterSerializer::NotifyPlayerReturning] Sending RPC to activate VFX at idx: {teleporterIdx}");
			if (PhotonNetwork.InRoom)
			{
				SendRPC("ActivateTeleportVFX", true, true, teleporterIdx);
			}
		}
	}

	[PunRPC]
	private void ActivateTeleportVFX(bool returning, short teleporterIdx, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ActivateTeleportVFX");
		if (teleporterIdx >= teleporters.Count)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[13].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			VirtualStumpTeleporter virtualStumpTeleporter = teleporters[teleporterIdx];
			if (virtualStumpTeleporter.IsNotNull())
			{
				virtualStumpTeleporter.PlayTeleportEffects(forLocalPlayer: false, !returning);
			}
		}
	}

	public short GetTeleporterIndex(VirtualStumpTeleporter teleporter)
	{
		for (short num = 0; num < teleporters.Count; num++)
		{
			if (teleporters[num] == teleporter)
			{
				return num;
			}
		}
		return -1;
	}
}
