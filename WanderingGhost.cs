using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

[NetworkBehaviourWeaved(1)]
public class WanderingGhost : NetworkComponent
{
	[Serializable]
	public struct Waypoint(bool visible, Transform tr)
	{
		[Tooltip("The ghost will be visible when its reached to this waypoint")]
		public bool _visible = visible;

		public Transform _transform = tr;
	}

	private enum ghostState
	{
		patrol,
		idle
	}

	public float patrolSpeed = 3f;

	public float idleStayDuration = 5f;

	public float sphereColliderRadius = 2f;

	public ThrowableSetDressing[] allFlowers;

	public Vector3 flowerDisabledPosition;

	public float flowerSpawnRadius;

	public float flowerSpawnDuration;

	public LayerMask flowerGroundMask;

	public MeshRenderer mrenderer;

	public Material visibleMaterial;

	public Material scryableMaterial;

	public GameObject waypointsContainer;

	private ZoneBasedObject[] waypointRegions;

	private ZoneBasedObject lastWaypointRegion;

	private List<Waypoint> waypoints = new List<Waypoint>();

	private Waypoint currentWaypoint;

	public string debugForceWaypointRegion;

	[SerializeField]
	private AudioSource audioSource;

	public AudioClip[] appearAudio;

	public float idleVolume;

	public AudioClip patrolAudio;

	public float patrolVolume;

	private ghostState currentState;

	private float idlePassedTime;

	public UnityAction<GameObject> TriggerHauntedObjects;

	private Vector3 hoverVelocity;

	public float hoverRectifyForce;

	public float hoverRandomForce;

	public float hoverDrag;

	private const int maxColliders = 10;

	private Collider[] hitColliders = new Collider[10];

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private ghostState _Data;

	[Networked]
	[NetworkedWeaved(0, 1)]
	private unsafe ghostState Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing WanderingGhost.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(ghostState*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing WanderingGhost.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(ghostState*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Start()
	{
		base.Start();
		waypointRegions = waypointsContainer.GetComponentsInChildren<ZoneBasedObject>();
		idlePassedTime = 0f;
		ThrowableSetDressing[] array = allFlowers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].anchor.position = flowerDisabledPosition;
		}
		Invoke("DelayedStart", 0.5f);
	}

	private void DelayedStart()
	{
		PickNextWaypoint();
		base.transform.position = currentWaypoint._transform.position;
		PickNextWaypoint();
		ChangeState(ghostState.patrol);
	}

	private void LateUpdate()
	{
		UpdateState();
		hoverVelocity -= mrenderer.transform.localPosition * hoverRectifyForce * Time.deltaTime;
		hoverVelocity += UnityEngine.Random.insideUnitSphere * hoverRandomForce * Time.deltaTime;
		hoverVelocity = Vector3.MoveTowards(hoverVelocity, Vector3.zero, hoverDrag * Time.deltaTime);
		mrenderer.transform.localPosition += hoverVelocity * Time.deltaTime;
	}

	private void PickNextWaypoint()
	{
		if (waypoints.Count == 0 || lastWaypointRegion == null || !lastWaypointRegion.IsLocalPlayerInZone())
		{
			ZoneBasedObject zoneBasedObject = ZoneBasedObject.SelectRandomEligible(waypointRegions, debugForceWaypointRegion);
			if (zoneBasedObject == null)
			{
				zoneBasedObject = lastWaypointRegion;
			}
			if (zoneBasedObject == null)
			{
				return;
			}
			lastWaypointRegion = zoneBasedObject;
			waypoints.Clear();
			foreach (Transform item in zoneBasedObject.transform)
			{
				waypoints.Add(new Waypoint(item.name.Contains("_v_"), item));
			}
		}
		int index = UnityEngine.Random.Range(0, waypoints.Count);
		currentWaypoint = waypoints[index];
		waypoints.RemoveAt(index);
	}

	private void Patrol()
	{
		idlePassedTime = 0f;
		mrenderer.sharedMaterial = scryableMaterial;
		Transform transform = currentWaypoint._transform;
		base.transform.position = Vector3.MoveTowards(base.transform.position, transform.position, patrolSpeed * Time.deltaTime);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(transform.position - base.transform.position), 360f * Time.deltaTime);
	}

	private bool MaybeHideGhost()
	{
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, sphereColliderRadius, hitColliders);
		for (int i = 0; i < num; i++)
		{
			if (hitColliders[i].gameObject.IsOnLayer(UnityLayer.GorillaHand) || hitColliders[i].gameObject.IsOnLayer(UnityLayer.GorillaBodyCollider))
			{
				ChangeState(ghostState.patrol);
				return true;
			}
		}
		return false;
	}

	private void ChangeState(ghostState newState)
	{
		currentState = newState;
		mrenderer.sharedMaterial = ((newState == ghostState.idle) ? visibleMaterial : scryableMaterial);
		switch (newState)
		{
		case ghostState.patrol:
			audioSource.GTStop();
			audioSource.volume = patrolVolume;
			audioSource.clip = patrolAudio;
			audioSource.GTPlay();
			break;
		case ghostState.idle:
			audioSource.GTStop();
			audioSource.volume = idleVolume;
			audioSource.GTPlayOneShot(appearAudio.GetRandomItem());
			if (NetworkSystem.Instance.IsMasterClient)
			{
				SpawnFlowerNearby();
			}
			break;
		}
	}

	private void UpdateState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		switch (currentState)
		{
		case ghostState.patrol:
			if (currentWaypoint._transform == null)
			{
				PickNextWaypoint();
				break;
			}
			Patrol();
			if (Vector3.Distance(base.transform.position, currentWaypoint._transform.position) < 0.2f)
			{
				if (currentWaypoint._visible)
				{
					ChangeState(ghostState.idle);
				}
				else
				{
					PickNextWaypoint();
				}
			}
			break;
		case ghostState.idle:
			idlePassedTime += Time.deltaTime;
			if (idlePassedTime >= idleStayDuration || MaybeHideGhost())
			{
				PickNextWaypoint();
				ChangeState(ghostState.patrol);
			}
			break;
		}
	}

	private void HauntObjects()
	{
		Collider[] array = new Collider[20];
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, sphereColliderRadius, array);
		for (int i = 0; i < num; i++)
		{
			if (array[i].CompareTag("HauntedObject"))
			{
				TriggerHauntedObjects?.Invoke(array[i].gameObject);
			}
		}
	}

	public override void WriteDataFusion()
	{
		Data = currentState;
	}

	public override void ReadDataFusion()
	{
		ReadDataShared(Data);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(currentState);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			ghostState state = (ghostState)stream.ReceiveNext();
			ReadDataShared(state);
		}
	}

	private void ReadDataShared(ghostState state)
	{
		ghostState num = currentState;
		currentState = state;
		if (num != currentState)
		{
			ChangeState(currentState);
		}
	}

	public override void OnOwnerChange(Player newOwner, Player previousOwner)
	{
		base.OnOwnerChange(newOwner, previousOwner);
		if (newOwner == PhotonNetwork.LocalPlayer)
		{
			ChangeState(currentState);
		}
	}

	private void SpawnFlowerNearby()
	{
		Vector3 position = base.transform.position + Vector3.down * 0.25f;
		if (Physics.Raycast(new Ray(base.transform.position + UnityEngine.Random.insideUnitCircle.x0y() * flowerSpawnRadius, Vector3.down), out var hitInfo, 3f, flowerGroundMask))
		{
			position = hitInfo.point;
		}
		ThrowableSetDressing throwableSetDressing = null;
		int num = 0;
		ThrowableSetDressing[] array = allFlowers;
		foreach (ThrowableSetDressing throwableSetDressing2 in array)
		{
			if (!throwableSetDressing2.InHand())
			{
				num++;
				if (UnityEngine.Random.Range(0, num) == 0)
				{
					throwableSetDressing = throwableSetDressing2;
				}
			}
		}
		if (throwableSetDressing != null)
		{
			if (!throwableSetDressing.IsLocalOwnedWorldShareable)
			{
				throwableSetDressing.WorldShareableRequestOwnership();
			}
			throwableSetDressing.SetWillTeleport();
			throwableSetDressing.transform.position = position;
			throwableSetDressing.StartRespawnTimer(flowerSpawnDuration);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
