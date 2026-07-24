using System.Collections.Generic;
using GTMathUtil;
using GorillaNetworking;
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

	[Tooltip("If true, then when the number of players in the collider changes call the zone callbacks.")]
	public bool updatePartyZoneCallbacks;

	private JoinTriggerUI ui;

	private float _nextUpdateTime = -1f;

	private static List<VRRig> playerRigs = new List<VRRig>();

	private static bool updateAdded = false;

	private static readonly ProfilerMarker profiler_SliceUpdate = new ProfilerMarker("GT/FriendCollider.SliceUpdate");

	private void Awake()
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

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void RegisterUI(JoinTriggerUI joinUI)
	{
		ui = joinUI;
	}

	public void UnregisterUI()
	{
		ui = null;
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
		int count = playerIDsCurrentlyTouching.Count;
		playerIDsCurrentlyTouching.Clear();
		NetPlayer localPlayer = NetworkSystem.Instance.LocalPlayer;
		if (localPlayer == null)
		{
			return;
		}
		bool flag = thisBox != null;
		bool flag2 = thisCapsule != null;
		for (int i = 0; i < playerRigs.Count; i++)
		{
			float y = playerRigs[i].bodyTransform.transform.position.y;
			if ((!applyCapsuleYLimits || (y >= capsuleColliderYLimits.x && y <= capsuleColliderYLimits.y)) && ((flag && WithinBounds.PointWithinBoxColliderBounds(playerRigs[i].rigContainer.SpeakerHead.position, thisBox)) || (!flag && flag2 && WithinBounds.PointWithinCapsuleColliderBounds(playerRigs[i].rigContainer.SpeakerHead.position, thisCapsule))))
			{
				playerIDsCurrentlyTouching.Add(playerRigs[i].isLocal ? localPlayer.UserId : playerRigs[i].creator.UserId);
			}
		}
		if (NetworkSystem.Instance.InRoom)
		{
			if (playerIDsCurrentlyTouching.Contains(localPlayer.UserId) && GorillaComputer.instance.friendJoinCollider != this)
			{
				GorillaComputer.instance.allowedMapsToJoin = myAllowedMapsToJoin;
				GorillaComputer.instance.friendJoinCollider = this;
				GorillaComputer.instance.UpdateScreen();
			}
			if (updatePartyZoneCallbacks && count != playerIDsCurrentlyTouching.Count && ui != null)
			{
				ui.TriggerUpdateUI();
			}
		}
	}
}
