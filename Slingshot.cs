using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Slingshot : ProjectileWeapon
{
	public enum SlingshotState
	{
		NoState = 1,
		OnChest = 2,
		LeftHandDrawing = 4,
		RightHandDrawing = 8
	}

	public enum SlingshotActions
	{
		Grab,
		Release
	}

	[SerializeField]
	private bool disableLineRenderer;

	[FormerlySerializedAs("elastic")]
	public LineRenderer elasticLeft;

	public LineRenderer elasticRight;

	public Transform leftArm;

	public Transform rightArm;

	public Transform center;

	public Transform centerOrigin;

	private GameObject dummyProjectile;

	public GameObject drawingHand;

	public InteractionPoint nock;

	public InteractionPoint grip;

	public float springConstant;

	public float maxDraw;

	[SerializeField]
	private GameObject disableInDraw;

	[SerializeField]
	private float minDrawDistanceToRelease;

	[Header("Stretching Haptics")]
	[Space]
	[SerializeField]
	private bool playStretchingHaptics;

	[SerializeField]
	private float hapticsStrength = 0.1f;

	[SerializeField]
	private float hapticsLength = 0.1f;

	[Header("Stretching Events")]
	[Space]
	public UnityEvent<bool> StretchStartShared;

	public UnityEvent<bool> StretchEndShared;

	[Space]
	public UnityEvent<bool> StretchStartLocal;

	public UnityEvent<bool> StretchEndLocal;

	private bool wasStretching;

	private bool wasStretchingLocal;

	private Transform leftHandSnap;

	private Transform rightHandSnap;

	public bool disableWhenNotInRoom;

	private bool hasDummyProjectile;

	private float delayLaunchTime = 0.07f;

	private float minTimeToLaunch = -1f;

	private float dummyProjectileColliderRadius;

	private float dummyProjectileInitialScale;

	private int projectileCount;

	private Vector3[] elasticLeftPoints = new Vector3[2];

	private Vector3[] elasticRightPoints = new Vector3[2];

	private float _elasticIntialWidthMultiplier;

	private new VRRig myRig;

	private void DestroyDummyProjectile()
	{
		if (hasDummyProjectile)
		{
			dummyProjectile.transform.localScale = Vector3.one * dummyProjectileInitialScale;
			dummyProjectile.GetComponent<SphereCollider>().enabled = true;
			ObjectPools.instance.Destroy(dummyProjectile);
			dummyProjectile = null;
			hasDummyProjectile = false;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if ((bool)elasticLeft)
		{
			_elasticIntialWidthMultiplier = elasticLeft.widthMultiplier;
		}
	}

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		myRig = rig;
		OnEnable();
	}

	internal override void OnEnable()
	{
		if (base.IsSpawned)
		{
			leftHandSnap = myRig.cosmeticReferences.Get(CosmeticRefID.SlingshotSnapLeft).transform;
			rightHandSnap = myRig.cosmeticReferences.Get(CosmeticRefID.SlingshotSnapRight).transform;
			currentState = PositionState.OnChest;
			itemState = ItemStates.State0;
			if ((bool)elasticLeft)
			{
				elasticLeft.positionCount = 2;
			}
			if ((bool)elasticRight)
			{
				elasticRight.positionCount = 2;
			}
			dummyProjectile = null;
			base.OnEnable();
		}
	}

	internal override void OnDisable()
	{
		DestroyDummyProjectile();
		base.OnDisable();
	}

	protected override void LateUpdateShared()
	{
		if (!base.IsSpawned)
		{
			return;
		}
		base.LateUpdateShared();
		float num = Mathf.Abs(base.transform.lossyScale.x);
		Vector3 vector;
		if (InDrawingState())
		{
			if (!hasDummyProjectile)
			{
				dummyProjectile = ObjectPools.instance.Instantiate(projectilePrefab);
				hasDummyProjectile = true;
				SphereCollider component = dummyProjectile.GetComponent<SphereCollider>();
				component.enabled = false;
				dummyProjectileColliderRadius = component.radius;
				dummyProjectileInitialScale = dummyProjectile.transform.localScale.x;
				GetIsOnTeams(out var blueTeam, out var orangeTeam, out var shouldUsePlayerColor);
				dummyProjectile.GetComponent<SlingshotProjectile>().ApplyTeamModelAndColor(blueTeam, orangeTeam, shouldUsePlayerColor && (bool)targetRig, targetRig ? targetRig.playerColor : default(Color));
			}
			if (disableInDraw != null)
			{
				disableInDraw.SetActive(value: false);
			}
			if (disableInDraw != null)
			{
				disableInDraw.SetActive(value: false);
			}
			float num2 = dummyProjectileInitialScale * num;
			dummyProjectile.transform.localScale = Vector3.one * num2;
			Vector3 position = drawingHand.transform.position;
			Vector3 position2 = centerOrigin.position;
			Vector3 normalized = (position2 - position).normalized;
			float num3 = (EquipmentInteractor.instance.grabRadius - dummyProjectileColliderRadius) * num;
			vector = position + normalized * num3;
			dummyProjectile.transform.position = vector;
			dummyProjectile.transform.rotation = Quaternion.LookRotation(position2 - vector, Vector3.up);
			if (!wasStretching)
			{
				StretchStartShared?.Invoke(!ForLeftHandSlingshot());
				wasStretching = true;
			}
		}
		else
		{
			DestroyDummyProjectile();
			if (disableInDraw != null)
			{
				disableInDraw.SetActive(value: true);
			}
			vector = centerOrigin.position;
			if (wasStretching)
			{
				StretchEndShared?.Invoke(!ForLeftHandSlingshot());
				wasStretching = false;
			}
		}
		center.position = vector;
		if (!disableLineRenderer)
		{
			elasticLeftPoints[0] = leftArm.position;
			elasticLeftPoints[1] = (elasticRightPoints[1] = vector);
			elasticRightPoints[0] = rightArm.position;
			elasticLeft.SetPositions(elasticLeftPoints);
			elasticRight.SetPositions(elasticRightPoints);
			elasticLeft.widthMultiplier = _elasticIntialWidthMultiplier * num;
			elasticRight.widthMultiplier = _elasticIntialWidthMultiplier * num;
		}
		if (!NetworkSystem.Instance.InRoom && disableWhenNotInRoom)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (InDrawingState())
		{
			if (ForLeftHandSlingshot())
			{
				drawingHand = EquipmentInteractor.instance.rightHand;
			}
			else
			{
				drawingHand = EquipmentInteractor.instance.leftHand;
			}
			GorillaTagger.Instance.StartVibration(!ForLeftHandSlingshot(), hapticsStrength, hapticsLength);
			if (!wasStretchingLocal)
			{
				StretchStartLocal?.Invoke(!ForLeftHandSlingshot());
				wasStretchingLocal = true;
			}
		}
		else if (wasStretchingLocal)
		{
			StretchEndLocal?.Invoke(!ForLeftHandSlingshot());
			wasStretchingLocal = false;
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (InDrawingState())
		{
			if (ForLeftHandSlingshot())
			{
				drawingHand = rightHandSnap.gameObject;
			}
			else
			{
				drawingHand = leftHandSnap.gameObject;
			}
		}
	}

	public static bool IsSlingShotEnabled()
	{
		if (GorillaTagger.Instance == null || GorillaTagger.Instance.offlineVRRig == null)
		{
			return false;
		}
		return GorillaTagger.Instance.offlineVRRig.cosmeticSet.HasItemOfCategory(CosmeticsController.CosmeticCategory.Chest);
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!IsMyItem())
		{
			return;
		}
		bool flag = pointGrabbed == nock;
		if (flag && !InHand())
		{
			return;
		}
		base.OnGrab(pointGrabbed, grabbingHand);
		if (!InDrawingState() && !OnChest() && flag)
		{
			if (grabbingHand == EquipmentInteractor.instance.leftHand)
			{
				EquipmentInteractor.instance.disableLeftGrab = true;
			}
			else
			{
				EquipmentInteractor.instance.disableRightGrab = true;
			}
			if (ForLeftHandSlingshot())
			{
				itemState = ItemStates.State2;
			}
			else
			{
				itemState = ItemStates.State3;
			}
			minTimeToLaunch = Time.time + delayLaunchTime;
			GorillaTagger.Instance.StartVibration(!ForLeftHandSlingshot(), GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration * 1.5f);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		base.OnRelease(zoneReleased, releasingHand);
		if (InDrawingState() && releasingHand == drawingHand)
		{
			if (releasingHand == EquipmentInteractor.instance.leftHand)
			{
				EquipmentInteractor.instance.disableLeftGrab = false;
			}
			else
			{
				EquipmentInteractor.instance.disableRightGrab = false;
			}
			if (ForLeftHandSlingshot())
			{
				currentState = PositionState.InLeftHand;
			}
			else
			{
				currentState = PositionState.InRightHand;
			}
			itemState = ItemStates.State0;
			GorillaTagger.Instance.StartVibration(ForLeftHandSlingshot(), GorillaTagger.Instance.tapHapticStrength * 2f, GorillaTagger.Instance.tapHapticDuration * 1.5f);
			if (Time.time > minTimeToLaunch && (releasingHand.transform.position - centerOrigin.transform.position).sqrMagnitude > minDrawDistanceToRelease * minDrawDistanceToRelease)
			{
				LaunchProjectile();
			}
		}
		else
		{
			EquipmentInteractor.instance.disableLeftGrab = false;
			EquipmentInteractor.instance.disableRightGrab = false;
		}
		return true;
	}

	public override void DropItemCleanup()
	{
		base.DropItemCleanup();
		currentState = PositionState.OnChest;
		itemState = ItemStates.State0;
	}

	public override bool AutoGrabTrue(bool leftGrabbingHand)
	{
		return true;
	}

	private bool ForLeftHandSlingshot()
	{
		if (itemState != ItemStates.State2)
		{
			return currentState == PositionState.InLeftHand;
		}
		return true;
	}

	private bool InDrawingState()
	{
		if (itemState != ItemStates.State2)
		{
			return itemState == ItemStates.State3;
		}
		return true;
	}

	protected override Vector3 GetLaunchPosition()
	{
		return dummyProjectile.transform.position;
	}

	protected override Vector3 GetLaunchVelocity()
	{
		float num = Mathf.Abs(base.transform.lossyScale.x);
		Vector3 vector = centerOrigin.position - center.position;
		vector /= num;
		Vector3 vector2 = Mathf.Min(springConstant * maxDraw, vector.magnitude * springConstant) * vector.normalized * num;
		Vector3 averagedVelocity = GTPlayer.Instance.AveragedVelocity;
		return vector2 + averagedVelocity;
	}
}
