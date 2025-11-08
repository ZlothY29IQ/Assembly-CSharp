using System;
using GorillaTag;
using UnityEngine;
using UnityEngine.Events;

public class HandTapEffect : MonoBehaviour
{
	[Serializable]
	public class HandTapEffectDownUp
	{
		public HandTapBehaviour[] onTapBehaviours;

		public UnityEvent onTapUnityEvents;

		[Tooltip("Must be in the global object pool and have a tag.\n\nPrefabs can have an FXModifier component to be adjusted after creation.")]
		public HashWrapper onTapPrefabToSpawn;

		public HandTapOverrides overrides;

		public bool HasOverrides
		{
			get
			{
				if (!overrides.overrideSurfacePrefab && !overrides.overrideGamemodePrefab)
				{
					return overrides.overrideSound;
				}
				return true;
			}
		}

		internal void OnTap(HandEffectContext handContext)
		{
			onTapUnityEvents?.Invoke();
			for (int i = 0; i < onTapBehaviours.Length; i++)
			{
				onTapBehaviours[i].OnTap(handContext);
			}
		}
	}

	[Serializable]
	public class HandTapEffectLeftRight
	{
		public bool separateUpTapCooldown;

		public HandTapEffectDownUp downTapEffect;

		public HandTapEffectDownUp upTapEffect;

		internal HandEffectContext handContext;

		public void OnEnable()
		{
			if (separateUpTapCooldown)
			{
				handContext.SeparateUpTapCooldown = true;
			}
			if ((int)downTapEffect.onTapPrefabToSpawn != -1)
			{
				handContext.AddFXPrefab(downTapEffect.onTapPrefabToSpawn);
			}
			if (downTapEffect.HasOverrides)
			{
				handContext.DownTapOverrides = downTapEffect.overrides;
			}
			if (upTapEffect.HasOverrides)
			{
				handContext.UpTapOverrides = upTapEffect.overrides;
			}
			handContext.handTapDown += downTapEffect.OnTap;
			handContext.handTapUp += upTapEffect.OnTap;
		}

		public void OnDisable()
		{
			if (separateUpTapCooldown)
			{
				handContext.SeparateUpTapCooldown = false;
			}
			if ((int)downTapEffect.onTapPrefabToSpawn != -1)
			{
				handContext.RemoveFXPrefab(downTapEffect.onTapPrefabToSpawn);
			}
			if (downTapEffect.HasOverrides && handContext.DownTapOverrides == downTapEffect.overrides)
			{
				handContext.DownTapOverrides = null;
			}
			if (upTapEffect.HasOverrides && handContext.UpTapOverrides == upTapEffect.overrides)
			{
				handContext.UpTapOverrides = null;
			}
			handContext.handTapDown -= downTapEffect.OnTap;
			handContext.handTapUp -= upTapEffect.OnTap;
		}
	}

	public HandTapEffectLeftRight leftHandEffect;

	public HandTapEffectLeftRight rightHandEffect;

	private void Awake()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>();
		leftHandEffect.handContext = componentInParent.LeftHandEffect;
		rightHandEffect.handContext = componentInParent.RightHandEffect;
	}

	private void OnEnable()
	{
		leftHandEffect.OnEnable();
		rightHandEffect.OnEnable();
	}

	private void OnDisable()
	{
		leftHandEffect.OnDisable();
		rightHandEffect.OnDisable();
	}
}
