using System;
using UnityEngine;

[Serializable]
public class NativeSizeChangerSettings
{
	public const float MinAllowedSize = 0.1f;

	public const float MaxAllowedSize = 10f;

	private Vector3 worldPosition;

	private float activationTime;

	[Range(0.1f, 10f)]
	public float playerSizeScale = 1f;

	public bool ExpireOnRoomJoin = true;

	public bool ExpireInWater = true;

	public float ExpireAfterSeconds;

	public float ExpireOnDistance;

	public Vector3 WorldPosition
	{
		get
		{
			return worldPosition;
		}
		set
		{
			worldPosition = value;
		}
	}

	public float ActivationTime
	{
		get
		{
			return activationTime;
		}
		set
		{
			activationTime = value;
		}
	}
}
