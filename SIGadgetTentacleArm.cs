using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class SIGadgetTentacleArm : SIGadget, ICallBack
{
	[SerializeField]
	private GameObject claw;

	[SerializeField]
	private LayerMask worldCollisionLayers;

	[SerializeField]
	private Transform marker;

	[SerializeField]
	private float maxTentacleLength;

	[SerializeField]
	private MeshRenderer tentacleRenderer;

	[SerializeField]
	private Transform tentacleAnchor;

	private Material tentacleMat;

	private ShaderHashId tentacleEnd = "_TentacleEndPos";

	private ShaderHashId tentacleEndDir = "_TentacleEndDir";

	private bool isLeftHanded;

	private Vector3 knownSafePosition;

	private Vector3 clawHoldAdjustment;

	private Vector3 clawAnchorPosition;

	private Vector3 lastRequestedPlayerPosition;

	private Quaternion clawRotationOnGrab;

	private bool isGripBroken;

	private bool hasRigCallback;

	private VRRig rigForCallback;

	private Vector3 clawVisualPos;

	private Quaternion clawVisualRot;

	private long anchoredBit = 4611686018427387904L;

	public bool isAnchored { get; private set; }

	private void Awake()
	{
		tentacleMat = new Material(tentacleRenderer.sharedMaterial);
		tentacleRenderer.sharedMaterial = tentacleMat;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void Start()
	{
		clawVisualPos = claw.transform.position;
		clawVisualRot = claw.transform.rotation;
		CallBack();
	}

	private void OnDestroy()
	{
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
	}

	private void OnGrabbed()
	{
		isLeftHanded = gameEntity.heldByHandIndex == 0;
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			hasRigCallback = true;
			rigForCallback = out_gamePlayer.rig;
			rigForCallback.AddLateUpdateCallback(this);
		}
	}

	private void OnSnapped()
	{
		isLeftHanded = gameEntity.snappedJoint == SnapJointType.HandL;
		if (GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer))
		{
			hasRigCallback = true;
			rigForCallback = out_gamePlayer.rig;
			rigForCallback.AddLateUpdateCallback(this);
		}
	}

	private void OnReleased()
	{
		ClearClawAnchor();
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
	}

	private void OnUnsnapped()
	{
		if (hasRigCallback)
		{
			hasRigCallback = false;
			rigForCallback.RemoveLateUpdateCallback(this);
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		Vector3 position = GTPlayer.Instance.bodyCollider.transform.position;
		Vector3 position2 = base.transform.position;
		Vector3 vector = position2 - position;
		GTPlayer.HandState obj = (isLeftHanded ? GTPlayer.Instance.LeftHand : GTPlayer.Instance.RightHand);
		Transform controllerTransform = obj.controllerTransform;
		float num = (isLeftHanded ? ControllerInputPoller.instance.leftControllerIndexFloat : ControllerInputPoller.instance.rightControllerIndexFloat);
		bool flag = num >= 0.9f;
		if (isGripBroken)
		{
			if (flag)
			{
				num = 0f;
				flag = false;
			}
			else
			{
				isGripBroken = false;
			}
		}
		Vector3 vector2 = position2 + vector;
		Quaternion quaternion = controllerTransform.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
		if ((knownSafePosition - vector2).IsLongerThan(1f))
		{
			knownSafePosition = position2;
		}
		float num2 = 0.15f;
		clawVisualRot = base.transform.rotation;
		RaycastHit hitInfo;
		bool flag2 = Physics.SphereCast(new Ray(knownSafePosition, vector2 - knownSafePosition), num2, out hitInfo, (vector2 - knownSafePosition).magnitude, worldCollisionLayers);
		if (isAnchored)
		{
			if (flag)
			{
				Vector3 position3 = GTPlayer.Instance.transform.position;
				clawHoldAdjustment -= position3 - lastRequestedPlayerPosition;
				Vector3 vector3 = clawAnchorPosition - (vector2 + clawHoldAdjustment);
				GTPlayer.Instance.RequestTentacleMove(isLeftHanded, vector3);
				lastRequestedPlayerPosition = position3 + vector3;
				if ((clawAnchorPosition - base.transform.position).IsLongerThan(maxTentacleLength))
				{
					isGripBroken = true;
					ClearClawAnchor();
				}
				else
				{
					clawVisualPos = clawAnchorPosition;
					clawVisualRot = clawRotationOnGrab;
				}
				return;
			}
			ClearClawAnchor();
		}
		Vector3 vector4 = vector2;
		Quaternion clawRotation = quaternion;
		if (flag2)
		{
			knownSafePosition += (vector2 - knownSafePosition).normalized * (hitInfo.distance - num2 * 2.01f);
			marker.transform.position = hitInfo.point;
			marker.transform.rotation = Quaternion.LookRotation(-hitInfo.normal, quaternion * Vector3.up);
			vector4 = hitInfo.point + hitInfo.normal * Mathf.Lerp(0.1f, 0.01f, num);
			clawRotation = Quaternion.Lerp(quaternion, Quaternion.LookRotation(-hitInfo.normal, quaternion * Vector3.up), num * 0.5f + 0.5f);
		}
		else
		{
			knownSafePosition = vector2;
		}
		clawVisualPos = vector4;
		clawVisualRot = clawRotation;
		if (!isAnchored && flag && flag2)
		{
			SetClawAnchor(vector4, clawRotation, vector4 - vector2);
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		if (isAnchored)
		{
			return;
		}
		int attachedPlayerActorNumber = GetAttachedPlayerActorNumber();
		if (attachedPlayerActorNumber >= 1 && GamePlayer.TryGetGamePlayer(attachedPlayerActorNumber, out var out_gamePlayer))
		{
			Vector3 position = out_gamePlayer.rig.bodyTransform.position;
			Vector3 position2 = base.transform.position;
			Vector3 vector = position2 - position;
			Vector3 vector2 = position2 + vector;
			Quaternion quaternion = base.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
			if ((knownSafePosition - vector2).IsLongerThan(1f))
			{
				knownSafePosition = position2;
			}
			float num = 0.15f;
			RaycastHit hitInfo;
			bool num2 = Physics.SphereCast(new Ray(knownSafePosition, vector2 - knownSafePosition), num, out hitInfo, (vector2 - knownSafePosition).magnitude, worldCollisionLayers);
			Vector3 vector3 = vector2;
			Quaternion quaternion2 = quaternion;
			if (num2)
			{
				knownSafePosition += (vector2 - knownSafePosition).normalized * (hitInfo.distance - num * 2.01f);
				vector3 = hitInfo.point + hitInfo.normal * 0.1f;
			}
			else
			{
				knownSafePosition = vector2;
			}
			clawVisualPos = vector3;
			clawVisualRot = quaternion2;
		}
	}

	private long GetStateLong()
	{
		if (isAnchored)
		{
			return anchoredBit | BitPackUtils.PackAnchoredPosRotForNetwork(clawVisualPos, clawVisualRot);
		}
		return 0L;
	}

	private void SetClawAnchor(Vector3 clawPosition, Quaternion clawRotation, Vector3 adjustment)
	{
		isAnchored = true;
		clawHoldAdjustment = adjustment;
		clawAnchorPosition = clawPosition;
		clawRotationOnGrab = clawRotation;
		if (IsEquippedLocal())
		{
			lastRequestedPlayerPosition = GTPlayer.Instance.transform.position;
			GTPlayer.Instance.SetGravityOverride(this, GravityOverrideFunction);
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
	}

	private void ClearClawAnchor()
	{
		isAnchored = false;
		if (IsEquippedLocal())
		{
			GTPlayer.Instance.SetVelocity(GTPlayer.Instance.AveragedVelocity);
			GTPlayer.Instance.UnsetGravityOverride(this);
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
	}

	private void GravityOverrideFunction(GTPlayer player)
	{
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			return;
		}
		if (newState != 0L)
		{
			int attachedPlayerActorNumber = GetAttachedPlayerActorNumber();
			if (attachedPlayerActorNumber >= 1 && GamePlayer.TryGetGamePlayer(attachedPlayerActorNumber, out var out_gamePlayer))
			{
				BitPackUtils.UnpackAnchoredPosRotForNetwork(newState, out_gamePlayer.rig.transform.position, out var pos, out var rot);
				SetClawAnchor(pos, rot, Vector3.zero);
				clawVisualPos = clawAnchorPosition;
				clawVisualRot = clawRotationOnGrab;
			}
		}
		else
		{
			ClearClawAnchor();
		}
	}

	public void CallBack()
	{
		claw.transform.position = clawVisualPos;
		claw.transform.rotation = clawVisualRot;
		Vector3 vector = tentacleRenderer.transform.InverseTransformPoint(tentacleAnchor.position);
		tentacleMat.SetVector(tentacleEnd, vector);
		Vector3 vector2 = -tentacleRenderer.transform.InverseTransformDirection(tentacleAnchor.forward);
		tentacleMat.SetVector(tentacleEndDir, vector2);
	}
}
