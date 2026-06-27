using System.Collections.Generic;
using GorillaLocomotion;
using GorillaTag;
using UnityEngine;

public class SnowballMaker : MonoBehaviourPostTick
{
	public bool isLeftHand;

	public GorillaVelocityEstimator velocityEstimator;

	private float snowballCreationCooldownTime = 0.1f;

	private float lastGroundContactTime;

	private bool requiresFreshMaterialContact;

	private Transform handTransform;

	private Dictionary<int, SnowballThrowable> matSnowballLookup = new Dictionary<int, SnowballThrowable>();

	private Dictionary<int, SnowballThrowable> snowballByThrowableIndex = new Dictionary<int, SnowballThrowable>();

	private Dictionary<int, string> snowballPlayfabIdByThrowableIndex = new Dictionary<int, string>();

	private Dictionary<int, string> snowballPlayfabIdByMaterialIndex = new Dictionary<int, string>();

	public static SnowballMaker leftHandInstance { get; private set; }

	public static SnowballMaker rightHandInstance { get; private set; }

	public SnowballThrowable[] snowballs { get; private set; }

	private void Awake()
	{
		if (snowballs == null)
		{
			snowballs = new SnowballThrowable[0];
		}
		if (isLeftHand)
		{
			if (leftHandInstance == null)
			{
				leftHandInstance = this;
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
		else if (rightHandInstance == null)
		{
			rightHandInstance = this;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		handTransform = (isLeftHand ? GorillaTagger.Instance.offlineVRRig.myBodyDockPositions.leftHandTransform : GorillaTagger.Instance.offlineVRRig.myBodyDockPositions.rightHandTransform);
	}

	internal void SetupThrowables(SnowballThrowable[] newThrowables)
	{
		snowballs = newThrowables;
		for (int i = 0; i < snowballs.Length; i++)
		{
			if (!(snowballs[i] == null))
			{
				for (int j = 0; j < snowballs[i].matDataIndexes.Count; j++)
				{
					matSnowballLookup.TryAdd(snowballs[i].matDataIndexes[j], snowballs[i]);
				}
			}
		}
	}

	public override void PostTick()
	{
		if (ApplicationQuittingState.IsQuitting || (BuilderPieceInteractor.instance != null && BuilderPieceInteractor.instance.BlockSnowballCreation()) || !GTPlayer.hasInstance || !EquipmentInteractor.hasInstance || !GorillaTagger.hasInstance || !GorillaTagger.Instance.offlineVRRig)
		{
			return;
		}
		int materialTouchIndex = GTPlayer.Instance.GetMaterialTouchIndex(isLeftHand);
		if (materialTouchIndex == 0)
		{
			if (Time.time > lastGroundContactTime + snowballCreationCooldownTime)
			{
				requiresFreshMaterialContact = false;
			}
			return;
		}
		lastGroundContactTime = Time.time;
		InitializeSnowballFromMatIndex(materialTouchIndex);
		EquipmentInteractor instance = EquipmentInteractor.instance;
		bool flag = (isLeftHand ? instance.leftHandHeldEquipment : instance.rightHandHeldEquipment) != null;
		bool num = (isLeftHand ? instance.isLeftGrabbing : instance.isRightGrabbing);
		bool flag2 = (isLeftHand ? instance.disableLeftGrab : instance.disableRightGrab);
		bool flag3 = false;
		if (!num || flag2 || requiresFreshMaterialContact)
		{
			return;
		}
		int num2 = -1;
		for (int i = 0; i < snowballs.Length; i++)
		{
			SnowballThrowable snowballThrowable = snowballs[i];
			if (!(snowballThrowable == null) && snowballThrowable.gameObject.activeSelf)
			{
				num2 = i;
				break;
			}
		}
		SnowballThrowable snowballThrowable2 = ((num2 > -1) ? snowballs[num2] : null);
		GrowingSnowballThrowable growingSnowballThrowable = snowballThrowable2 as GrowingSnowballThrowable;
		bool flag4 = (isLeftHand ? (!ConnectedControllerHandler.Instance.RightValid) : (!ConnectedControllerHandler.Instance.LeftValid));
		SnowballThrowable value;
		if (growingSnowballThrowable != null && (!GrowingSnowballThrowable.twoHandedSnowballGrowing || flag4 || flag3))
		{
			if (snowballThrowable2.matDataIndexes.Contains(materialTouchIndex))
			{
				growingSnowballThrowable.IncreaseSize(1);
				GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
				requiresFreshMaterialContact = true;
			}
		}
		else if (!flag && matSnowballLookup.TryGetValue(materialTouchIndex, out value))
		{
			Transform obj = value.transform;
			Transform transform = handTransform;
			XformOffset spawnOffset = value.SpawnOffset;
			value.SetSnowballActiveLocal(enabled: true);
			value.velocityEstimator = velocityEstimator;
			obj.position = transform.TransformPoint(spawnOffset.pos);
			obj.rotation = transform.rotation * spawnOffset.rot;
			GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			requiresFreshMaterialContact = true;
		}
	}

	public bool TryCreateSnowball(int materialIndex, out SnowballThrowable result)
	{
		EquipmentInteractor instance = EquipmentInteractor.instance;
		if (isLeftHand ? instance.disableLeftGrab : instance.disableRightGrab)
		{
			result = null;
			return false;
		}
		InitializeSnowballFromMatIndex(materialIndex);
		SnowballThrowable[] array = snowballs;
		foreach (SnowballThrowable snowballThrowable in array)
		{
			if (!(snowballThrowable == null) && snowballThrowable.matDataIndexes.Contains(materialIndex))
			{
				Transform obj = snowballThrowable.transform;
				Transform transform = handTransform;
				XformOffset spawnOffset = snowballThrowable.SpawnOffset;
				snowballThrowable.SetSnowballActiveLocal(enabled: true);
				snowballThrowable.velocityEstimator = velocityEstimator;
				obj.position = transform.TransformPoint(spawnOffset.pos);
				obj.rotation = transform.rotation * spawnOffset.rot;
				GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
				result = snowballThrowable;
				return true;
			}
		}
		result = null;
		return false;
	}

	private void InitializeSnowballFromMatIndex(int matIndex)
	{
		if (CosmeticsV2Spawner_Dirty.GetThrowableIDFromMaterialIndex(isLeftHand, matIndex, out var throwableId))
		{
			VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(throwableId);
		}
	}
}
