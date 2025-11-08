using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class VoiceBroadcastCosmeticWearable : MonoBehaviour, IGorillaSliceableSimple
{
	public TalkingCosmeticType talkingCosmeticType;

	[SerializeField]
	private bool headDistanceActivation = true;

	[SerializeField]
	private float headDistance = 0.4f;

	[SerializeField]
	private float toggleCooldown = 0.5f;

	private bool toggleState;

	private float lastToggleTime;

	[SerializeField]
	private UnityEvent onStartListening;

	[SerializeField]
	private UnityEvent onStopListening;

	private List<VoiceBroadcastCosmetic> voiceBroadcasters;

	private Collider playerHeadCollider;

	private void Start()
	{
		VoiceBroadcastCosmetic[] componentsInChildren = GetComponentInParent<VRRig>().GetComponentsInChildren<VoiceBroadcastCosmetic>(includeInactive: true);
		voiceBroadcasters = new List<VoiceBroadcastCosmetic>();
		VoiceBroadcastCosmetic[] array = componentsInChildren;
		foreach (VoiceBroadcastCosmetic voiceBroadcastCosmetic in array)
		{
			if (voiceBroadcastCosmetic.talkingCosmeticType == talkingCosmeticType)
			{
				voiceBroadcasters.Add(voiceBroadcastCosmetic);
				voiceBroadcastCosmetic.SetWearable(this);
			}
		}
	}

	public void OnEnable()
	{
		if (playerHeadCollider == null)
		{
			playerHeadCollider = GetComponentInParent<VRRig>()?.rigContainer.HeadCollider;
		}
		if (headDistanceActivation && playerHeadCollider != null)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		if (!(Time.time - lastToggleTime >= toggleCooldown))
		{
			return;
		}
		bool flag = (base.transform.position - playerHeadCollider.transform.position).sqrMagnitude <= headDistance * headDistance;
		if (flag != toggleState)
		{
			toggleState = flag;
			lastToggleTime = Time.time;
			if (flag)
			{
				onStartListening?.Invoke();
			}
			else
			{
				onStopListening?.Invoke();
			}
			for (int i = 0; i < voiceBroadcasters.Count; i++)
			{
				voiceBroadcasters[i].SetListenState(flag);
			}
		}
	}

	public void OnCosmeticStartListening()
	{
		if (!headDistanceActivation)
		{
			onStartListening?.Invoke();
		}
	}

	public void OnCosmeticStopListening()
	{
		if (!headDistanceActivation)
		{
			onStopListening?.Invoke();
		}
	}
}
