using System;
using System.Collections.Generic;
using GorillaLocomotion;
using JetBrains.Annotations;
using Pathfinding;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(NetworkView))]
public class MonkeyeAI : MonoBehaviour, IGorillaSliceableSimple
{
	public List<Transform> patrolPts;

	public Transform sleepPt;

	private int patrolIdx = -1;

	private int patrolCount;

	private Vector3 targetPosition;

	private MaterialPropertyBlock portalMatPropBlock;

	private MaterialPropertyBlock monkEyeMatPropBlock;

	private Renderer renderer;

	private AIDestinationSetter aiDest;

	private AIPath aiPath;

	private AILerp aiLerp;

	private Seeker seeker;

	private Path path;

	private int currentWaypoint;

	private bool calculatingPath;

	private Monkeye_LazerFX lazerFx;

	private Animator animController;

	private RaycastHit[] rayResults = new RaycastHit[1];

	private LayerMask layerMask;

	private bool wasConnectedToRoom;

	public SkinnedMeshRenderer skinnedMeshRenderer;

	public MazePlayerCollection playerCollection;

	public PlayerCollection playersInRoomCollection;

	private List<VRRig> validRigs = new List<VRRig>();

	public GameObject portalFx;

	public Transform[] eyeBones;

	public float speed = 0.1f;

	public float rotationSpeed = 1f;

	public float wakeDistance = 1f;

	public float chaseDistance = 3f;

	public float sleepDuration = 3f;

	public float attackDistance = 0.1f;

	public float beginAttackTime = 1f;

	public float openFloorTime = 3f;

	public float dropPlayerTime = 1f;

	public float closeFloorTime = 1f;

	public Color portalColor;

	public Color gorillaPortalColor;

	public Color monkEyeColor;

	public Color monkEyeEyeColorNormal;

	public Color monkEyeEyeColorAttacking;

	public int maxPatrols = 4;

	private VRRig targetRig;

	private float deltaTime;

	private float lastTime;

	public MonkeyeAI_ReplState replState;

	private MonkeyeAI_ReplState.EStates previousState;

	private RequestableOwnershipGuard replStateRequestableOwnershipGaurd;

	private RequestableOwnershipGuard myRequestableOwnershipGaurd;

	private int layerBase;

	private int layerForward = 1;

	private int layerLeft = 2;

	private int layerRight = 3;

	private static readonly int EmissionColorShaderProp = ShaderProps._EmissionColor;

	private static readonly int ColorShaderProp = ShaderProps._BaseColor;

	private static readonly int EyeColorShaderProp = ShaderProps._GChannelColor;

	private static readonly int tintColorShaderProp = ShaderProps._TintColor;

	private static readonly int animStateID = Animator.StringToHash("state");

	private Vector3 prevPosition;

	private Vector3 velocity;

	public AudioSource audioSource;

	public AudioClip sleepLoopSound;

	public float sleepLoopVolume = 0.5f;

	[FormerlySerializedAs("moveLoopSound")]
	public AudioClip patrolLoopSound;

	public float patrolLoopVolume = 0.5f;

	public float patrolLoopFadeInTime = 1f;

	public AudioClip chaseLoopSound;

	public float chaseLoopVolume = 0.5f;

	public float chaseLoopFadeInTime = 0.05f;

	public AudioClip attackSound;

	public float attackVolume = 0.5f;

	public float overlapRadius;

	private bool lockedOn;

	private string UserIdFromRig(VRRig rig)
	{
		if (rig == null)
		{
			return "";
		}
		if (!NetworkSystem.Instance.InRoom)
		{
			if (rig == GorillaTagger.Instance.offlineVRRig)
			{
				return "-1";
			}
			Debug.Log("Not in a room but not targeting offline rig");
			return null;
		}
		if (rig == GorillaTagger.Instance.offlineVRRig)
		{
			return NetworkSystem.Instance.LocalPlayer.UserId;
		}
		if (rig.creator == null)
		{
			return "";
		}
		return rig.creator.UserId;
	}

	private VRRig GetRig(string userId)
	{
		if (userId == "")
		{
			return null;
		}
		if (!NetworkSystem.Instance.InRoom && userId != "-1")
		{
			if (userId == "-1 " && GorillaTagger.Instance != null)
			{
				return GorillaTagger.Instance.offlineVRRig;
			}
			return null;
		}
		foreach (VRRig validChoosableRig in GetValidChoosableRigs())
		{
			if (!(validChoosableRig == null))
			{
				NetPlayer creator = validChoosableRig.creator;
				if (creator != null && userId == creator.UserId)
				{
					return validChoosableRig;
				}
			}
		}
		return null;
	}

	private float Distance2D(Vector3 a, Vector3 b)
	{
		Vector2 a2 = new Vector2(a.x, a.z);
		Vector2 b2 = new Vector2(b.x, b.z);
		return Vector2.Distance(a2, b2);
	}

	private Transform PickRandomPatrolPoint()
	{
		int num = 0;
		do
		{
			num = UnityEngine.Random.Range(0, patrolPts.Count);
		}
		while (num == patrolIdx);
		patrolIdx = num;
		return patrolPts[num];
	}

	private void PickNewPath(bool pathFinished = false)
	{
		if (calculatingPath)
		{
			return;
		}
		currentWaypoint = 0;
		switch (replState.state)
		{
		case MonkeyeAI_ReplState.EStates.Patrolling:
			if (patrolCount == maxPatrols)
			{
				SetState(MonkeyeAI_ReplState.EStates.Patrolling);
				targetPosition = PickRandomPatrolPoint().position;
				patrolCount = 0;
			}
			else
			{
				targetPosition = PickRandomPatrolPoint().position;
				patrolCount++;
			}
			break;
		case MonkeyeAI_ReplState.EStates.Chasing:
		{
			if (!lockedOn && ClosestPlayer(base.transform.position, out var outRig) && outRig != targetRig)
			{
				SetTargetPlayer(outRig);
			}
			if (targetRig == null)
			{
				SetState(MonkeyeAI_ReplState.EStates.Patrolling);
				targetPosition = sleepPt.position;
			}
			else
			{
				targetPosition = targetRig.transform.position;
			}
			break;
		}
		case MonkeyeAI_ReplState.EStates.ReturnToSleepPt:
			targetPosition = sleepPt.position;
			break;
		}
		calculatingPath = true;
		seeker.StartPath(base.transform.position, targetPosition, OnPathComplete);
	}

	private void Awake()
	{
		lazerFx = GetComponent<Monkeye_LazerFX>();
		animController = GetComponent<Animator>();
		layerBase = animController.GetLayerIndex("Base_Layer");
		layerForward = animController.GetLayerIndex("MoveFwdAddPose");
		layerLeft = animController.GetLayerIndex("TurnLAddPose");
		layerRight = animController.GetLayerIndex("TurnRAddPose");
		seeker = GetComponent<Seeker>();
		renderer = portalFx.GetComponent<Renderer>();
		portalMatPropBlock = new MaterialPropertyBlock();
		monkEyeMatPropBlock = new MaterialPropertyBlock();
		layerMask = UnityLayer.Default.ToLayerMask() | UnityLayer.GorillaObject.ToLayerMask();
		SetDefaultAttackState();
		SetState(MonkeyeAI_ReplState.EStates.Sleeping);
		replStateRequestableOwnershipGaurd = replState.GetComponent<RequestableOwnershipGuard>();
		myRequestableOwnershipGaurd = GetComponent<RequestableOwnershipGuard>();
		if (monkEyeColor.a != 0f || monkEyeEyeColorNormal.a != 0f)
		{
			if (monkEyeColor.a != 0f)
			{
				monkEyeMatPropBlock.SetVector(ColorShaderProp, monkEyeColor);
			}
			if (monkEyeEyeColorNormal.a != 0f)
			{
				monkEyeMatPropBlock.SetVector(EyeColorShaderProp, monkEyeEyeColorNormal);
			}
			skinnedMeshRenderer.SetPropertyBlock(monkEyeMatPropBlock);
		}
		InvokeRepeating("AntiOverlapAssurance", 0.2f, 0.5f);
	}

	private void Start()
	{
		NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
	}

	private void OnPathComplete(Path path_)
	{
		path = path_;
		currentWaypoint = 0;
		if (path.vectorPath.Count < 1)
		{
			base.transform.position = sleepPt.position;
			base.transform.rotation = sleepPt.rotation;
			path = null;
		}
		calculatingPath = false;
	}

	private void FollowPath()
	{
		if (path == null || currentWaypoint >= path.vectorPath.Count || currentWaypoint < 0)
		{
			PickNewPath();
			if (path == null)
			{
				return;
			}
		}
		if (Distance2D(base.transform.position, path.vectorPath[currentWaypoint]) < 0.01f)
		{
			if (currentWaypoint + 1 == path.vectorPath.Count)
			{
				PickNewPath(pathFinished: true);
				return;
			}
			currentWaypoint++;
		}
		Vector3 normalized = (path.vectorPath[currentWaypoint] - base.transform.position).normalized;
		normalized.y = 0f;
		if (animController.GetCurrentAnimatorStateInfo(0).IsName("Move"))
		{
			Vector3 vector = normalized * speed;
			base.transform.position += vector * deltaTime;
		}
		Mathf.Clamp01(Vector3.Dot(base.transform.forward, normalized) / (MathF.PI / 2f));
		if (Mathf.Sign(Vector3.Cross(base.transform.forward, normalized).y) > 0f)
		{
			animController.SetLayerWeight(layerRight, 0f);
		}
		else
		{
			animController.SetLayerWeight(layerLeft, 0f);
		}
		animController.SetLayerWeight(layerForward, 0f);
		Vector3 forward = Vector3.RotateTowards(base.transform.forward, normalized, rotationSpeed * deltaTime, 0f);
		base.transform.rotation = Quaternion.LookRotation(forward);
	}

	private bool PlayerNear(VRRig rig, float dist, out float playerDist)
	{
		if (rig == null)
		{
			playerDist = float.PositiveInfinity;
			return false;
		}
		playerDist = Distance2D(rig.transform.position, base.transform.position);
		if (playerDist < dist)
		{
			return Physics.RaycastNonAlloc(new Ray(base.transform.position, rig.transform.position - base.transform.position), rayResults, playerDist, layerMask) <= 0;
		}
		return false;
	}

	private void Sleeping()
	{
		audioSource.volume = Mathf.Min(sleepLoopVolume, audioSource.volume + deltaTime / sleepDuration);
		if (audioSource.volume == sleepLoopVolume)
		{
			SetState(MonkeyeAI_ReplState.EStates.Patrolling);
			PickNewPath();
		}
	}

	private bool ClosestPlayer(in Vector3 myPos, out VRRig outRig)
	{
		float num = float.MaxValue;
		outRig = null;
		foreach (VRRig validChoosableRig in GetValidChoosableRigs())
		{
			float playerDist = 0f;
			if (PlayerNear(validChoosableRig, chaseDistance, out playerDist) && playerDist < num)
			{
				num = playerDist;
				outRig = validChoosableRig;
			}
		}
		if (num != float.MaxValue)
		{
			return true;
		}
		return false;
	}

	private bool CheckForChase()
	{
		foreach (VRRig validChoosableRig in GetValidChoosableRigs())
		{
			float playerDist = 0f;
			if (PlayerNear(validChoosableRig, wakeDistance, out playerDist))
			{
				SetTargetPlayer(validChoosableRig);
				SetState(MonkeyeAI_ReplState.EStates.Chasing);
				PickNewPath();
				return true;
			}
		}
		return false;
	}

	public void SetChasePlayer(VRRig rig)
	{
		if (GetValidChoosableRigs().Contains(rig))
		{
			SetTargetPlayer(rig);
			lockedOn = true;
			SetState(MonkeyeAI_ReplState.EStates.Chasing);
			PickNewPath();
		}
	}

	public void SetSleep()
	{
		if (replState.state == MonkeyeAI_ReplState.EStates.Patrolling || replState.state == MonkeyeAI_ReplState.EStates.Chasing)
		{
			SetState(MonkeyeAI_ReplState.EStates.Sleeping);
		}
	}

	private void Patrolling()
	{
		audioSource.volume = Mathf.Min(patrolLoopVolume, audioSource.volume + deltaTime / patrolLoopFadeInTime);
		if (path == null)
		{
			PickNewPath();
		}
		if (audioSource.volume == patrolLoopVolume)
		{
			CheckForChase();
		}
	}

	private void Chasing()
	{
		audioSource.volume = Mathf.Min(chaseLoopVolume, audioSource.volume + deltaTime / chaseLoopFadeInTime);
		PickNewPath();
		if (targetRig == null)
		{
			SetState(MonkeyeAI_ReplState.EStates.Patrolling);
		}
		else if (Distance2D(base.transform.position, targetRig.transform.position) < attackDistance)
		{
			SetState(MonkeyeAI_ReplState.EStates.BeginAttack);
		}
	}

	private void ReturnToSleepPt()
	{
		if (path == null)
		{
			PickNewPath();
		}
		if (CheckForChase())
		{
			SetState(MonkeyeAI_ReplState.EStates.Chasing);
		}
		else if (Distance2D(base.transform.position, sleepPt.position) < 0.01f)
		{
			SetState(MonkeyeAI_ReplState.EStates.Sleeping);
		}
	}

	private void UpdateClientState()
	{
		if (wasConnectedToRoom && !NetworkSystem.Instance.InRoom)
		{
			SetDefaultState();
			return;
		}
		if (ColliderEnabledManager.instance != null && !replState.floorEnabled)
		{
			if (!NetworkSystem.Instance.InRoom)
			{
				if (replState.userId == "-1")
				{
					ColliderEnabledManager.instance.DisableFloorForFrame();
				}
			}
			else if (replState.userId == NetworkSystem.Instance.LocalPlayer.UserId)
			{
				ColliderEnabledManager.instance.DisableFloorForFrame();
			}
		}
		if (portalFx.activeSelf != replState.portalEnabled)
		{
			portalFx.SetActive(replState.portalEnabled);
		}
		portalFx.transform.position = new Vector3(replState.attackPos.x, portalFx.transform.position.y, replState.attackPos.z);
		replState.timer -= deltaTime;
		if (replState.timer < 0f)
		{
			replState.timer = 0f;
		}
		VRRig rig = GetRig(replState.userId);
		if (replState.state >= MonkeyeAI_ReplState.EStates.BeginAttack)
		{
			if (rig == null)
			{
				lazerFx.DisableLazer();
			}
			else if (replState.state < MonkeyeAI_ReplState.EStates.DropPlayer)
			{
				lazerFx.EnableLazer(eyeBones, rig);
			}
			else
			{
				lazerFx.DisableLazer();
			}
		}
		else
		{
			lazerFx.DisableLazer();
		}
		if (replState.portalEnabled)
		{
			portalColor.a = replState.alpha;
			portalMatPropBlock.SetVector(tintColorShaderProp, portalColor);
			renderer.SetPropertyBlock(portalMatPropBlock);
		}
		if (GorillaTagger.Instance.offlineVRRig == rig && replState.freezePlayer)
		{
			GTPlayer.Instance.SetMaximumSlipThisFrame();
			Rigidbody rigidbody = GorillaTagger.Instance.rigidbody;
			Vector3 linearVelocity = rigidbody.linearVelocity;
			rigidbody.linearVelocity = new Vector3(linearVelocity.x * deltaTime * 4f, Mathf.Min(linearVelocity.y, 0f), linearVelocity.x * deltaTime * 4f);
		}
		if (!replState.IsMine)
		{
			SetClientState(replState.state);
		}
	}

	private void SetDefaultState()
	{
		SetState(MonkeyeAI_ReplState.EStates.Sleeping);
		SetDefaultAttackState();
	}

	private void SetDefaultAttackState()
	{
		replState.floorEnabled = true;
		replState.timer = 0f;
		replState.userId = "";
		replState.attackPos = base.transform.position;
		replState.portalEnabled = false;
		replState.freezePlayer = false;
		replState.alpha = 0f;
	}

	private void ExitAttackState()
	{
		SetDefaultAttackState();
		SetState(MonkeyeAI_ReplState.EStates.Patrolling);
	}

	private void BeginAttack()
	{
		path = null;
		replState.freezePlayer = true;
		if (replState.timer <= 0f)
		{
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.GTPlayOneShot(attackSound, attackVolume);
			}
			replState.timer = openFloorTime;
			replState.portalEnabled = true;
			SetState(MonkeyeAI_ReplState.EStates.OpenFloor);
		}
	}

	private void OpenFloor()
	{
		replState.alpha = Mathf.Lerp(0f, 1f, 1f - Mathf.Clamp01(replState.timer / openFloorTime));
		if (replState.timer <= 0f)
		{
			replState.timer = dropPlayerTime;
			replState.floorEnabled = false;
			SetState(MonkeyeAI_ReplState.EStates.DropPlayer);
		}
	}

	private void DropPlayer()
	{
		if (replState.timer <= 0f)
		{
			replState.timer = dropPlayerTime;
			replState.floorEnabled = true;
			SetState(MonkeyeAI_ReplState.EStates.CloseFloor);
		}
	}

	private void CloseFloor()
	{
		if (replState.timer <= 0f)
		{
			ExitAttackState();
		}
	}

	private void ValidateChasingRig()
	{
		if (targetRig == null)
		{
			SetTargetPlayer(null);
			return;
		}
		bool flag = false;
		foreach (VRRig validChoosableRig in GetValidChoosableRigs())
		{
			if (validChoosableRig == targetRig)
			{
				flag = true;
				SetTargetPlayer(validChoosableRig);
				break;
			}
		}
		if (!flag)
		{
			SetTargetPlayer(null);
		}
	}

	public void SetState(MonkeyeAI_ReplState.EStates state_)
	{
		if (replState.IsMine)
		{
			replState.state = state_;
		}
		animController.SetInteger(animStateID, (int)replState.state);
		switch (replState.state)
		{
		case MonkeyeAI_ReplState.EStates.Sleeping:
			setEyeColor(monkEyeEyeColorNormal);
			lockedOn = false;
			audioSource.clip = sleepLoopSound;
			audioSource.volume = 0f;
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.GTPlay();
			}
			break;
		case MonkeyeAI_ReplState.EStates.Patrolling:
			setEyeColor(monkEyeEyeColorNormal);
			lockedOn = false;
			audioSource.clip = patrolLoopSound;
			audioSource.loop = true;
			audioSource.volume = 0f;
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.GTPlay();
			}
			patrolCount = 0;
			break;
		case MonkeyeAI_ReplState.EStates.Chasing:
			setEyeColor(monkEyeEyeColorNormal);
			audioSource.loop = true;
			audioSource.volume = 0f;
			audioSource.clip = chaseLoopSound;
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.GTPlay();
			}
			break;
		case MonkeyeAI_ReplState.EStates.BeginAttack:
			setEyeColor(monkEyeEyeColorAttacking);
			if (replState.IsMine)
			{
				replState.attackPos = ((targetRig != null) ? targetRig.transform.position : base.transform.position);
				replState.timer = beginAttackTime;
			}
			break;
		case MonkeyeAI_ReplState.EStates.ReturnToSleepPt:
		case MonkeyeAI_ReplState.EStates.GoToSleep:
			break;
		}
	}

	public void SetClientState(MonkeyeAI_ReplState.EStates state_)
	{
		animController.SetInteger(animStateID, (int)replState.state);
		if (previousState != replState.state)
		{
			previousState = replState.state;
			switch (replState.state)
			{
			case MonkeyeAI_ReplState.EStates.Sleeping:
				setEyeColor(monkEyeEyeColorNormal);
				lockedOn = false;
				audioSource.clip = sleepLoopSound;
				audioSource.volume = Mathf.Min(sleepLoopVolume, audioSource.volume + deltaTime / sleepDuration);
				if (audioSource.isActiveAndEnabled)
				{
					audioSource.GTPlay();
				}
				break;
			case MonkeyeAI_ReplState.EStates.Patrolling:
				setEyeColor(monkEyeEyeColorNormal);
				lockedOn = false;
				audioSource.clip = patrolLoopSound;
				audioSource.loop = true;
				audioSource.volume = Mathf.Min(patrolLoopVolume, audioSource.volume + deltaTime / patrolLoopFadeInTime);
				if (audioSource.isActiveAndEnabled)
				{
					audioSource.GTPlay();
				}
				patrolCount = 0;
				break;
			case MonkeyeAI_ReplState.EStates.Chasing:
				setEyeColor(monkEyeEyeColorNormal);
				audioSource.loop = true;
				audioSource.volume = Mathf.Min(chaseLoopVolume, audioSource.volume + deltaTime / chaseLoopFadeInTime);
				audioSource.clip = chaseLoopSound;
				if (audioSource.isActiveAndEnabled)
				{
					audioSource.GTPlay();
				}
				break;
			case MonkeyeAI_ReplState.EStates.BeginAttack:
				setEyeColor(monkEyeEyeColorAttacking);
				break;
			}
		}
		switch (replState.state)
		{
		case MonkeyeAI_ReplState.EStates.Sleeping:
			audioSource.volume = Mathf.Min(sleepLoopVolume, audioSource.volume + deltaTime / sleepDuration);
			break;
		case MonkeyeAI_ReplState.EStates.Patrolling:
			audioSource.volume = Mathf.Min(patrolLoopVolume, audioSource.volume + deltaTime / patrolLoopFadeInTime);
			break;
		case MonkeyeAI_ReplState.EStates.Chasing:
			audioSource.volume = Mathf.Min(chaseLoopVolume, audioSource.volume + deltaTime / chaseLoopFadeInTime);
			break;
		}
	}

	private void setEyeColor(Color c)
	{
		if (c.a != 0f)
		{
			monkEyeMatPropBlock.SetVector(EyeColorShaderProp, c);
			skinnedMeshRenderer.SetPropertyBlock(monkEyeMatPropBlock);
		}
	}

	public List<VRRig> GetValidChoosableRigs()
	{
		validRigs.Clear();
		foreach (VRRig containedRig in playerCollection.containedRigs)
		{
			if ((NetworkSystem.Instance.InRoom || containedRig.isOfflineVRRig) && !(containedRig == null))
			{
				validRigs.Add(containedRig);
			}
		}
		return validRigs;
	}

	public void SliceUpdate()
	{
		wasConnectedToRoom = NetworkSystem.Instance.InRoom;
		deltaTime = Time.time - lastTime;
		lastTime = Time.time;
		UpdateClientState();
		if (NetworkSystem.Instance.InRoom && !replState.IsMine)
		{
			path = null;
			return;
		}
		if (!playerCollection.gameObject.activeInHierarchy)
		{
			NetPlayer netPlayer = null;
			float num = float.PositiveInfinity;
			foreach (VRRig containedRig in playersInRoomCollection.containedRigs)
			{
				if (!(containedRig == null))
				{
					float num2 = Vector3.Distance(base.transform.position, containedRig.transform.position);
					if (num2 < num)
					{
						netPlayer = containedRig.creator;
						num = num2;
					}
				}
			}
			if (!(num > 6f))
			{
				path = null;
				if (netPlayer != null)
				{
					replStateRequestableOwnershipGaurd.TransferOwnership(netPlayer);
					myRequestableOwnershipGaurd.TransferOwnership(netPlayer);
				}
			}
			return;
		}
		ValidateChasingRig();
		switch (replState.state)
		{
		case MonkeyeAI_ReplState.EStates.Sleeping:
			Sleeping();
			break;
		case MonkeyeAI_ReplState.EStates.Patrolling:
			Patrolling();
			break;
		case MonkeyeAI_ReplState.EStates.Chasing:
			Chasing();
			break;
		case MonkeyeAI_ReplState.EStates.ReturnToSleepPt:
			ReturnToSleepPt();
			break;
		case MonkeyeAI_ReplState.EStates.BeginAttack:
			BeginAttack();
			break;
		case MonkeyeAI_ReplState.EStates.OpenFloor:
			OpenFloor();
			break;
		case MonkeyeAI_ReplState.EStates.DropPlayer:
			DropPlayer();
			break;
		case MonkeyeAI_ReplState.EStates.CloseFloor:
			CloseFloor();
			break;
		}
		if (path != null)
		{
			FollowPath();
			velocity = base.transform.position - prevPosition;
			prevPosition = base.transform.position;
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void AntiOverlapAssurance()
	{
		try
		{
			if ((NetworkSystem.Instance.InRoom && !replState.IsMine) || !playerCollection.gameObject.activeInHierarchy)
			{
				return;
			}
			foreach (MonkeyeAI monkeyeAi in playerCollection.monkeyeAis)
			{
				if (monkeyeAi == this || !(Vector3.Distance(base.transform.position, monkeyeAi.transform.position) < overlapRadius) || !((double)Vector3.Dot(base.transform.forward, monkeyeAi.transform.forward) > 0.2))
				{
					continue;
				}
				switch (replState.state)
				{
				case MonkeyeAI_ReplState.EStates.Patrolling:
					PickNewPath();
					break;
				case MonkeyeAI_ReplState.EStates.Chasing:
					if (monkeyeAi.replState.state == MonkeyeAI_ReplState.EStates.Chasing)
					{
						SetState(MonkeyeAI_ReplState.EStates.Patrolling);
					}
					break;
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
		}
	}

	private void SetTargetPlayer([CanBeNull] VRRig rig)
	{
		if (rig == null)
		{
			replState.userId = "";
			replState.freezePlayer = false;
			replState.floorEnabled = true;
			replState.portalEnabled = false;
			targetRig = null;
		}
		else
		{
			replState.userId = UserIdFromRig(rig);
			targetRig = rig;
		}
	}
}
