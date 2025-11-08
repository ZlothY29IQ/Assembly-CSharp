using System;
using System.Collections.Generic;
using GorillaTagScripts;
using UnityEngine;

[Serializable]
internal class HandEffectContext : IFXEffectContextObject
{
	internal List<int> prefabHashes = new List<int> { -1, -1 };

	internal Vector3 position;

	internal Quaternion rotation;

	internal float speed;

	internal Color color = Color.white;

	[SerializeField]
	internal AudioSource handSoundSource;

	internal AudioClip soundFX;

	internal float soundVolume;

	internal float soundPitch;

	internal int separateUpTapCooldownCount;

	[SerializeField]
	internal HandTapOverrides defaultDownTapOverrides;

	internal HandTapOverrides downTapOverrides;

	[SerializeField]
	internal HandTapOverrides defaultUpTapOverrides;

	internal HandTapOverrides upTapOverrides;

	internal bool isDownTap;

	internal bool isLeftHand;

	public List<int> PrefabPoolIds => prefabHashes;

	public Vector3 Position => position;

	public Quaternion Rotation => rotation;

	public float Speed => speed;

	public Color Color => color;

	public AudioSource SoundSource => handSoundSource;

	public AudioClip Sound => soundFX;

	public float Volume => soundVolume;

	public float Pitch => soundPitch;

	public bool SeparateUpTapCooldown
	{
		get
		{
			return separateUpTapCooldownCount > 0;
		}
		set
		{
			separateUpTapCooldownCount = Mathf.Max(separateUpTapCooldownCount + (value ? 1 : (-1)), 0);
		}
	}

	public HandTapOverrides DownTapOverrides
	{
		get
		{
			return downTapOverrides ?? defaultDownTapOverrides;
		}
		set
		{
			downTapOverrides = value;
		}
	}

	public HandTapOverrides UpTapOverrides
	{
		get
		{
			return upTapOverrides ?? defaultUpTapOverrides;
		}
		set
		{
			upTapOverrides = value;
		}
	}

	public event Action<HandEffectContext> handTapDown;

	public event Action<HandEffectContext> handTapUp;

	public void AddFXPrefab(int hash)
	{
		prefabHashes.Add(hash);
	}

	public void RemoveFXPrefab(int hash)
	{
		int num = prefabHashes.IndexOf(hash, 2);
		if (num >= 2)
		{
			prefabHashes.RemoveAt(num);
		}
	}

	public void OnTriggerActions()
	{
		if (isDownTap)
		{
			this.handTapDown?.Invoke(this);
		}
		else
		{
			this.handTapUp?.Invoke(this);
		}
	}

	public void OnPlayVisualFX(int fxID, GameObject fx)
	{
		if (fx.TryGetComponent<FXModifier>(out var component))
		{
			component.UpdateScale(soundVolume * ((fxID == GorillaAmbushManager.HandEffectHash) ? GorillaAmbushManager.HandFXScaleModifier : 1f), color);
		}
	}

	public void OnPlaySoundFX(AudioSource audioSource)
	{
	}
}
