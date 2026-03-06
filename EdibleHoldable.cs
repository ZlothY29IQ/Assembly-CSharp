using System;
using GorillaExtensions;
using GorillaTag;
using UnityEngine;
using UnityEngine.Events;

public class EdibleHoldable : TransferrableObject
{
	private enum EdibleHoldableStates
	{
		EatingState0 = 1,
		EatingState1 = 2,
		EatingState2 = 4,
		EatingState3 = 8
	}

	[Serializable]
	public class BiteEvent : UnityEvent<VRRig, int>
	{
	}

	public AudioClip[] eatSounds;

	public GameObject[] edibleMeshObjects;

	public BiteEvent onBiteView;

	public BiteEvent onBiteWorld;

	[DebugReadout]
	public float lastEatTime;

	[DebugReadout]
	public float lastFullyEatenTime;

	public float eatMinimumCooldown = 1f;

	public float respawnTime = 7f;

	public float biteDistance = 0.1666667f;

	public Vector3 biteOffset = new Vector3(0f, 0.0208f, 0.171f);

	public Transform biteSpot;

	public bool inBiteZone;

	public AudioSource eatSoundSource;

	private EdibleHoldableStates previousEdibleState;

	private IResettableItem[] iResettableItems;

	[field: NonSerialized]
	public int lastBiterActorID { get; private set; } = -1;

	protected override void Start()
	{
		base.Start();
		itemState = ItemStates.State0;
		previousEdibleState = (EdibleHoldableStates)itemState;
		lastFullyEatenTime = 0f - respawnTime;
		iResettableItems = GetComponentsInChildren<IResettableItem>(includeInactive: true);
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		lastEatTime = Time.time - eatMinimumCooldown;
	}

	public override void OnActivate()
	{
		base.OnActivate();
	}

	internal override void OnEnable()
	{
		base.OnEnable();
	}

	internal override void OnDisable()
	{
		base.OnDisable();
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (InHand())
		{
			return false;
		}
		return true;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (itemState == ItemStates.State3)
		{
			if (Time.time > lastFullyEatenTime + respawnTime)
			{
				itemState = ItemStates.State0;
			}
		}
		else
		{
			if (!(Time.time > lastEatTime + eatMinimumCooldown))
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			float num = biteDistance * biteDistance;
			if (!GorillaParent.hasInstance)
			{
				return;
			}
			VRRig vRRig = null;
			VRRig vRRig2 = null;
			for (int i = 0; i < VRRigCache.ActiveRigContainers.Count; i++)
			{
				VRRig rig = VRRigCache.ActiveRigContainers[i].Rig;
				if (!rig.isOfflineVRRig)
				{
					if (rig.head == null || rig.head.rigTarget.IsNull())
					{
						break;
					}
					Transform transform = rig.head.rigTarget.transform;
					if ((transform.position + transform.rotation * biteOffset - biteSpot.position).sqrMagnitude < num)
					{
						flag = true;
						vRRig2 = rig;
					}
				}
			}
			Transform transform2 = GorillaTagger.Instance.offlineVRRig.head.rigTarget.transform;
			if ((transform2.position + transform2.rotation * biteOffset - biteSpot.position).sqrMagnitude < num)
			{
				flag = true;
				flag2 = true;
				vRRig = GorillaTagger.Instance.offlineVRRig;
			}
			if (flag && !inBiteZone && (!flag2 || InHand()) && itemState != ItemStates.State3)
			{
				if (itemState == ItemStates.State0)
				{
					itemState = ItemStates.State1;
				}
				else if (itemState == ItemStates.State1)
				{
					itemState = ItemStates.State2;
				}
				else if (itemState == ItemStates.State2)
				{
					itemState = ItemStates.State3;
				}
				lastEatTime = Time.time;
				lastFullyEatenTime = Time.time;
			}
			if (flag)
			{
				if (flag2)
				{
					lastBiterActorID = ((!vRRig) ? (-1) : (vRRig.OwningNetPlayer?.ActorNumber ?? (-1)));
					onBiteView?.Invoke(vRRig, (int)itemState);
				}
				else
				{
					lastBiterActorID = ((!vRRig2) ? (-1) : (vRRig2.OwningNetPlayer?.ActorNumber ?? (-1)));
					onBiteWorld?.Invoke(vRRig2, (int)itemState);
				}
			}
			inBiteZone = flag;
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		EdibleHoldableStates edibleHoldableStates = (EdibleHoldableStates)itemState;
		if (edibleHoldableStates != previousEdibleState)
		{
			OnEdibleHoldableStateChange();
		}
		previousEdibleState = edibleHoldableStates;
	}

	protected virtual void OnEdibleHoldableStateChange()
	{
		float amplitude = GorillaTagger.Instance.tapHapticStrength / 4f;
		float fixedDeltaTime = Time.fixedDeltaTime;
		float volumeScale = 0.08f;
		int num = 0;
		if (itemState == ItemStates.State0)
		{
			num = 0;
			if (iResettableItems != null)
			{
				IResettableItem[] array = iResettableItems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i]?.ResetToDefaultState();
				}
			}
		}
		else if (itemState == ItemStates.State1)
		{
			num = 1;
		}
		else if (itemState == ItemStates.State2)
		{
			num = 2;
		}
		else if (itemState == ItemStates.State3)
		{
			num = 3;
		}
		int num2 = num - 1;
		if (num2 < 0)
		{
			num2 = edibleMeshObjects.Length - 1;
		}
		edibleMeshObjects[num2].SetActive(value: false);
		edibleMeshObjects[num].SetActive(value: true);
		if ((itemState != ItemStates.State0 && onBiteView != null) || onBiteWorld != null)
		{
			VRRig vRRig = null;
			float num3 = float.PositiveInfinity;
			for (int j = 0; j < VRRigCache.ActiveRigContainers.Count; j++)
			{
				VRRig rig = VRRigCache.ActiveRigContainers[j].Rig;
				if (rig.head == null || rig.head.rigTarget.IsNull())
				{
					break;
				}
				Transform transform = rig.head.rigTarget.transform;
				float sqrMagnitude = (transform.position + transform.rotation * biteOffset - biteSpot.position).sqrMagnitude;
				if (sqrMagnitude < num3)
				{
					num3 = sqrMagnitude;
					vRRig = rig;
				}
			}
			if (vRRig.IsNotNull())
			{
				(vRRig.isOfflineVRRig ? onBiteView : onBiteWorld)?.Invoke(vRRig, (int)itemState);
				if (vRRig.isOfflineVRRig && itemState != ItemStates.State0)
				{
					PlayerGameEvents.EatObject(interactEventName);
				}
			}
		}
		eatSoundSource.GTPlayOneShot(eatSounds[num], volumeScale);
		if (IsMyItem())
		{
			if (InHand())
			{
				GorillaTagger.Instance.StartVibration(InLeftHand(), amplitude, fixedDeltaTime);
				return;
			}
			GorillaTagger.Instance.StartVibration(forLeftController: false, amplitude, fixedDeltaTime);
			GorillaTagger.Instance.StartVibration(forLeftController: true, amplitude, fixedDeltaTime);
		}
	}

	public override bool CanActivate()
	{
		return true;
	}

	public override bool CanDeactivate()
	{
		return true;
	}
}
