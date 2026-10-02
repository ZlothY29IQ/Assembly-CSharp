using System;
using System.Collections.Generic;
using GTMathUtil;
using GorillaNetworking;
using Unity.Profiling;
using UnityEngine;

public class GorillaFriendCollider : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public struct TouchingPlayerInfo : IEquatable<TouchingPlayerInfo>
	{
		public string UserID { get; private set; }

		public float EnteredTime { get; private set; }

		public TouchingPlayerInfo(string userid, float enteredTime)
		{
			EnteredTime = enteredTime;
			UserID = userid;
		}

		bool IEquatable<TouchingPlayerInfo>.Equals(TouchingPlayerInfo other)
		{
			if (!string.IsNullOrEmpty(UserID) && !string.IsNullOrEmpty(other.UserID))
			{
				return UserID == other.UserID;
			}
			return false;
		}

		public static implicit operator TouchingPlayerInfo(string userid)
		{
			return new TouchingPlayerInfo(userid, 0f);
		}

		public static implicit operator string(TouchingPlayerInfo playerInfo)
		{
			return playerInfo.UserID;
		}
	}

	public List<TouchingPlayerInfo> playerIDsCurrentlyTouching = new List<TouchingPlayerInfo>(20);

	private List<TouchingPlayerInfo> m_prevPlayerIDsCurrentlyTouching = new List<TouchingPlayerInfo>(20);

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
		List<TouchingPlayerInfo> prevPlayerIDsCurrentlyTouching = m_prevPlayerIDsCurrentlyTouching;
		List<TouchingPlayerInfo> prevPlayerIDsCurrentlyTouching2 = playerIDsCurrentlyTouching;
		playerIDsCurrentlyTouching = prevPlayerIDsCurrentlyTouching;
		m_prevPlayerIDsCurrentlyTouching = prevPlayerIDsCurrentlyTouching2;
		playerIDsCurrentlyTouching.Clear();
		int num = -1;
		bool flag = thisBox != null;
		bool flag2 = thisCapsule != null;
		for (int i = 0; i < playerRigs.Count; i++)
		{
			VRRig vRRig = playerRigs[i];
			string userid = vRRig.creator.UserId;
			float y = vRRig.bodyTransform.transform.position.y;
			if ((!applyCapsuleYLimits || (y >= capsuleColliderYLimits.x && y <= capsuleColliderYLimits.y)) && ((flag && WithinBounds.PointWithinBoxColliderBounds(vRRig.rigContainer.SpeakerHead.position, thisBox)) || (!flag && flag2 && WithinBounds.PointWithinCapsuleColliderBounds(vRRig.rigContainer.SpeakerHead.position, thisCapsule))))
			{
				if (vRRig.isLocal)
				{
					num = playerIDsCurrentlyTouching.Count;
				}
				int num2 = m_prevPlayerIDsCurrentlyTouching.FindIndex((TouchingPlayerInfo info) => info.UserID == userid);
				playerIDsCurrentlyTouching.Add((num2 > -1) ? m_prevPlayerIDsCurrentlyTouching[num2] : new TouchingPlayerInfo(userid, Time.time));
			}
		}
		if (NetworkSystem.Instance.InRoom)
		{
			if (num > -1 && GorillaComputer.instance.friendJoinCollider != this)
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
