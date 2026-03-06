using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class SecondLookSkeleton : MonoBehaviour
{
	public enum GhostState
	{
		Unactivated,
		Activated,
		Patrolling,
		Chasing,
		CaughtPlayer,
		PlayerThrown,
		Reset
	}

	public Transform[] angerPoint;

	public int angerPointIndex;

	public SkeletonPathingNode[] pathPoints;

	public SkeletonPathingNode[] exitPoints;

	public Transform heightOffset;

	public bool requireSecondLookToActivate;

	public bool requireTappingToActivate;

	public bool changeAngerPointOnTimeInterval;

	public float changeAngerPointTimeMinutes = 3f;

	private bool firstLookActivated;

	private bool lookedAway;

	private bool currentlyLooking;

	public float ghostActivationDistance;

	public GameObject spookyGhost;

	public float timeFirstAppeared;

	public float timeToFirstDisappear;

	public GhostState currentState;

	public GameObject spookyText;

	public float patrolSpeed;

	public float chaseSpeed;

	public float caughtSpeed;

	public SkeletonPathingNode firstNode;

	public SkeletonPathingNode currentNode;

	public SkeletonPathingNode nextNode;

	public Transform lookSource;

	private Transform playerTransform;

	public float reachNodeDist;

	public float maxRotSpeed;

	public float hapticStrength;

	public float hapticDuration;

	public Vector3 offsetGrabPosition;

	public float throwForce;

	public Animator animator;

	public float bodyHeightOffset;

	private float timeThrown;

	public float timeThrownCooldown = 1f;

	public float catchDistance;

	public float maxSeeDistance;

	private RaycastHit[] rHits;

	public LayerMask mask;

	public LayerMask playerMask;

	public AudioSource audioSource;

	public AudioClip initialScream;

	public AudioClip patrolLoop;

	public AudioClip chaseLoop;

	public AudioClip grabbedSound;

	public AudioClip carryingLoop;

	public AudioClip throwSound;

	public List<SkeletonPathingNode> resetChaseHistory = new List<SkeletonPathingNode>();

	private SecondLookSkeletonSynchValues synchValues;

	private bool localCaught;

	private bool localThrown;

	public List<NetPlayer> playersSeen;

	public bool tapped;

	private RaycastHit closest;

	private float angerPointChangedTime;

	private void Start()
	{
		playersSeen = new List<NetPlayer>();
		synchValues = GetComponent<SecondLookSkeletonSynchValues>();
		playerTransform = Camera.main.transform;
		tapped = !requireTappingToActivate;
		localCaught = false;
		audioSource = GetComponentInChildren<AudioSource>();
		spookyGhost.SetActive(value: false);
		angerPointIndex = Random.Range(0, angerPoint.Length);
		angerPointChangedTime = Time.time;
		synchValues.angerPoint = angerPointIndex;
		spookyGhost.transform.position = angerPoint[synchValues.angerPoint].position;
		spookyGhost.transform.rotation = angerPoint[synchValues.angerPoint].rotation;
		ChangeState(GhostState.Unactivated);
		rHits = new RaycastHit[20];
		lookedAway = false;
		firstLookActivated = false;
		animator.Play("ArmsOut");
	}

	private void Update()
	{
		ProcessGhostState();
	}

	public void ChangeState(GhostState newState)
	{
		if (newState == currentState)
		{
			return;
		}
		switch (newState)
		{
		case GhostState.Unactivated:
			spookyGhost.gameObject.SetActive(value: false);
			audioSource.GTStop();
			audioSource.loop = false;
			if (IsMine())
			{
				synchValues.angerPoint = Random.Range(0, angerPoint.Length);
				angerPointIndex = synchValues.angerPoint;
				angerPointChangedTime = Time.time;
				spookyGhost.transform.position = angerPoint[angerPointIndex].position;
				spookyGhost.transform.rotation = angerPoint[angerPointIndex].rotation;
			}
			currentState = GhostState.Unactivated;
			break;
		case GhostState.Activated:
			currentState = GhostState.Activated;
			if (tapped)
			{
				GTAudioSourceExtensions.GTPlayClipAtPoint(initialScream, audioSource.transform.position, 1f);
				if (spookyText != null)
				{
					spookyText.SetActive(value: true);
				}
				spookyGhost.SetActive(value: true);
			}
			animator.Play("ArmsOut");
			spookyGhost.transform.rotation = Quaternion.LookRotation(playerTransform.position - spookyGhost.transform.position, Vector3.up);
			if (IsMine())
			{
				timeFirstAppeared = Time.time;
			}
			break;
		case GhostState.Patrolling:
			playersSeen.Clear();
			if (tapped)
			{
				spookyGhost.SetActive(value: true);
				animator.Play("CrawlPatrol");
				audioSource.loop = true;
				audioSource.clip = patrolLoop;
				audioSource.GTPlay();
			}
			if (IsMine())
			{
				currentNode = pathPoints[Random.Range(0, pathPoints.Length)];
				nextNode = currentNode.connectedNodes[Random.Range(0, currentNode.connectedNodes.Length)];
				SyncNodes();
				spookyGhost.transform.position = currentNode.transform.position;
			}
			currentState = GhostState.Patrolling;
			break;
		case GhostState.Chasing:
			currentState = GhostState.Chasing;
			resetChaseHistory.Clear();
			animator.Play("CrawlChase");
			localThrown = false;
			localCaught = false;
			if (tapped)
			{
				audioSource.clip = chaseLoop;
				audioSource.loop = true;
				audioSource.GTPlay();
			}
			break;
		case GhostState.CaughtPlayer:
			currentState = GhostState.CaughtPlayer;
			heightOffset.localPosition = Vector3.zero;
			if (tapped)
			{
				audioSource.GTPlayOneShot(grabbedSound);
				audioSource.loop = true;
				audioSource.clip = carryingLoop;
				audioSource.GTPlay();
				animator.Play("ArmsOut");
			}
			if (!IsMine())
			{
				SetNodes();
			}
			break;
		case GhostState.PlayerThrown:
			currentState = GhostState.PlayerThrown;
			timeThrown = Time.time;
			localThrown = false;
			break;
		case GhostState.Reset:
			break;
		}
	}

	private void ProcessGhostState()
	{
		if (IsMine())
		{
			switch (currentState)
			{
			case GhostState.Unactivated:
				if (changeAngerPointOnTimeInterval && Time.time - angerPointChangedTime > changeAngerPointTimeMinutes * 60f)
				{
					synchValues.angerPoint = Random.Range(0, angerPoint.Length);
					angerPointIndex = synchValues.angerPoint;
					angerPointChangedTime = Time.time;
				}
				spookyGhost.transform.position = angerPoint[angerPointIndex].position;
				spookyGhost.transform.rotation = angerPoint[angerPointIndex].rotation;
				CheckActivateGhost();
				break;
			case GhostState.Activated:
				if (Time.time > timeFirstAppeared + timeToFirstDisappear)
				{
					ChangeState(GhostState.Patrolling);
				}
				break;
			case GhostState.Patrolling:
				if (!CheckPlayerSeen() && playersSeen.Count == 0)
				{
					PatrolMove();
				}
				else
				{
					StartChasing();
				}
				break;
			case GhostState.Chasing:
				if (!CheckPlayerSeen() || !CanGrab())
				{
					ChaseMove();
				}
				else
				{
					GrabPlayer();
				}
				break;
			case GhostState.CaughtPlayer:
				CaughtPlayerUpdate();
				break;
			case GhostState.PlayerThrown:
				if (Time.time > timeThrown + timeThrownCooldown)
				{
					ChangeState(GhostState.Unactivated);
				}
				break;
			case GhostState.Reset:
				break;
			}
			return;
		}
		SetTappedState();
		switch (currentState)
		{
		case GhostState.Unactivated:
			SetNodes();
			spookyGhost.transform.position = angerPoint[angerPointIndex].position;
			spookyGhost.transform.rotation = angerPoint[angerPointIndex].rotation;
			CheckActivateGhost();
			break;
		case GhostState.Activated:
			FollowPosition();
			break;
		case GhostState.Patrolling:
			FollowPosition();
			CheckPlayerSeen();
			break;
		case GhostState.Chasing:
			if (CheckPlayerSeen() && CanGrab())
			{
				GrabPlayer();
			}
			FollowPosition();
			break;
		case GhostState.CaughtPlayer:
		case GhostState.PlayerThrown:
			CaughtPlayerUpdate();
			break;
		case GhostState.Reset:
			break;
		}
	}

	private void CaughtPlayerUpdate()
	{
		if (localThrown)
		{
			return;
		}
		if (GhostAtExit())
		{
			if (localCaught)
			{
				ChuckPlayer();
			}
			if (IsMine())
			{
				DeactivateGhost();
			}
		}
		else
		{
			CaughtMove();
			if (localCaught)
			{
				FloatPlayer();
			}
			else if (CheckPlayerSeen() && CanGrab())
			{
				localCaught = true;
			}
		}
	}

	private void SetTappedState()
	{
		if (!tapped)
		{
			return;
		}
		if (spookyText != null && !spookyText.activeSelf)
		{
			spookyText.SetActive(value: true);
		}
		if (!spookyGhost.activeSelf || currentState == GhostState.Unactivated)
		{
			spookyGhost.SetActive(value: true);
			switch (currentState)
			{
			case GhostState.Unactivated:
				spookyGhost.SetActive(value: false);
				break;
			case GhostState.Activated:
				animator.Play("ArmsOut");
				break;
			case GhostState.Patrolling:
				animator.Play("CrawlPatrol");
				audioSource.loop = true;
				audioSource.clip = patrolLoop;
				audioSource.GTPlay();
				break;
			case GhostState.Chasing:
				audioSource.clip = chaseLoop;
				audioSource.loop = true;
				audioSource.GTPlay();
				animator.Play("CrawlChase");
				spookyGhost.SetActive(value: true);
				break;
			case GhostState.PlayerThrown:
				animator.Play("ArmsOut");
				break;
			case GhostState.CaughtPlayer:
				audioSource.GTPlayOneShot(grabbedSound);
				audioSource.loop = true;
				audioSource.clip = carryingLoop;
				audioSource.GTPlay();
				animator.Play("ArmsOut");
				break;
			case GhostState.Reset:
				break;
			}
		}
	}

	private void FollowPosition()
	{
		spookyGhost.transform.position = Vector3.Lerp(spookyGhost.transform.position, synchValues.position, 0.66f);
		spookyGhost.transform.rotation = Quaternion.Lerp(spookyGhost.transform.rotation, synchValues.rotation, 0.66f);
		if (currentState == GhostState.Patrolling || currentState == GhostState.Chasing)
		{
			SetHeightOffset();
		}
		else
		{
			heightOffset.localPosition = Vector3.zero;
		}
	}

	private void CheckActivateGhost()
	{
		if (!tapped || currentState != GhostState.Unactivated || playerTransform == null)
		{
			return;
		}
		currentlyLooking = IsCurrentlyLooking();
		if (requireSecondLookToActivate)
		{
			if (!firstLookActivated && currentlyLooking)
			{
				firstLookActivated = currentlyLooking;
			}
			else if (firstLookActivated && !currentlyLooking)
			{
				lookedAway = true;
			}
			else if (firstLookActivated && lookedAway && currentlyLooking)
			{
				firstLookActivated = false;
				lookedAway = false;
				ActivateGhost();
			}
		}
		else if (currentlyLooking)
		{
			ActivateGhost();
		}
	}

	private bool CanSeePlayer()
	{
		return CanSeePlayerWithResults(out closest);
	}

	private bool CanSeePlayerWithResults(out RaycastHit closest)
	{
		Vector3 vector = playerTransform.position - lookSource.position;
		int num = Physics.RaycastNonAlloc(lookSource.position, vector.normalized, rHits, maxSeeDistance, mask, QueryTriggerInteraction.Ignore);
		closest = rHits[0];
		if (num == 0)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			if (closest.distance > rHits[i].distance)
			{
				closest = rHits[i];
			}
		}
		return ((int)playerMask & (1 << closest.collider.gameObject.layer)) != 0;
	}

	private void ActivateGhost()
	{
		if (IsMine())
		{
			ChangeState(GhostState.Activated);
		}
		else
		{
			synchValues.SendRPC("RemoteActivateGhost", RpcTarget.MasterClient);
		}
	}

	private void StartChasing()
	{
		if (IsMine())
		{
			ChangeState(GhostState.Chasing);
		}
	}

	private bool CheckPlayerSeen()
	{
		if (!tapped)
		{
			return false;
		}
		if (playersSeen.Contains(NetworkSystem.Instance.LocalPlayer))
		{
			return true;
		}
		if (!CanSeePlayer())
		{
			return false;
		}
		if (NetworkSystem.Instance.InRoom)
		{
			synchValues.SendRPC("RemotePlayerSeen", RpcTarget.Others);
		}
		playersSeen.Add(NetworkSystem.Instance.LocalPlayer);
		return true;
	}

	public void RemoteActivateGhost()
	{
		if (IsMine() && currentState == GhostState.Unactivated)
		{
			ActivateGhost();
		}
	}

	public void RemotePlayerSeen(NetPlayer player)
	{
		if (IsMine() && !playersSeen.Contains(player))
		{
			playersSeen.Add(player);
		}
	}

	public void RemotePlayerCaught(NetPlayer player)
	{
		if (IsMine() && currentState == GhostState.Chasing)
		{
			VRRigCache.Instance.TryGetVrrig(player, out var playerRig);
			if (playerRig != null && playersSeen.Contains(player))
			{
				ChangeState(GhostState.CaughtPlayer);
			}
		}
	}

	private bool IsCurrentlyLooking()
	{
		if (Vector3.Dot(playerTransform.forward, -spookyGhost.transform.forward) > 0f && (spookyGhost.transform.position - playerTransform.position).magnitude < ghostActivationDistance)
		{
			return CanSeePlayer();
		}
		return false;
	}

	private void PatrolMove()
	{
		GhostMove(nextNode.transform, patrolSpeed);
		SetHeightOffset();
		CheckReachedNextNode(forChuck: false, forChase: false);
	}

	private void CheckReachedNextNode(bool forChuck, bool forChase)
	{
		if (!((nextNode.transform.position - spookyGhost.transform.position).magnitude < reachNodeDist))
		{
			return;
		}
		if (nextNode.connectedNodes.Length == 1)
		{
			currentNode = nextNode;
			nextNode = nextNode.connectedNodes[0];
			SyncNodes();
			return;
		}
		if (forChuck)
		{
			float distanceToExitNode = nextNode.distanceToExitNode;
			SkeletonPathingNode skeletonPathingNode = nextNode.connectedNodes[0];
			for (int i = 0; i < nextNode.connectedNodes.Length; i++)
			{
				if (!(nextNode.connectedNodes[i].distanceToExitNode > distanceToExitNode))
				{
					skeletonPathingNode = nextNode.connectedNodes[i];
					distanceToExitNode = skeletonPathingNode.distanceToExitNode;
				}
			}
			currentNode = nextNode;
			nextNode = skeletonPathingNode;
			SyncNodes();
			return;
		}
		if (forChase)
		{
			float num = float.MaxValue;
			float num2 = num;
			RigContainer playerRig = GorillaTagger.Instance.offlineVRRig.rigContainer;
			RigContainer rigContainer = playerRig;
			for (int j = 0; j < playersSeen.Count; j++)
			{
				VRRigCache.Instance.TryGetVrrig(playersSeen[j], out playerRig);
				if (!(playerRig == null))
				{
					num = (playerRig.transform.position - nextNode.transform.position).sqrMagnitude;
					if (num < num2)
					{
						rigContainer = playerRig;
						num2 = num;
					}
				}
			}
			Vector3 vector = rigContainer.transform.position - nextNode.transform.position;
			SkeletonPathingNode skeletonPathingNode2 = nextNode.connectedNodes[0];
			num2 = 0f;
			for (int k = 0; k < nextNode.connectedNodes.Length; k++)
			{
				Vector3 vector2 = nextNode.connectedNodes[k].transform.position - nextNode.transform.position;
				num = Mathf.Sign(Vector3.Dot(vector, vector2)) * Vector3.Project(vector, vector2).sqrMagnitude;
				if (!(num < num2))
				{
					skeletonPathingNode2 = nextNode.connectedNodes[k];
					num2 = num;
				}
			}
			currentNode = nextNode;
			nextNode = skeletonPathingNode2;
			SyncNodes();
			resetChaseHistory.Add(nextNode);
			if (resetChaseHistory.Count > 8)
			{
				resetChaseHistory.RemoveAt(0);
			}
			if (resetChaseHistory.Count >= 8 && resetChaseHistory[0] == resetChaseHistory[2] == (bool)resetChaseHistory[4] == (bool)resetChaseHistory[6] && resetChaseHistory[1] == resetChaseHistory[3] == (bool)resetChaseHistory[5] == (bool)resetChaseHistory[7])
			{
				resetChaseHistory.Clear();
				ChangeState(GhostState.Patrolling);
			}
			return;
		}
		SkeletonPathingNode skeletonPathingNode3 = nextNode.connectedNodes[Random.Range(0, nextNode.connectedNodes.Length)];
		for (int l = 0; l < 10; l++)
		{
			skeletonPathingNode3 = nextNode.connectedNodes[Random.Range(0, nextNode.connectedNodes.Length)];
			if (!skeletonPathingNode3.ejectionPoint && skeletonPathingNode3 != currentNode)
			{
				break;
			}
		}
		currentNode = nextNode;
		nextNode = skeletonPathingNode3;
		SyncNodes();
	}

	private void ChaseMove()
	{
		GhostMove(nextNode.transform, chaseSpeed);
		SetHeightOffset();
		CheckReachedNextNode(forChuck: false, forChase: true);
	}

	private void CaughtMove()
	{
		GhostMove(nextNode.transform, caughtSpeed);
		CheckReachedNextNode(forChuck: true, forChase: false);
		SyncNodes();
	}

	private void SyncNodes()
	{
		synchValues.currentNode = pathPoints.IndexOfRef(currentNode);
		synchValues.nextNode = pathPoints.IndexOfRef(nextNode);
		synchValues.angerPoint = angerPointIndex;
	}

	public void SetNodes()
	{
		if (synchValues.currentNode <= pathPoints.Length && synchValues.currentNode >= 0)
		{
			currentNode = pathPoints[synchValues.currentNode];
			nextNode = pathPoints[synchValues.nextNode];
			angerPointIndex = synchValues.angerPoint;
		}
	}

	private bool GhostAtExit()
	{
		if (currentNode.distanceToExitNode == 0f)
		{
			return (spookyGhost.transform.position - currentNode.transform.position).magnitude < reachNodeDist;
		}
		return false;
	}

	private void GhostMove(Transform target, float speed)
	{
		spookyGhost.transform.rotation = Quaternion.RotateTowards(spookyGhost.transform.rotation, Quaternion.LookRotation(target.position - spookyGhost.transform.position, Vector3.up), maxRotSpeed * Time.deltaTime);
		spookyGhost.transform.position += (target.position - spookyGhost.transform.position).normalized * speed * Time.deltaTime;
	}

	private void DeactivateGhost()
	{
		ChangeState(GhostState.PlayerThrown);
	}

	private bool CanGrab()
	{
		return (spookyGhost.transform.position - playerTransform.position).magnitude < catchDistance;
	}

	private void GrabPlayer()
	{
		if (IsMine())
		{
			if (currentState == GhostState.Chasing)
			{
				ChangeState(GhostState.CaughtPlayer);
			}
			localCaught = true;
		}
		synchValues.SendRPC("RemotePlayerCaught", RpcTarget.MasterClient);
	}

	private void FloatPlayer()
	{
		if (CanSeePlayerWithResults(out var raycastHit))
		{
			GorillaTagger.Instance.rigidbody.MovePosition(Vector3.MoveTowards(GorillaTagger.Instance.rigidbody.position, spookyGhost.transform.position + spookyGhost.transform.rotation * offsetGrabPosition, caughtSpeed * 10f * Time.deltaTime));
		}
		else
		{
			Vector3 vector = raycastHit.point - playerTransform.position;
			vector += GTPlayer.Instance.headCollider.radius * 1.05f * vector.normalized;
			GorillaTagger.Instance.transform.parent.position += vector;
			GTPlayer.Instance.InitializeValues();
		}
		GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
		EquipmentInteractor.instance.ForceStopClimbing();
		GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, 0.25f);
		GorillaTagger.Instance.StartVibration(forLeftController: true, hapticStrength / 4f, Time.deltaTime);
		GorillaTagger.Instance.StartVibration(forLeftController: false, hapticStrength / 4f, Time.deltaTime);
	}

	private void ChuckPlayer()
	{
		localCaught = false;
		localThrown = true;
		Vector3 vector = currentNode.transform.position - currentNode.connectedNodes[0].transform.position;
		Rigidbody rigidbody = GorillaTagger.Instance?.rigidbody;
		GTAudioSourceExtensions.GTPlayClipAtPoint(throwSound, audioSource.transform.position, 0.25f);
		audioSource.GTStop();
		audioSource.loop = false;
		if (rigidbody != null)
		{
			rigidbody.linearVelocity = vector.normalized * throwForce;
		}
	}

	private void SetHeightOffset()
	{
		int num = Physics.RaycastNonAlloc(spookyGhost.transform.position + Vector3.up * bodyHeightOffset, Vector3.down, rHits, maxSeeDistance, mask, QueryTriggerInteraction.Ignore);
		if (num == 0)
		{
			heightOffset.localPosition = Vector3.zero;
			return;
		}
		RaycastHit raycastHit = rHits[0];
		for (int i = 0; i < num; i++)
		{
			if (raycastHit.distance < rHits[i].distance)
			{
				raycastHit = rHits[i];
			}
		}
		heightOffset.localPosition = new Vector3(0f, 0f - raycastHit.distance, 0f);
	}

	private bool IsMine()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			return synchValues.IsMine;
		}
		return true;
	}
}
