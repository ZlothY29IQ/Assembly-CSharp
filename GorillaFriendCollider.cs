using System.Collections.Generic;
using GorillaNetworking;
using GTMathUtil;
using Unity.Profiling;
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

	public bool manualRefreshOnly;

	private float _nextUpdateTime = -1f;

	private static List<VRRig> playerRigs = new List<VRRig>();

	private static bool updateAdded = false;

	private static readonly ProfilerMarker profiler_SliceUpdate = new ProfilerMarker("GT/FriendCollider.SliceUpdate");

	public void Awake()
	{
		thisCapsule = GetComponent<CapsuleCollider>();
		thisBox = GetComponent<BoxCollider>();
		if (!updateAdded)
		{
			updateAdded = true;
			VRRigCache.OnActiveRigsChanged += UpdateActiveRigs;
			UpdateActiveRigs();
		}
	}

	private static void UpdateActiveRigs()
	{
		VRRigCache.Instance.GetActiveRigs(playerRigs);
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
		using (profiler_SliceUpdate.Auto())
		{
			if (NetworkSystem.Instance.InRoom || runCheckWhileNotInRoom)
			{
				RefreshPlayersWithinBounds();
			}
		}
	}

	public void RefreshPlayersWithinBounds()
	{
		playerIDsCurrentlyTouching.Clear();
		for (int i = 0; i < playerRigs.Count; i++)
		{
			float y = playerRigs[i].bodyTransform.transform.position.y;
			bool num = !applyCapsuleYLimits || (y >= capsuleColliderYLimits.x && y <= capsuleColliderYLimits.y);
			bool flag = (thisBox != null && WithinBounds.PointWithinBoxColliderBounds(playerRigs[i].rigContainer.SpeakerHead.position, thisBox)) || (thisBox == null && thisCapsule != null && WithinBounds.PointWithinCapsuleColliderBounds(playerRigs[i].rigContainer.SpeakerHead.position, thisCapsule));
			if (num && flag)
			{
				playerIDsCurrentlyTouching.Add(playerRigs[i].isLocal ? NetworkSystem.Instance.LocalPlayer.UserId : playerRigs[i].creator.UserId);
			}
		}
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.LocalPlayer != null && playerIDsCurrentlyTouching.Contains(NetworkSystem.Instance.LocalPlayer.UserId) && GorillaComputer.instance.friendJoinCollider != this)
		{
			GorillaComputer.instance.allowedMapsToJoin = myAllowedMapsToJoin;
			GorillaComputer.instance.friendJoinCollider = this;
			GorillaComputer.instance.UpdateScreen();
		}
	}
}
