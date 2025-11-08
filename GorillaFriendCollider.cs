using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

public class GorillaFriendCollider : MonoBehaviour, IGorillaSliceableSimple
{
	public List<string> playerIDsCurrentlyTouching = new List<string>();

	private CapsuleCollider thisCapsule;

	private BoxCollider thisBox;

	[Tooltip("If using a capsule collider, the player position can be checked against these minimum and maximum Y limits (world position) to make it behave more like a cylinder check")]
	public bool applyCapsuleYLimits;

	[Tooltip("If the player's Y world position is lower than Limits.x or higher than Limits.y, they will not be considered \"Inside\" the friend collider")]
	public Vector2 capsuleColliderYLimits = Vector2.zero;

	public bool runCheckWhileNotInRoom;

	public string[] myAllowedMapsToJoin;

	private readonly Collider[] overlapColliders = new Collider[20];

	private int tagAndBodyLayerMask;

	private float jiggleAmount;

	private Collider otherCollider;

	private GameObject otherColliderGO;

	private VRRig collidingRig;

	private int collisions;

	private WaitForSeconds wait1Sec = new WaitForSeconds(1f);

	public bool manualRefreshOnly;

	private float _nextUpdateTime = -1f;

	public void Awake()
	{
		thisCapsule = GetComponent<CapsuleCollider>();
		thisBox = GetComponent<BoxCollider>();
		jiggleAmount = Random.Range(0f, 1f);
		tagAndBodyLayerMask = LayerMask.GetMask("Gorilla Tag Collider") | LayerMask.GetMask("Gorilla Body Collider");
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void AddUserID(in string userID)
	{
		if (!playerIDsCurrentlyTouching.Contains(userID))
		{
			playerIDsCurrentlyTouching.Add(userID);
		}
	}

	public void SliceUpdate()
	{
		float time = Time.time;
		if (_nextUpdateTime < 0f)
		{
			_nextUpdateTime = time + 1f + jiggleAmount;
		}
		else if (!(time < _nextUpdateTime))
		{
			_nextUpdateTime = time + 1f;
			if (NetworkSystem.Instance.InRoom || runCheckWhileNotInRoom)
			{
				RefreshPlayersInSphere();
			}
		}
	}

	public void RefreshPlayersInSphere()
	{
		playerIDsCurrentlyTouching.Clear();
		if (thisBox != null)
		{
			collisions = Physics.OverlapBoxNonAlloc(thisBox.transform.position, thisBox.size / 2f, overlapColliders, thisBox.transform.rotation, tagAndBodyLayerMask);
		}
		else
		{
			collisions = Physics.OverlapSphereNonAlloc(base.transform.position, thisCapsule.radius, overlapColliders, tagAndBodyLayerMask);
		}
		collisions = Mathf.Min(collisions, overlapColliders.Length);
		if (collisions <= 0)
		{
			return;
		}
		for (int i = 0; i < collisions; i++)
		{
			otherCollider = overlapColliders[i];
			if (otherCollider == null || otherCollider.attachedRigidbody == null)
			{
				continue;
			}
			otherColliderGO = otherCollider.attachedRigidbody.gameObject;
			collidingRig = otherColliderGO.GetComponent<VRRig>();
			if (collidingRig == null || collidingRig.creator == null || collidingRig.creator.IsNull || string.IsNullOrEmpty(collidingRig.creator.UserId))
			{
				GTPlayer component = otherColliderGO.GetComponent<GTPlayer>();
				if (component == null || NetworkSystem.Instance.LocalPlayer == null)
				{
					continue;
				}
				if (thisCapsule != null && applyCapsuleYLimits)
				{
					float y = component.bodyCollider.transform.position.y;
					if (y < capsuleColliderYLimits.x || y > capsuleColliderYLimits.y)
					{
						continue;
					}
				}
				AddUserID(NetworkSystem.Instance.LocalPlayer.UserId);
			}
			else
			{
				if (thisCapsule != null && applyCapsuleYLimits)
				{
					float y2 = collidingRig.bodyTransform.transform.position.y;
					if (y2 < capsuleColliderYLimits.x || y2 > capsuleColliderYLimits.y)
					{
						continue;
					}
				}
				AddUserID(collidingRig.creator.UserId);
			}
			overlapColliders[i] = null;
		}
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.LocalPlayer != null && playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId) && GorillaComputer.instance.friendJoinCollider != this)
		{
			GorillaComputer.instance.allowedMapsToJoin = myAllowedMapsToJoin;
			GorillaComputer.instance.friendJoinCollider = this;
			GorillaComputer.instance.UpdateScreen();
		}
		otherCollider = null;
		otherColliderGO = null;
		collidingRig = null;
	}
}
