using System;
using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.XR;

namespace GorillaLocomotion.Gameplay;

public class GorillaRopeSwing : MonoBehaviour, IBuilderPieceComponent
{
	public int ropeId;

	public string staticId;

	public bool useStaticId;

	protected float ropeBitGenOffset = 1f;

	[SerializeField]
	protected GameObject prefabRopeBit;

	[SerializeField]
	private bool supportMovingAtRuntime;

	public Transform[] nodes = Array.Empty<Transform>();

	private Dictionary<int, int> remotePlayers = new Dictionary<int, int>();

	[NonSerialized]
	public float lastGrabTime;

	[SerializeField]
	private AudioSource ropeCreakSFX;

	public GorillaVelocityTracker velocityTracker;

	private bool localPlayerOn;

	private int localPlayerBoneIndex;

	private XRNode localPlayerXRNode;

	private const float MAX_VELOCITY_FOR_IDLE = 0.5f;

	private const float TIME_FOR_IDLE = 2f;

	private float potentialIdleTimer;

	[SerializeField]
	protected int ropeLength = 8;

	[SerializeField]
	private GorillaRopeSwingSettings settings;

	private bool hasMonkeBlockParent;

	private BuilderPiece monkeBlockParent;

	[NonSerialized]
	public int ropeDataStartIndex;

	[NonSerialized]
	public int ropeDataIndexOffset;

	[SerializeField]
	private LayerMask wallLayerMask;

	private RaycastHit[] nodeHits = new RaycastHit[1];

	private float scaleFactor = 1f;

	private bool started;

	private int lastNodeCheckIndex = 2;

	public bool isIdle { get; private set; }

	public bool isFullyIdle { get; private set; }

	public bool SupportsMovingAtRuntime => supportMovingAtRuntime;

	public bool hasPlayers
	{
		get
		{
			if (!localPlayerOn)
			{
				return remotePlayers.Count > 0;
			}
			return true;
		}
	}

	private void EdRecalculateId()
	{
		CalculateId(force: true);
	}

	protected virtual void Awake()
	{
		base.transform.rotation = Quaternion.identity;
		scaleFactor = (base.transform.lossyScale.x + base.transform.lossyScale.y + base.transform.lossyScale.z) / 3f;
		SetIsIdle(idle: true);
	}

	protected virtual void Start()
	{
		if (!useStaticId)
		{
			CalculateId();
		}
		RopeSwingManager.Register(this);
		started = true;
	}

	private void OnDestroy()
	{
		if (RopeSwingManager.instance != null)
		{
			RopeSwingManager.Unregister(this);
		}
	}

	protected virtual void OnEnable()
	{
		base.transform.rotation = Quaternion.identity;
		scaleFactor = (base.transform.lossyScale.x + base.transform.lossyScale.y + base.transform.lossyScale.z) / 3f;
		SetIsIdle(idle: true, resetPos: true);
		VectorizedCustomRopeSimulation.Register(this);
		GorillaRopeSwingUpdateManager.RegisterRopeSwing(this);
	}

	private void OnDisable()
	{
		if (!isIdle)
		{
			SetIsIdle(idle: true, resetPos: true);
		}
		VectorizedCustomRopeSimulation.Unregister(this);
		GorillaRopeSwingUpdateManager.UnregisterRopeSwing(this);
	}

	internal void CalculateId(bool force = false)
	{
		Transform transform = base.transform;
		int staticHash = TransformUtils.GetScenePath(transform).GetStaticHash();
		int staticHash2 = GetType().Name.GetStaticHash();
		int num = StaticHash.Compute(staticHash, staticHash2);
		if (useStaticId)
		{
			if (string.IsNullOrEmpty(staticId) || force)
			{
				Vector3 position = transform.position;
				int i = StaticHash.Compute(position.x, position.y, position.z);
				int instanceID = transform.GetInstanceID();
				int num2 = StaticHash.Compute(num, i, instanceID);
				staticId = $"#ID_{num2:X8}";
			}
			ropeId = staticId.GetStaticHash();
		}
		else
		{
			ropeId = (Application.isPlaying ? num : 0);
		}
	}

	public void InvokeUpdate()
	{
		if (isIdle)
		{
			isFullyIdle = true;
		}
		if (!isIdle)
		{
			int num = -1;
			if (localPlayerOn)
			{
				num = localPlayerBoneIndex;
			}
			else if (remotePlayers.Count > 0)
			{
				num = remotePlayers.First().Value;
			}
			if (num >= 0 && VectorizedCustomRopeSimulation.instance.GetNodeVelocity(this, num).magnitude > 2f && !ropeCreakSFX.isPlaying && Mathf.RoundToInt(Time.time) % 5 == 0)
			{
				ropeCreakSFX.GTPlay();
			}
			if (localPlayerOn)
			{
				float num2 = MathUtils.Linear(velocityTracker.GetLatestVelocity(worldSpace: true).magnitude / scaleFactor, 0f, 10f, -0.07f, 0.5f);
				if (num2 > 0f)
				{
					GorillaTagger.Instance.DoVibration(localPlayerXRNode, num2, Time.deltaTime);
				}
			}
			Transform bone = GetBone(lastNodeCheckIndex);
			Vector3 nodeVelocity = VectorizedCustomRopeSimulation.instance.GetNodeVelocity(this, lastNodeCheckIndex);
			if (Physics.SphereCastNonAlloc(bone.position, 0.2f * scaleFactor, nodeVelocity.normalized, nodeHits, 0.4f * scaleFactor, wallLayerMask, QueryTriggerInteraction.Ignore) > 0)
			{
				SetVelocity(lastNodeCheckIndex, Vector3.zero, wholeRope: false, default(PhotonMessageInfoWrapped));
			}
			if (!(nodeVelocity.magnitude > 0.35f))
			{
				potentialIdleTimer += Time.deltaTime;
			}
			else
			{
				potentialIdleTimer = 0f;
			}
			if (potentialIdleTimer >= 2f)
			{
				SetIsIdle(idle: true);
				potentialIdleTimer = 0f;
			}
			lastNodeCheckIndex++;
			if (lastNodeCheckIndex > nodes.Length)
			{
				lastNodeCheckIndex = 2;
			}
		}
		if (hasMonkeBlockParent && supportMovingAtRuntime)
		{
			base.transform.rotation = Quaternion.Euler(0f, base.transform.parent.rotation.eulerAngles.y, 0f);
		}
	}

	private void SetIsIdle(bool idle, bool resetPos = false)
	{
		isIdle = idle;
		ropeCreakSFX.gameObject.SetActive(!idle);
		if (idle)
		{
			ToggleVelocityTracker(enable: false);
			if (resetPos)
			{
				Vector3 zero = Vector3.zero;
				for (int i = 0; i < nodes.Length; i++)
				{
					nodes[i].transform.localRotation = Quaternion.identity;
					nodes[i].transform.localPosition = zero;
					zero += new Vector3(0f, 0f - ropeBitGenOffset, 0f);
				}
			}
		}
		else
		{
			isFullyIdle = false;
		}
	}

	public Transform GetBone(int index)
	{
		if (index >= nodes.Length)
		{
			return nodes.Last();
		}
		return nodes[index];
	}

	public int GetBoneIndex(Transform r)
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i] == r)
			{
				return i;
			}
		}
		return nodes.Length - 1;
	}

	public void AttachLocalPlayer(XRNode xrNode, Transform grabbedBone, Vector3 offset, Vector3 velocity)
	{
		int num = (localPlayerBoneIndex = GetBoneIndex(grabbedBone));
		velocity /= scaleFactor;
		velocity *= settings.inheritVelocityMultiplier;
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = ropeId;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeBoneIndex = num;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsLeft = xrNode == XRNode.LeftHand;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeOffset = offset;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsPhotonView = false;
		}
		RefreshAllBonesMass();
		List<Vector3> list = new List<Vector3>();
		if (remotePlayers.Count <= 0)
		{
			Transform[] array = nodes;
			foreach (Transform transform in array)
			{
				list.Add(transform.position);
			}
		}
		velocity.y = 0f;
		if (Time.time - lastGrabTime > 1f && (remotePlayers.Count == 0 || velocity.magnitude > 2.5f))
		{
			RopeSwingManager.instance.SendSetVelocity_RPC(ropeId, num, velocity, wholeRope: true);
		}
		lastGrabTime = Time.time;
		ropeCreakSFX.transform.parent = GetBone(Math.Max(0, num - 3)).transform;
		ropeCreakSFX.transform.localPosition = Vector3.zero;
		localPlayerOn = true;
		localPlayerXRNode = xrNode;
		ToggleVelocityTracker(enable: true, num, offset);
	}

	public void DetachLocalPlayer()
	{
		if (GorillaTagger.hasInstance && (bool)GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = -1;
		}
		localPlayerOn = false;
		localPlayerBoneIndex = 0;
		RefreshAllBonesMass();
	}

	private void ToggleVelocityTracker(bool enable, int boneIndex = 0, Vector3 offset = default(Vector3))
	{
		if (enable)
		{
			velocityTracker.transform.SetParent(GetBone(boneIndex));
			velocityTracker.transform.localPosition = offset;
			velocityTracker.ResetState();
		}
		velocityTracker.gameObject.SetActive(enable);
		if (enable)
		{
			velocityTracker.Tick();
		}
	}

	private void RefreshAllBonesMass()
	{
		int num = 0;
		foreach (KeyValuePair<int, int> remotePlayer in remotePlayers)
		{
			if (remotePlayer.Value > num)
			{
				num = remotePlayer.Value;
			}
		}
		if (localPlayerBoneIndex > num)
		{
			num = localPlayerBoneIndex;
		}
		VectorizedCustomRopeSimulation.instance.SetMassForPlayers(this, hasPlayers, num);
	}

	public bool AttachRemotePlayer(int playerId, int boneIndex, Transform offsetTransform, Vector3 offset)
	{
		Transform bone = GetBone(boneIndex);
		if (bone == null)
		{
			return false;
		}
		offsetTransform.SetParent(bone.transform);
		offsetTransform.localPosition = offset;
		offsetTransform.localRotation = Quaternion.identity;
		if (remotePlayers.ContainsKey(playerId))
		{
			Debug.LogError("already on the list!");
			return false;
		}
		remotePlayers.Add(playerId, boneIndex);
		RefreshAllBonesMass();
		return true;
	}

	public void DetachRemotePlayer(int playerId)
	{
		remotePlayers.Remove(playerId);
		RefreshAllBonesMass();
	}

	public void SetVelocity(int boneIndex, Vector3 velocity, bool wholeRope, PhotonMessageInfoWrapped info)
	{
		if (!base.isActiveAndEnabled || !velocity.IsValid(10000f))
		{
			return;
		}
		velocity.x = Mathf.Clamp(velocity.x, -100f, 100f);
		velocity.y = Mathf.Clamp(velocity.y, -100f, 100f);
		velocity.z = Mathf.Clamp(velocity.z, -100f, 100f);
		boneIndex = Mathf.Clamp(boneIndex, 0, nodes.Length);
		Transform bone = GetBone(boneIndex);
		if (!bone)
		{
			return;
		}
		if (info.Sender != null && !info.Sender.IsLocal)
		{
			VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(info.Sender);
			if (!vRRig || Vector3.Distance(bone.position, vRRig.transform.position) > 5f)
			{
				return;
			}
		}
		SetIsIdle(idle: false);
		if ((bool)bone)
		{
			VectorizedCustomRopeSimulation.instance.SetVelocity(this, velocity, wholeRope, boneIndex);
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		monkeBlockParent = GetComponentInParent<BuilderPiece>();
		hasMonkeBlockParent = monkeBlockParent != null;
		int num = StaticHash.Compute(pieceType, pieceId);
		staticId = $"#ID_{num:X8}";
		ropeId = staticId.GetStaticHash();
		if (started && !RopeSwingManager.instance.TryGetRope(ropeId, out var _))
		{
			RopeSwingManager.Register(this);
		}
	}

	public void OnPieceDestroy()
	{
		RopeSwingManager.Unregister(this);
	}

	public void OnPiecePlacementDeserialized()
	{
		VectorizedCustomRopeSimulation.Unregister(this);
		base.transform.rotation = Quaternion.identity;
		scaleFactor = (base.transform.lossyScale.x + base.transform.lossyScale.y + base.transform.lossyScale.z) / 3f;
		SetIsIdle(idle: true, resetPos: true);
		VectorizedCustomRopeSimulation.Register(this);
		if (monkeBlockParent != null)
		{
			supportMovingAtRuntime = IsAttachedToMovingPiece();
		}
	}

	public void OnPieceActivate()
	{
		if (monkeBlockParent != null)
		{
			supportMovingAtRuntime = IsAttachedToMovingPiece();
		}
	}

	private bool IsAttachedToMovingPiece()
	{
		if (monkeBlockParent.attachIndex >= 0 && monkeBlockParent.attachIndex < monkeBlockParent.gridPlanes.Count)
		{
			return monkeBlockParent.gridPlanes[monkeBlockParent.attachIndex].GetMovingParentGrid() != null;
		}
		return false;
	}

	public void OnPieceDeactivate()
	{
		supportMovingAtRuntime = false;
	}
}
