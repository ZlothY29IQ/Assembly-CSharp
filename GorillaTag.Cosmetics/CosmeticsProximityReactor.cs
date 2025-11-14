using System;
using System.Collections.Generic;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class CosmeticsProximityReactor : MonoBehaviour, ISpawnable
{
	public enum ItemKind
	{
		Cosmetic,
		GorillaBody
	}

	[Flags]
	public enum GorillaBodyPart
	{
		None = 0,
		HandLeft = 1,
		HandRight = 2,
		Mouth = 4
	}

	public enum InteractionMode
	{
		CosmeticToCosmetic,
		GorillaBodyToCosmetic
	}

	public enum TargetType
	{
		Owner,
		Others,
		All
	}

	[Serializable]
	public class InteractionSetting
	{
		[Tooltip("Determines what type of interaction this block handles.\n• CosmeticToCosmetic: triggers when two cosmetics with matching keys are nearby.\n• GorillaBodyToCosmetic: triggers when a Gorilla body part (hand, head, etc.) is near this cosmetic.")]
		public InteractionMode mode;

		[Tooltip("List of shared string identifiers that link this cosmetic to others.\nCosmetics with matching keys can trigger interactions with each other.")]
		public List<string> interactionKeys = new List<string>();

		[Tooltip("Specifies which Gorilla body parts (e.g., Hands, Head) can trigger this interaction.\nUse this when the Mode is set to GorillaBodyToCosmetic.")]
		public GorillaBodyPart gorillaBodyMask;

		[Tooltip("The distance threshold (in meters) for triggering the interaction.\nIf another object enters this range, the OnBelow and WhileBelow events are fired.")]
		public float proximityThreshold = 0.15f;

		[Tooltip("Minimum time (in seconds) between consecutive triggers for this interaction block.\nPrevents rapid re-triggering when objects remain within proximity.")]
		[SerializeField]
		private float cooldownTime = 0.5f;

		[Tooltip("Who is allowed to trigger this block (if gorilla body part is selected).\n• Owner: only this cosmetic's own rig/body can trigger this.\n• Others: only other players' rigs/bodies can trigger this.\n• All: anyone can trigger.\n\nNote: everyone will still be able to see the result when it triggers.")]
		public TargetType targetType = TargetType.All;

		public UnityEvent<Vector3> onBelowLocal;

		public UnityEvent<Vector3> onBelowShared;

		public UnityEvent<Vector3> whileBelowLocal;

		public UnityEvent<Vector3> whileBelowShared;

		public UnityEvent onAboveLocal;

		public UnityEvent onAboveShared;

		[NonSerialized]
		public bool wasBelow;

		[NonSerialized]
		public bool isMatched;

		[NonSerialized]
		public float lastEffectTime = -9999f;

		public bool IsCosmeticToCosmetic()
		{
			return mode == InteractionMode.CosmeticToCosmetic;
		}

		public bool IsGorillaBodyToCosmetic()
		{
			return mode == InteractionMode.GorillaBodyToCosmetic;
		}

		public bool AcceptsGorillaBodyPart(GorillaBodyPart kind)
		{
			if (mode != InteractionMode.GorillaBodyToCosmetic)
			{
				return false;
			}
			return (gorillaBodyMask & kind) != 0;
		}

		public bool SharesKeyWith(InteractionSetting other)
		{
			if (mode != 0)
			{
				return false;
			}
			if (other == null)
			{
				return false;
			}
			if (other.mode != 0)
			{
				return false;
			}
			if (interactionKeys == null || other.interactionKeys == null)
			{
				return false;
			}
			foreach (string interactionKey in interactionKeys)
			{
				if (!string.IsNullOrEmpty(interactionKey) && other.interactionKeys.Contains(interactionKey))
				{
					return true;
				}
			}
			return false;
		}

		public bool CanPlay(float now)
		{
			return now - lastEffectTime >= cooldownTime;
		}

		public void FireBelow(VRRig rig, Vector3 contact, float now)
		{
			if (!wasBelow && CanPlay(now))
			{
				if (rig != null && rig.isLocal)
				{
					onBelowLocal?.Invoke(contact);
				}
				onBelowShared?.Invoke(contact);
				wasBelow = true;
				lastEffectTime = now;
			}
		}

		public void FireWhile(VRRig rig, Vector3 contact)
		{
			if (rig != null && rig.isLocal)
			{
				whileBelowLocal?.Invoke(contact);
			}
			whileBelowShared?.Invoke(contact);
		}

		public void FireAbove(VRRig rig)
		{
			if (wasBelow)
			{
				if (rig != null && rig.isLocal)
				{
					onAboveLocal?.Invoke();
				}
				onAboveShared?.Invoke();
				wasBelow = false;
				isMatched = false;
			}
		}

		public bool AllowsRig(VRRig myRig, VRRig otherRig)
		{
			if (myRig == null || otherRig == null)
			{
				return true;
			}
			return targetType switch
			{
				TargetType.Owner => (object)myRig == otherRig, 
				TargetType.Others => (object)myRig != otherRig, 
				_ => true, 
			};
		}
	}

	[Tooltip("Is this object a Cosmetic or a gorilla body part like hand? (gorilla body slot is reserved for Gorilla Player Networked)")]
	public ItemKind itemKind;

	[FormerlySerializedAs("sourceKinds")]
	public GorillaBodyPart gorillaBodyParts;

	public List<InteractionSetting> blocks = new List<InteractionSetting>();

	[Tooltip("If enabled, this cosmetic ignores other instances that share the same PlayFabID.")]
	public bool ignoreSameCosmeticInstances;

	public string PlayFabID = "";

	[Tooltip("If collider is not assigned, we will use the position of this object to find the distance between two cosmetic/body part")]
	public Collider collider;

	private RubberDuckEvents _events;

	public bool IsMatched { get; set; }

	private VRRig MyRig { get; set; }

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public bool IsBelow
	{
		get
		{
			foreach (InteractionSetting block in blocks)
			{
				if (block.wasBelow)
				{
					return true;
				}
			}
			return false;
		}
	}

	public void OnSpawn(VRRig rig)
	{
		if (MyRig == null)
		{
			MyRig = rig;
		}
	}

	public void OnDespawn()
	{
	}

	private void Start()
	{
		IsMatched = false;
		if (CosmeticsProximityReactorManager.Instance != null)
		{
			CosmeticsProximityReactorManager.Instance.Register(this);
		}
	}

	private void OnEnable()
	{
		if (MyRig == null)
		{
			MyRig = GetComponentInParent<VRRig>();
		}
		if (CosmeticsProximityReactorManager.Instance != null)
		{
			CosmeticsProximityReactorManager.Instance.Register(this);
		}
	}

	private void OnDisable()
	{
		if ((bool)CosmeticsProximityReactorManager.Instance)
		{
			CosmeticsProximityReactorManager.Instance.Unregister(this);
		}
	}

	public IReadOnlyList<string> GetTypes()
	{
		List<string> sharedKeysCache = CosmeticsProximityReactorManager.SharedKeysCache;
		sharedKeysCache.Clear();
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode != 0 || block.interactionKeys == null || block.interactionKeys.Count == 0)
			{
				continue;
			}
			foreach (string interactionKey in block.interactionKeys)
			{
				if (!string.IsNullOrEmpty(interactionKey) && !sharedKeysCache.Contains(interactionKey))
				{
					sharedKeysCache.Add(interactionKey);
				}
			}
		}
		return sharedKeysCache;
	}

	public bool IsGorillaBody()
	{
		return itemKind == ItemKind.GorillaBody;
	}

	public bool IsCosmeticItem()
	{
		return itemKind == ItemKind.Cosmetic;
	}

	public bool AcceptsAnySource()
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.gorillaBodyMask != 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool AcceptsThisSource(GorillaBodyPart kind)
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.AcceptsGorillaBodyPart(kind))
			{
				return true;
			}
		}
		return false;
	}

	public float GetCosmeticPairThresholdWith(CosmeticsProximityReactor other, out bool any)
	{
		any = false;
		float num = float.MaxValue;
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode != 0 || !block.AllowsRig(MyRig, other.MyRig))
			{
				continue;
			}
			foreach (InteractionSetting block2 in other.blocks)
			{
				if (block2.mode == InteractionMode.CosmeticToCosmetic && block2.AllowsRig(other.MyRig, MyRig) && block.SharesKeyWith(block2))
				{
					any = true;
					float num2 = Mathf.Min(block.proximityThreshold, block2.proximityThreshold);
					if (num2 < num)
					{
						num = num2;
					}
				}
			}
		}
		return num;
	}

	public float GetSourceThresholdFor(CosmeticsProximityReactor gorillaBody, out bool any)
	{
		any = false;
		float num = float.MaxValue;
		GorillaBodyPart kind = gorillaBody.gorillaBodyParts;
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.AcceptsGorillaBodyPart(kind) && block.AllowsRig(MyRig, gorillaBody.MyRig))
			{
				any = true;
				if (block.proximityThreshold < num)
				{
					num = block.proximityThreshold;
				}
			}
		}
		return num;
	}

	public void OnCosmeticBelowWith(CosmeticsProximityReactor other, Vector3 contact)
	{
		float time = Time.time;
		bool flag = false;
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode != 0 || !block.AllowsRig(MyRig, other.MyRig))
			{
				continue;
			}
			bool flag2 = false;
			foreach (InteractionSetting block2 in other.blocks)
			{
				if (block2.mode == InteractionMode.CosmeticToCosmetic && block2.AllowsRig(other.MyRig, MyRig) && block.SharesKeyWith(block2))
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				block.FireBelow(MyRig, contact, time);
				if (block.wasBelow)
				{
					block.isMatched = true;
					flag = true;
				}
			}
		}
		if (flag)
		{
			IsMatched = true;
		}
	}

	public void WhileCosmeticBelowWith(CosmeticsProximityReactor other, Vector3 contact)
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode != 0 || !block.isMatched || !block.AllowsRig(MyRig, other.MyRig))
			{
				continue;
			}
			bool flag = false;
			foreach (InteractionSetting block2 in other.blocks)
			{
				if (block2.mode == InteractionMode.CosmeticToCosmetic && block2.AllowsRig(other.MyRig, MyRig) && block.SharesKeyWith(block2))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				block.FireWhile(MyRig, contact);
			}
		}
	}

	public void OnCosmeticAboveAll()
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.CosmeticToCosmetic && block.isMatched)
			{
				block.FireAbove(MyRig);
			}
		}
		RefreshAggregateMatched();
	}

	public void OnSourceBelow(Vector3 contact, GorillaBodyPart kind, VRRig sourceRig)
	{
		float time = Time.time;
		bool flag = false;
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.AcceptsGorillaBodyPart(kind) && block.AllowsRig(MyRig, sourceRig))
			{
				block.FireBelow(MyRig, contact, time);
				if (block.wasBelow)
				{
					block.isMatched = true;
					flag = true;
				}
			}
		}
		if (flag)
		{
			RefreshAggregateMatched();
		}
	}

	public void WhileSourceBelow(Vector3 contact, GorillaBodyPart kind, VRRig sourceRig)
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.AcceptsGorillaBodyPart(kind) && block.isMatched && block.AllowsRig(MyRig, sourceRig))
			{
				block.FireWhile(MyRig, contact);
			}
		}
	}

	public void OnSourceAboveAll()
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.isMatched)
			{
				block.FireAbove(MyRig);
			}
		}
		RefreshAggregateMatched();
	}

	public bool HasAnyCosmeticMatch()
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.CosmeticToCosmetic && block.isMatched)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasAnyGorillaBodyPartMatch()
	{
		foreach (InteractionSetting block in blocks)
		{
			if (block.mode == InteractionMode.GorillaBodyToCosmetic && block.isMatched)
			{
				return true;
			}
		}
		return false;
	}

	public void RefreshAggregateMatched()
	{
		IsMatched = HasAnyCosmeticMatch() || HasAnyGorillaBodyPartMatch();
	}
}
