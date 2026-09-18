using System;
using GorillaTag;
using UnityEngine;

[Serializable]
public class RoomSystemEffect
{
	[SerializeField]
	private StaticHashWrapper m_Id;

	public bool Registered { get; set; }

	public StaticHashWrapper ID => m_Id;

	public void PlayNetworkedEffect(RigContainer target, PhotonMessageInfoWrapped info)
	{
		PlayEffectLocal(target);
	}

	public void PlayEffectNetworked(RigContainer player)
	{
		PlayEffectLocal(player);
		if (Registered && RoomSystem.JoinedRoom)
		{
			RoomSystem.PlayEffect(this);
		}
	}

	public void PlayEffectLocal(RigContainer player)
	{
	}

	public void EnableNetworking()
	{
		RoomSystem.AddEffect(this);
	}

	public void DisableNetworking()
	{
		RoomSystem.RemoveEffect(this);
	}
}
