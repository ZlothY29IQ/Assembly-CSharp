using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class EnvironmentProximityReactor : MonoBehaviour
{
	[Serializable]
	public class InteractionBlock
	{
		[Tooltip("Keys this block broadcasts. Cosmetics whose Key List or Listener List contains a matching key can trigger this block.")]
		public List<string> interactionKeys = new List<string>();

		[Tooltip("If the cosmetic broadcasts any of these keys this block will not fire, even if another key matches.")]
		public List<string> ignoreKeys = new List<string>();

		[Tooltip("React when a cosmetic broadcasts one of these keys. Listener keys are never broadcast outward, so two Listener-only objects will never trigger each other.")]
		public List<string> listenerKeys = new List<string>();

		[Tooltip("Distance (m) at which a cosmetic triggers this block.")]
		public float proximityThreshold = 0.3f;

		[Tooltip("Minimum seconds between consecutive OnBelow triggers for this block.")]
		[SerializeField]
		private float cooldownTime = 0.5f;

		[Tooltip("Fires immediately on the client whose cosmetic crossed below the threshold. Local-only")]
		public UnityEvent<Vector3> onBelowLocal;

		[Tooltip("Fires on aLL clients when any player's cosmetic crosses below the threshold.")]
		public UnityEvent<Vector3> onBelowShared;

		[Tooltip("Fires every frame on the triggering client while the cosmetic remains below the threshold. Local-only")]
		public UnityEvent<Vector3> whileBelowLocal;

		[Tooltip("Fires every frame on ALL clients while any player's cosmetic remains below the threshold.")]
		public UnityEvent<Vector3> whileBelowShared;

		[Tooltip("Fires on the triggering client when the cosmetic goes back above the threshold.")]
		public UnityEvent onAboveLocal;

		[Tooltip("Fires on aLL clients when the cosmetic goes back above the threshold.")]
		public UnityEvent onAboveShared;

		[NonSerialized]
		public bool wasBelow;

		[NonSerialized]
		public bool wasSharedBelow;

		[NonSerialized]
		public float lastTriggerTime = -9999f;

		public bool CanTriggerFrom(CosmeticsProximityReactor cosmetic)
		{
			foreach (CosmeticsProximityReactor.InteractionSetting block in cosmetic.blocks)
			{
				if ((block.mode != CosmeticsProximityReactor.InteractionMode.CosmeticToCosmetic && block.mode != CosmeticsProximityReactor.InteractionMode.CosmeticToEnvironment) || block.interactionKeys == null || block.interactionKeys.Count == 0)
				{
					continue;
				}
				if (ignoreKeys != null && ignoreKeys.Count > 0)
				{
					bool flag = false;
					foreach (string interactionKey in block.interactionKeys)
					{
						if (!string.IsNullOrEmpty(interactionKey) && ignoreKeys.Contains(interactionKey))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
				}
				foreach (string interactionKey2 in block.interactionKeys)
				{
					if (!string.IsNullOrEmpty(interactionKey2))
					{
						if (interactionKeys != null && interactionKeys.Contains(interactionKey2))
						{
							return true;
						}
						if (listenerKeys != null && listenerKeys.Contains(interactionKey2))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool CanPlay(float now)
		{
			return now - lastTriggerTime >= cooldownTime;
		}
	}

	public List<InteractionBlock> blocks = new List<InteractionBlock>();

	[Tooltip("Optional collider for precise proximity measurement. If unassigned, the transform position is used.")]
	public Collider proximityCollider;

	public int reactorId;

	public string staticId;

	[Tooltip("Leave off for most objects- the ID is computed automatically from the hierarchy path, type name, and world position, so no manual setup is needed.\n\nEnable only if this object is expected to move or be renamed in the editor after the ID has already been referenced elsewhere When enabled, the ID is pinned to the Static ID string above so it stays stable across repositions. Hit Recalculate once to generate it, then leave it alone.")]
	public bool useStaticId;

	private void OnEnable()
	{
		if (!useStaticId)
		{
			CalculateId();
		}
		EnvironmentProximityReactorManager.Register(this);
	}

	private void OnDisable()
	{
		EnvironmentProximityReactorManager.Unregister(this);
		ResetBlockState();
	}

	private void Update()
	{
		if (CosmeticsProximityReactorManager.Instance == null)
		{
			return;
		}
		IReadOnlyList<CosmeticsProximityReactor> cosmetics = CosmeticsProximityReactorManager.Instance.Cosmetics;
		float time = Time.time;
		for (int i = 0; i < blocks.Count; i++)
		{
			InteractionBlock interactionBlock = blocks[i];
			bool flag = false;
			Vector3 arg = base.transform.position;
			for (int j = 0; j < cosmetics.Count; j++)
			{
				CosmeticsProximityReactor cosmeticsProximityReactor = cosmetics[j];
				if (!(cosmeticsProximityReactor == null))
				{
					VRRig ownerRig = cosmeticsProximityReactor.GetOwnerRig();
					if (!(ownerRig == null) && ownerRig.isLocal && interactionBlock.CanTriggerFrom(cosmeticsProximityReactor) && AreWithinThreshold(cosmeticsProximityReactor, interactionBlock.proximityThreshold, out var contactPoint))
					{
						flag = true;
						arg = contactPoint;
						break;
					}
				}
			}
			if (flag)
			{
				if (!interactionBlock.wasBelow && interactionBlock.CanPlay(time))
				{
					interactionBlock.wasBelow = true;
					interactionBlock.wasSharedBelow = true;
					interactionBlock.lastTriggerTime = time;
					interactionBlock.onBelowLocal?.Invoke(arg);
					interactionBlock.onBelowShared?.Invoke(arg);
					EnvironmentProximityReactorManager.Instance?.BroadcastProximityState(reactorId, i, isBelow: true);
				}
				else if (interactionBlock.wasBelow)
				{
					interactionBlock.whileBelowLocal?.Invoke(arg);
					interactionBlock.whileBelowShared?.Invoke(arg);
					if (!interactionBlock.wasSharedBelow)
					{
						interactionBlock.wasSharedBelow = true;
						interactionBlock.onBelowShared?.Invoke(arg);
						EnvironmentProximityReactorManager.Instance?.BroadcastProximityState(reactorId, i, isBelow: true);
					}
				}
			}
			else if (interactionBlock.wasBelow)
			{
				interactionBlock.wasBelow = false;
				interactionBlock.wasSharedBelow = false;
				interactionBlock.onAboveLocal?.Invoke();
				interactionBlock.onAboveShared?.Invoke();
				EnvironmentProximityReactorManager.Instance?.BroadcastProximityState(reactorId, i, isBelow: false);
			}
			if (interactionBlock.wasSharedBelow && !interactionBlock.wasBelow)
			{
				interactionBlock.whileBelowShared?.Invoke(base.transform.position);
			}
		}
	}

	public void SyncStateTo(NetPlayer newPlayer, EnvironmentProximityReactorManager manager)
	{
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i].wasBelow)
			{
				manager.BroadcastProximityStateTo(newPlayer, reactorId, i, isBelow: true);
			}
		}
	}

	public void ApplySharedProximity(int blockIndex, bool isBelow)
	{
		if (blockIndex >= 0 && blockIndex < blocks.Count)
		{
			InteractionBlock interactionBlock = blocks[blockIndex];
			if (isBelow)
			{
				interactionBlock.wasSharedBelow = true;
				interactionBlock.onBelowShared?.Invoke(base.transform.position);
			}
			else
			{
				interactionBlock.wasSharedBelow = false;
				interactionBlock.onAboveShared?.Invoke();
			}
		}
	}

	private bool AreWithinThreshold(CosmeticsProximityReactor cosmetic, float threshold, out Vector3 contactPoint)
	{
		Vector3 vector = ((cosmetic.collider == null) ? cosmetic.transform.position : cosmetic.collider.ClosestPoint(base.transform.position));
		Vector3 vector2 = ((proximityCollider == null) ? base.transform.position : proximityCollider.ClosestPoint(vector));
		contactPoint = (vector + vector2) * 0.5f;
		return Vector3.Distance(vector, vector2) <= threshold;
	}

	private void CalculateId(bool force = false)
	{
		Transform transform = base.transform;
		int hashCode = TransformUtils.ComputePathHash(transform).ToId128().GetHashCode();
		int staticHash = GetType().Name.GetStaticHash();
		int hashCode2 = transform.position.QuantizedId128().GetHashCode();
		int num = StaticHash.Compute(hashCode, staticHash, hashCode2);
		if (useStaticId)
		{
			if (string.IsNullOrEmpty(staticId) || force)
			{
				int instanceID = transform.GetInstanceID();
				int num2 = StaticHash.Compute(num, instanceID);
				staticId = $"#ID_{num2:X8}";
			}
			reactorId = staticId.GetStaticHash();
		}
		else
		{
			reactorId = (Application.isPlaying ? num : 0);
		}
	}

	private void ResetBlockState()
	{
		foreach (InteractionBlock block in blocks)
		{
			block.wasBelow = false;
			block.wasSharedBelow = false;
			block.lastTriggerTime = -9999f;
		}
	}

	private void EdRecalculateId()
	{
		CalculateId(force: true);
	}
}
