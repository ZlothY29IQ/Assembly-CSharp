using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaNetworking;
using GorillaTag;
using Photon.Pun;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Scripting;

namespace GorillaTagScripts;

public class FriendshipGroupDetection : NetworkSceneObject, ITickSystemTick
{
	private struct PlayerFist
	{
		public int actorNumber;

		public Vector3 position;

		public bool isLeftHand;
	}

	[SerializeField]
	private float detectionRadius = 0.5f;

	[SerializeField]
	private float groupTime = 5f;

	[SerializeField]
	private float cooldownAfterCreatingGroup = 5f;

	[SerializeField]
	private float hapticStrength = 1.5f;

	[SerializeField]
	private float hapticDuration = 2f;

	[SerializeField]
	private double joinedRoomRefreshPartyDelay = 30.0;

	[SerializeField]
	private double failedToFollowRefreshPartyDelay = 30.0;

	public bool debug;

	public double offset = 0.5;

	[SerializeField]
	private float m_maxGroupJoinTimeDifference = 1f;

	private List<string> myPartyMemberIDs;

	private HashSet<string> myPartyMembersHash = new HashSet<string>();

	private List<Action<GroupJoinZoneAB>> groupZoneCallbacks = new List<Action<GroupJoinZoneAB>>();

	[SerializeField]
	private GTColor.HSVRanges braceletRandomColorHSVRanges;

	public GameObject friendshipBubble;

	public AudioClip fistBumpInterruptedAudio;

	private ParticleSystem particleSystem;

	private AudioSource audioSource;

	private double lastJoinedRoomTime;

	private bool wantsPartyRefreshPostJoin;

	private double lastFailedToFollowPartyTime;

	private bool wantsPartyRefreshPostFollowFailed;

	private Queue<PlayerFist> playersToPropagateFrom = new Queue<PlayerFist>();

	private List<int> playersInProvisionalGroup = new List<int>();

	private List<int> provisionalGroupUsingLeftHands = new List<int>();

	private List<int> tempIntList = new List<int>();

	private bool amFirstProvisionalPlayer;

	private Dictionary<int, int[]> partyMergeIDs = new Dictionary<int, int[]>();

	private float groupCreateAfterTimestamp;

	private float playEffectsAfterTimestamp;

	[SerializeField]
	private float playEffectsDelay;

	private float suppressPartyCreationUntilTimestamp;

	private bool WillJoinLeftHanded;

	private static readonly ProfilerMarker profiler_Tick = new ProfilerMarker("GT/FriendshipGroupDetection.Tick");

	private List<PlayerFist> playersMakingFists = new List<PlayerFist>();

	private static readonly ProfilerMarker profiler_updateProvisionalGroup = new ProfilerMarker("GT/FriendshipGroupDetection.UpdateProvisionalGroup");

	private StringBuilder debugStr = new StringBuilder();

	private float aboutToGroupJoin_CooldownUntilTimestamp;

	private static Dictionary<int, string> userIdLookup = new Dictionary<int, string>();

	private static Dictionary<string, Color> tempColorLookup = new Dictionary<string, Color>();

	public static FriendshipGroupDetection Instance { get; private set; }

	public List<Color> myBeadColors { get; private set; } = new List<Color>();

	public Color myBraceletColor { get; private set; }

	public int MyBraceletSelfIndex { get; private set; }

	public List<string> PartyMemberIDs => myPartyMemberIDs;

	public bool IsInParty => myPartyMemberIDs != null;

	public GroupJoinZoneAB partyZone { get; private set; }

	public bool TickRunning { get; set; }

	public bool DidJoinLeftHanded { get; private set; }

	private void Awake()
	{
		Instance = this;
		if ((bool)friendshipBubble)
		{
			particleSystem = friendshipBubble.GetComponent<ParticleSystem>();
			audioSource = friendshipBubble.GetComponent<AudioSource>();
		}
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerJoinedRoom);
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void OnPlayerJoinedRoom(NetPlayer joiningPlayer)
	{
		if (!IsInParty)
		{
			return;
		}
		bool flag = NetworkSystem.Instance.CurrentRoom.MaxPlayers == NetworkSystem.Instance.RoomPlayerCount;
		Debug.Log("[FriendshipGroupDetection::OnPlayerJoinedRoom] JoiningPlayer: " + joiningPlayer.NickName + ", " + joiningPlayer.UserId + " " + $"| IsLocal: {joiningPlayer.IsLocal} | Room Full: {flag}");
		if (joiningPlayer.IsLocal)
		{
			lastJoinedRoomTime = Time.time;
			if (!flag)
			{
				Debug.Log("[FriendshipGroupDetection::OnPlayerJoinedRoom] Delaying PartyRefresh...");
				wantsPartyRefreshPostJoin = true;
				return;
			}
		}
		if (flag)
		{
			RefreshPartyMembers();
		}
	}

	public void AddGroupZoneCallback(Action<GroupJoinZoneAB> callback)
	{
		groupZoneCallbacks.Add(callback);
	}

	public void RemoveGroupZoneCallback(Action<GroupJoinZoneAB> callback)
	{
		groupZoneCallbacks.Remove(callback);
	}

	public bool IsInMyGroup(string userID)
	{
		if (myPartyMemberIDs != null)
		{
			return myPartyMemberIDs.Contains(userID);
		}
		return false;
	}

	public bool AnyPartyMembersOutsideFriendCollider()
	{
		if (!IsInParty)
		{
			return false;
		}
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			if (activeRigContainer.Rig.IsLocalPartyMember && !GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(activeRigContainer.Creator.UserId))
			{
				return true;
			}
		}
		return false;
	}

	public void Tick()
	{
		using (profiler_Tick.Auto())
		{
			if (wantsPartyRefreshPostJoin && lastJoinedRoomTime + joinedRoomRefreshPartyDelay < (double)Time.time)
			{
				RefreshPartyMembers();
			}
			if (wantsPartyRefreshPostFollowFailed && lastFailedToFollowPartyTime + failedToFollowRefreshPartyDelay < (double)Time.time)
			{
				RefreshPartyMembers();
			}
			List<int> list = playersInProvisionalGroup;
			List<int> list2 = playersInProvisionalGroup;
			List<int> list3 = tempIntList;
			tempIntList = list2;
			playersInProvisionalGroup = list3;
			UpdateProvisionalGroup(out var midpoint);
			if (playersInProvisionalGroup.Count > 0)
			{
				friendshipBubble.transform.position = midpoint;
			}
			bool flag = false;
			if (list.Count == playersInProvisionalGroup.Count)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] != playersInProvisionalGroup[i])
					{
						flag = true;
						break;
					}
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				groupCreateAfterTimestamp = Time.time + groupTime;
				amFirstProvisionalPlayer = playersInProvisionalGroup.Count > 0 && playersInProvisionalGroup[0] == NetworkSystem.Instance.LocalPlayer.ActorNumber;
				if (playersInProvisionalGroup.Count > 0 && !amFirstProvisionalPlayer)
				{
					List<int> list4 = tempIntList;
					list4.Clear();
					NetPlayer netPlayer = null;
					foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
					{
						VRRig rig = activeRigContainer.Rig;
						if (rig.creator.ActorNumber == playersInProvisionalGroup[0])
						{
							netPlayer = rig.creator;
							if (rig.IsLocalPartyMember)
							{
								list4.Clear();
								break;
							}
						}
						else if (rig.IsLocalPartyMember)
						{
							list4.Add(rig.creator.ActorNumber);
						}
					}
					if (list4.Count > 0)
					{
						photonView.RPC("NotifyPartyMerging", netPlayer.GetPlayerRef(), list4.ToArray());
					}
					else
					{
						photonView.RPC("NotifyNoPartyToMerge", netPlayer.GetPlayerRef());
					}
				}
				if (playersInProvisionalGroup.Count == 0)
				{
					if (Time.time > suppressPartyCreationUntilTimestamp && playEffectsAfterTimestamp == 0f)
					{
						audioSource.GTStop();
						audioSource.GTPlayOneShot(fistBumpInterruptedAudio);
					}
					particleSystem.Stop();
					playEffectsAfterTimestamp = 0f;
				}
				else
				{
					playEffectsAfterTimestamp = Time.time + playEffectsDelay;
				}
			}
			else if (playEffectsAfterTimestamp > 0f && Time.time > playEffectsAfterTimestamp)
			{
				audioSource.time = 0f;
				audioSource.GTPlay();
				particleSystem.Play();
				playEffectsAfterTimestamp = 0f;
			}
			else if (playersInProvisionalGroup.Count > 0 && Time.time > groupCreateAfterTimestamp && amFirstProvisionalPlayer)
			{
				List<int> list5 = tempIntList;
				list5.Clear();
				list5.AddRange(playersInProvisionalGroup);
				int num = 0;
				if (IsInParty)
				{
					foreach (RigContainer activeRigContainer2 in VRRigCache.ActiveRigContainers)
					{
						VRRig rig2 = activeRigContainer2.Rig;
						if (rig2.IsLocalPartyMember)
						{
							list5.Add(rig2.creator.ActorNumber);
							num++;
						}
					}
				}
				int num2 = 0;
				foreach (int item in playersInProvisionalGroup)
				{
					if (partyMergeIDs.TryGetValue(item, out var value))
					{
						list5.AddRange(value);
						num2++;
					}
				}
				list5.Sort();
				int[] memberIDs = list5.Distinct().ToArray();
				myBraceletColor = GTColor.RandomHSV(braceletRandomColorHSVRanges);
				SendPartyFormedRPC(PackColor(myBraceletColor), memberIDs, forceDebug: false);
				groupCreateAfterTimestamp = Time.time + cooldownAfterCreatingGroup;
			}
			if (myPartyMemberIDs != null)
			{
				UpdateWarningSigns();
			}
		}
	}

	private void UpdateProvisionalGroup(out Vector3 midpoint)
	{
		using (profiler_updateProvisionalGroup.Auto())
		{
			playersInProvisionalGroup.Clear();
			bool isLeftHand;
			VRMap makingFist = VRRig.LocalRig.GetMakingFist(debug, out isLeftHand);
			if (makingFist == null || !NetworkSystem.Instance.InRoom || VRRig.LocalRig.leftHandLink.IsLinkActive() || VRRig.LocalRig.rightHandLink.IsLinkActive() || VRRigCache.ActiveRigs.Count == 0 || Time.time < suppressPartyCreationUntilTimestamp || (GorillaGameModes.GameMode.ActiveGameMode != null && !GorillaGameModes.GameMode.ActiveGameMode.CanJoinFrienship(NetworkSystem.Instance.LocalPlayer)))
			{
				midpoint = Vector3.zero;
				return;
			}
			WillJoinLeftHanded = isLeftHand;
			playersToPropagateFrom.Clear();
			provisionalGroupUsingLeftHands.Clear();
			playersMakingFists.Clear();
			int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
			int num = -1;
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				VRRig rig = activeRigContainer.Rig;
				bool isLeftHand2;
				VRMap makingFist2 = rig.GetMakingFist(debug, out isLeftHand2);
				if (makingFist2 != null && !rig.leftHandLink.IsLinkActive() && !rig.rightHandLink.IsLinkActive() && (!GorillaGameModes.GameMode.ActiveGameMode.IsNotNull() || GorillaGameModes.GameMode.ActiveGameMode.CanJoinFrienship(rig.OwningNetPlayer)))
				{
					PlayerFist item = new PlayerFist
					{
						actorNumber = rig.creator.ActorNumber,
						position = makingFist2.rigTarget.position,
						isLeftHand = isLeftHand2
					};
					if (rig.isOfflineVRRig)
					{
						num = playersMakingFists.Count;
					}
					playersMakingFists.Add(item);
				}
			}
			if (playersMakingFists.Count <= 1 || num == -1)
			{
				midpoint = Vector3.zero;
				return;
			}
			playersToPropagateFrom.Enqueue(playersMakingFists[num]);
			playersInProvisionalGroup.Add(actorNumber);
			midpoint = makingFist.rigTarget.position;
			int num2 = 1 << num;
			PlayerFist result;
			while (playersToPropagateFrom.TryDequeue(out result))
			{
				for (int i = 0; i < playersMakingFists.Count; i++)
				{
					if ((num2 & (1 << i)) != 0)
					{
						continue;
					}
					PlayerFist item2 = playersMakingFists[i];
					if ((result.position - item2.position).IsShorterThan(detectionRadius))
					{
						int index = ~playersInProvisionalGroup.BinarySearch(item2.actorNumber);
						num2 |= 1 << i;
						playersInProvisionalGroup.Insert(index, item2.actorNumber);
						if (item2.isLeftHand)
						{
							provisionalGroupUsingLeftHands.Add(item2.actorNumber);
						}
						playersToPropagateFrom.Enqueue(item2);
						midpoint += item2.position;
					}
				}
			}
			if (playersInProvisionalGroup.Count == 1)
			{
				playersInProvisionalGroup.Clear();
			}
			if (playersInProvisionalGroup.Count > 0)
			{
				midpoint /= (float)playersInProvisionalGroup.Count;
			}
		}
	}

	private void UpdateWarningSigns()
	{
		_ = GorillaTagger.Instance.offlineVRRig.zoneEntity;
		_ = PhotonNetworkController.Instance.CurrentRoomZone;
		GroupJoinZoneAB groupJoinZoneAB = 0;
		if (myPartyMemberIDs != null)
		{
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				VRRig rig = activeRigContainer.Rig;
				if (rig.IsLocalPartyMember && !rig.isOfflineVRRig)
				{
					groupJoinZoneAB |= rig.zoneEntity.GroupZone;
				}
			}
		}
		if (!(groupJoinZoneAB != partyZone))
		{
			return;
		}
		debugStr.Clear();
		foreach (RigContainer activeRigContainer2 in VRRigCache.ActiveRigContainers)
		{
			VRRig rig2 = activeRigContainer2.Rig;
			if (rig2.IsLocalPartyMember && !rig2.isOfflineVRRig)
			{
				debugStr.Append($"{rig2.playerNameVisible} in {rig2.zoneEntity.GroupZone};");
			}
		}
		partyZone = groupJoinZoneAB;
		foreach (Action<GroupJoinZoneAB> groupZoneCallback in groupZoneCallbacks)
		{
			groupZoneCallback(partyZone);
		}
	}

	[PunRPC]
	private void NotifyNoPartyToMerge(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "NotifyNoPartyToMerge");
		if (info.Sender != null && partyMergeIDs != null)
		{
			partyMergeIDs.Remove(info.Sender.ActorNumber);
		}
	}

	[Rpc]
	private unsafe static void RPC_NotifyNoPartyToMerge(NetworkRunner runner, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int num = 8;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyNoPartyToMerge(Fusion.NetworkRunner,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyNoPartyToMerge(Fusion.NetworkRunner,Fusion.RpcInfo)"));
				int num2 = 8;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.partyMergeIDs.Remove(info.Source.PlayerId);
	}

	[PunRPC]
	private void NotifyPartyMerging(int[] memberIDs, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "NotifyPartyMerging");
		if (memberIDs != null && memberIDs.Length <= 10)
		{
			partyMergeIDs[info.Sender.ActorNumber] = memberIDs;
		}
	}

	[Rpc]
	private unsafe static void RPC_NotifyPartyMerging(NetworkRunner runner, [RpcTarget] PlayerRef playerRef, int[] memberIDs, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(playerRef);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(playerRef, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyMerging(Fusion.NetworkRunner,Fusion.PlayerRef,System.Int32[],Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				num += (memberIDs.Length * 4 + 4 + 3) & -4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyMerging(Fusion.NetworkRunner,Fusion.PlayerRef,System.Int32[],Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyMerging(Fusion.NetworkRunner,Fusion.PlayerRef,System.Int32[],Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = memberIDs.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, memberIDs) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(playerRef);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		if (memberIDs.Length <= 10)
		{
			Instance.partyMergeIDs[info.Source.PlayerId] = memberIDs;
		}
	}

	public void SendAboutToGroupJoin()
	{
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			Debug.Log("Sending group join to " + VRRigCache.ActiveRigContainers.Count + " players. Party member:" + rig.OwningNetPlayer.NickName + "Is offline rig" + rig.isOfflineVRRig);
			if (rig.IsLocalPartyMember && !rig.isOfflineVRRig)
			{
				photonView.RPC("PartyMemberIsAboutToGroupJoin", rig.Creator.GetPlayerRef());
			}
		}
	}

	[PunRPC]
	private void PartyMemberIsAboutToGroupJoin(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PartyMemberIsAboutToGroupJoin");
		PartMemberIsAboutToGroupJoinWrapped(new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	private unsafe static void RPC_PartyMemberIsAboutToGroupJoin(NetworkRunner runner, [RpcTarget] PlayerRef targetPlayer, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(targetPlayer);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(targetPlayer, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyMemberIsAboutToGroupJoin(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyMemberIsAboutToGroupJoin(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyMemberIsAboutToGroupJoin(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)"));
				int num2 = 8;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(targetPlayer);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.PartMemberIsAboutToGroupJoinWrapped(new PhotonMessageInfoWrapped(info));
	}

	private void PartMemberIsAboutToGroupJoinWrapped(PhotonMessageInfoWrapped wrappedInfo)
	{
		_ = Time.time;
		_ = aboutToGroupJoin_CooldownUntilTimestamp;
		if (wrappedInfo.senderID < NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			aboutToGroupJoin_CooldownUntilTimestamp = Time.time + 5f;
			if (myPartyMembersHash.Contains(wrappedInfo.Sender.UserId))
			{
				PhotonNetworkController.Instance.DeferJoining(2f);
			}
		}
	}

	private void SendPartyFormedRPC(short braceletColor, int[] memberIDs, bool forceDebug)
	{
		string text = Enum.Parse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true).ToString();
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (playersInProvisionalGroup.BinarySearch(activeRig.creator.ActorNumber) >= 0)
			{
				photonView.RPC("PartyFormedSuccessfully", activeRig.Creator.GetPlayerRef(), text, braceletColor, memberIDs, forceDebug);
			}
		}
	}

	[Rpc]
	private unsafe static void RPC_PartyFormedSuccessfully(NetworkRunner runner, [RpcTarget] PlayerRef targetPlayer, string partyGameMode, short braceletColor, int[] memberIDs, bool forceDebug, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(targetPlayer);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(targetPlayer, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyFormedSuccessfully(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],System.Boolean,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(partyGameMode) + 3) & -4;
				num += 4;
				num += (memberIDs.Length * 4 + 4 + 3) & -4;
				num += 4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyFormedSuccessfully(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],System.Boolean,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyFormedSuccessfully(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],System.Boolean,Fusion.RpcInfo)"));
				int num2 = 8;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, partyGameMode) + 3) & -4) + num2;
				*(short*)(ptr2 + num2) = braceletColor;
				num2 += 5 & -4;
				*(int*)(ptr2 + num2) = memberIDs.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, memberIDs) + 3) & -4) + num2;
				ReadWriteUtilsForWeaver.WriteBoolean((int*)(ptr2 + num2), forceDebug);
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(targetPlayer);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(info, "PartyFormedSuccessfully");
		Instance.PartyFormedSuccesfullyWrapped(partyGameMode, braceletColor, memberIDs, forceDebug, new PhotonMessageInfoWrapped(info));
	}

	[PunRPC]
	private void PartyFormedSuccessfully(string partyGameMode, short braceletColor, int[] memberIDs, bool forceDebug, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PartyFormedSuccessfully");
		PartyFormedSuccesfullyWrapped(partyGameMode, braceletColor, memberIDs, forceDebug, new PhotonMessageInfoWrapped(info));
	}

	private void PartyFormedSuccesfullyWrapped(string partyGameMode, short braceletColor, int[] memberIDs, bool forceDebug, PhotonMessageInfoWrapped info)
	{
		if (memberIDs == null || memberIDs.Length > 10 || !Enumerable.Contains(memberIDs, info.Sender.ActorNumber) || playersInProvisionalGroup.IndexOf(info.Sender.ActorNumber) != 0 || Mathf.Abs(groupCreateAfterTimestamp - Time.time) > m_maxGroupJoinTimeDifference || !GorillaGameModes.GameMode.IsValidGameMode(partyGameMode))
		{
			return;
		}
		if (IsInParty)
		{
			string text = Enum.Parse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true).ToString();
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				VRRig rig = activeRigContainer.Rig;
				if (rig.IsLocalPartyMember && !rig.isOfflineVRRig)
				{
					photonView.RPC("AddPartyMembers", rig.Creator.GetPlayerRef(), text, braceletColor, memberIDs);
				}
			}
		}
		suppressPartyCreationUntilTimestamp = Time.time + cooldownAfterCreatingGroup;
		DidJoinLeftHanded = WillJoinLeftHanded;
		SetNewParty(partyGameMode, braceletColor, memberIDs);
	}

	[PunRPC]
	private void AddPartyMembers(string partyGameMode, short braceletColor, int[] memberIDs, PhotonMessageInfo info)
	{
		AddPartyMembersWrapped(partyGameMode, braceletColor, memberIDs, new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	private unsafe static void RPC_AddPartyMembers(NetworkRunner runner, [RpcTarget] PlayerRef rpcTarget, string partyGameMode, short braceletColor, int[] memberIDs, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(rpcTarget);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(rpcTarget, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_AddPartyMembers(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(partyGameMode) + 3) & -4;
				num += 4;
				num += (memberIDs.Length * 4 + 4 + 3) & -4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_AddPartyMembers(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_AddPartyMembers(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],Fusion.RpcInfo)"));
				int num2 = 8;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, partyGameMode) + 3) & -4) + num2;
				*(short*)(ptr2 + num2) = braceletColor;
				num2 += 5 & -4;
				*(int*)(ptr2 + num2) = memberIDs.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, memberIDs) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(rpcTarget);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.AddPartyMembersWrapped(partyGameMode, braceletColor, memberIDs, new PhotonMessageInfoWrapped(info));
	}

	private void AddPartyMembersWrapped(string partyGameMode, short braceletColor, int[] memberIDs, PhotonMessageInfoWrapped infoWrapped)
	{
		MonkeAgent.IncrementRPCCall(infoWrapped, "AddPartyMembersWrapped");
		if (IsInParty && memberIDs != null && memberIDs.Length <= 10 && myPartyMembersHash.Contains(NetworkSystem.Instance.GetUserID(infoWrapped.senderID)) && GorillaGameModes.GameMode.IsValidGameMode(partyGameMode))
		{
			SetNewParty(partyGameMode, braceletColor, memberIDs);
		}
	}

	private void SetNewParty(string partyGameMode, short braceletColor, int[] memberIDs)
	{
		GorillaComputer.instance.SetGameModeWithoutButton(partyGameMode);
		myPartyMemberIDs = new List<string>();
		userIdLookup.Clear();
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			userIdLookup.Add(activeRigContainer.Creator.ActorNumber, activeRigContainer.Creator.UserId);
		}
		foreach (int key in memberIDs)
		{
			if (userIdLookup.TryGetValue(key, out var value))
			{
				myPartyMemberIDs.Add(value);
			}
		}
		myBraceletColor = UnpackColor(braceletColor);
		GorillaTagger.Instance.StartVibration(DidJoinLeftHanded, hapticStrength, hapticDuration);
		OnPartyMembershipChanged();
		PlayerGameEvents.MiscEvent("FriendshipGroupJoined");
	}

	public void LeaveParty()
	{
		if (myPartyMemberIDs == null)
		{
			return;
		}
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.IsLocalPartyMember && !rig.isOfflineVRRig)
			{
				photonView.RPC("PlayerLeftParty", rig.Creator.GetPlayerRef());
			}
		}
		myPartyMemberIDs = null;
		OnPartyMembershipChanged();
		PhotonNetworkController.Instance.ClearDeferredJoin();
		GorillaTagger.Instance.StartVibration(forLeftController: false, hapticStrength, hapticDuration);
	}

	public void OnFailedToFollowParty()
	{
		if (IsInParty)
		{
			lastFailedToFollowPartyTime = Time.time;
			wantsPartyRefreshPostFollowFailed = true;
		}
	}

	public void RefreshPartyMembers()
	{
		if (myPartyMemberIDs.IsNullOrEmpty())
		{
			return;
		}
		Debug.Log("[FriendshipGroupDetection::RefreshPartyMembers] refreshing...");
		List<string> list = new List<string>(myPartyMemberIDs);
		Debug.Log("[FriendshipGroupDetection::RefreshPartyMembers] found " + $"{NetworkSystem.Instance.AllNetPlayers.Length} current players in Room...");
		for (int i = 0; i < NetworkSystem.Instance.AllNetPlayers.Length; i++)
		{
			if (NetworkSystem.Instance.AllNetPlayers[i] != null)
			{
				list.Remove(NetworkSystem.Instance.AllNetPlayers[i].UserId);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Debug.Log("[FriendshipGroupDetection::RefreshPartyMembers] removing missing player " + list[j] + " from party...");
			PlayerIDLeftParty(list[j]);
		}
		wantsPartyRefreshPostJoin = false;
		wantsPartyRefreshPostFollowFailed = false;
	}

	[Rpc]
	private unsafe static void RPC_PlayerLeftParty(NetworkRunner runner, [RpcTarget] PlayerRef player, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(player);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(player, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PlayerLeftParty(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PlayerLeftParty(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PlayerLeftParty(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)"));
				int num2 = 8;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(player);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(info, "PlayerLeftParty");
		Instance.PlayerLeftPartyWrapped(new PhotonMessageInfoWrapped(info));
	}

	[PunRPC]
	private void PlayerLeftParty(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayerLeftParty");
		PlayerLeftPartyWrapped(new PhotonMessageInfoWrapped(info));
	}

	private void PlayerLeftPartyWrapped(PhotonMessageInfoWrapped infoWrapped)
	{
		if (myPartyMemberIDs != null && myPartyMemberIDs.Remove(infoWrapped.Sender.UserId))
		{
			if (myPartyMemberIDs.Count <= 1)
			{
				myPartyMemberIDs = null;
			}
			OnPartyMembershipChanged();
			GorillaTagger.Instance.StartVibration(DidJoinLeftHanded, hapticStrength, hapticDuration);
		}
	}

	private void PlayerIDLeftParty(string userID)
	{
		if (myPartyMemberIDs != null && myPartyMemberIDs.Remove(userID))
		{
			if (myPartyMemberIDs.Count <= 1)
			{
				myPartyMemberIDs = null;
			}
			OnPartyMembershipChanged();
			GorillaTagger.Instance.StartVibration(DidJoinLeftHanded, hapticStrength, hapticDuration);
		}
	}

	public void SendVerifyPartyMember(NetPlayer player)
	{
		photonView.RPC("VerifyPartyMember", player.GetPlayerRef());
	}

	[PunRPC]
	private void VerifyPartyMember(PhotonMessageInfo info)
	{
		VerifyPartyMemberWrapped(new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	private unsafe static void RPC_VerifyPartyMember(NetworkRunner runner, [RpcTarget] PlayerRef rpcTarget, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(rpcTarget);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(rpcTarget, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_VerifyPartyMember(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_VerifyPartyMember(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_VerifyPartyMember(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)"));
				int num2 = 8;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(rpcTarget);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.VerifyPartyMemberWrapped(new PhotonMessageInfoWrapped(info));
	}

	private void VerifyPartyMemberWrapped(PhotonMessageInfoWrapped infoWrapped)
	{
		MonkeAgent.IncrementRPCCall(infoWrapped, "VerifyPartyMemberWrapped");
		if (VRRigCache.Instance.TryGetVrrig(infoWrapped.Sender, out var playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 15, infoWrapped.SentServerTime) && (myPartyMemberIDs == null || !myPartyMemberIDs.Contains(NetworkSystem.Instance.GetUserID(infoWrapped.senderID))))
		{
			photonView.RPC("PlayerLeftParty", infoWrapped.Sender.GetPlayerRef());
		}
	}

	public void SendRequestPartyGameMode(string gameMode)
	{
		int num = int.MaxValue;
		NetPlayer netPlayer = null;
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.IsLocalPartyMember && rig.creator.ActorNumber < num)
			{
				netPlayer = rig.creator;
				num = rig.creator.ActorNumber;
			}
		}
		if (netPlayer != null)
		{
			photonView.RPC("RequestPartyGameMode", netPlayer.GetPlayerRef(), gameMode);
		}
	}

	[Rpc]
	private unsafe static void RPC_RequestPartyGameMode(NetworkRunner runner, [RpcTarget] PlayerRef targetPlayer, string gameMode, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(targetPlayer);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(targetPlayer, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_RequestPartyGameMode(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(gameMode) + 3) & -4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_RequestPartyGameMode(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_RequestPartyGameMode(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)"));
				int num2 = 8;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, gameMode) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(targetPlayer);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.RequestPartyGameModeWrapped(gameMode, new PhotonMessageInfoWrapped(info));
	}

	[PunRPC]
	private void RequestPartyGameMode(string gameMode, PhotonMessageInfo info)
	{
		RequestPartyGameModeWrapped(gameMode, new PhotonMessageInfoWrapped(info));
	}

	private void RequestPartyGameModeWrapped(string gameMode, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestPartyGameModeWrapped");
		if (!IsInParty || !IsInMyGroup(info.Sender.UserId) || !GorillaGameModes.GameMode.IsValidGameMode(gameMode))
		{
			return;
		}
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.IsLocalPartyMember)
			{
				photonView.RPC("NotifyPartyGameModeChanged", rig.creator.GetPlayerRef(), gameMode);
			}
		}
	}

	[Rpc]
	private unsafe static void RPC_NotifyPartyGameModeChanged(NetworkRunner runner, [RpcTarget] PlayerRef targetPlayer, string gameMode, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			RpcTargetStatus rpcTargetStatus = runner.GetRpcTargetStatus(targetPlayer);
			if (rpcTargetStatus == RpcTargetStatus.Unreachable)
			{
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(targetPlayer, "System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyGameModeChanged(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)");
				return;
			}
			if (rpcTargetStatus != RpcTargetStatus.Self)
			{
				int num = 8;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(gameMode) + 3) & -4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyGameModeChanged(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyGameModeChanged(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)"));
				int num2 = 8;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, gameMode) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(targetPlayer);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Instance.NotifyPartyGameModeChangedWrapped(gameMode, new PhotonMessageInfoWrapped(info));
	}

	[PunRPC]
	private void NotifyPartyGameModeChanged(string gameMode, PhotonMessageInfo info)
	{
		NotifyPartyGameModeChangedWrapped(gameMode, new PhotonMessageInfoWrapped(info));
	}

	private void NotifyPartyGameModeChangedWrapped(string gameMode, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "NotifyPartyGameModeChangedWrapped");
		if (IsInParty && IsInMyGroup(info.Sender.UserId) && GorillaGameModes.GameMode.IsValidGameMode(gameMode))
		{
			GorillaComputer.instance.SetGameModeWithoutButton(gameMode);
		}
	}

	private void OnPartyMembershipChanged()
	{
		myPartyMembersHash.Clear();
		if (myPartyMemberIDs != null)
		{
			foreach (string myPartyMemberID in myPartyMemberIDs)
			{
				myPartyMembersHash.Add(myPartyMemberID);
			}
		}
		myBeadColors.Clear();
		tempColorLookup.Clear();
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			rig.ClearPartyMemberStatus();
			if (rig.IsLocalPartyMember)
			{
				tempColorLookup.Add(rig.Creator.UserId, rig.playerColor);
			}
		}
		MyBraceletSelfIndex = 0;
		if (myPartyMemberIDs != null)
		{
			foreach (string myPartyMemberID2 in myPartyMemberIDs)
			{
				if (tempColorLookup.TryGetValue(myPartyMemberID2, out var value))
				{
					if (myPartyMemberID2 == PhotonNetwork.LocalPlayer.UserId)
					{
						MyBraceletSelfIndex = myBeadColors.Count;
					}
					myBeadColors.Add(value);
				}
			}
		}
		else
		{
			GorillaComputer.instance.SetGameModeWithoutButton(GorillaComputer.instance.lastPressedGameMode);
			wantsPartyRefreshPostJoin = false;
			wantsPartyRefreshPostFollowFailed = false;
		}
		myBeadColors.Add(myBraceletColor);
		GorillaTagger.Instance.offlineVRRig.UpdateFriendshipBracelet();
		UpdateWarningSigns();
	}

	public bool IsPartyWithinCollider(GorillaFriendCollider friendCollider)
	{
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			if (rig.IsLocalPartyMember && !rig.isOfflineVRRig && !friendCollider.playerIDsCurrentlyTouching.Contains(rig.Creator.UserId))
			{
				return false;
			}
		}
		return true;
	}

	public static short PackColor(Color col)
	{
		return (short)(Mathf.RoundToInt(col.r * 9f) + Mathf.RoundToInt(col.g * 9f) * 10 + Mathf.RoundToInt(col.b * 9f) * 100);
	}

	public static Color UnpackColor(short data)
	{
		return new Color
		{
			r = (float)(data % 10) / 9f,
			g = (float)(data / 10 % 10) / 9f,
			b = (float)(data / 100 % 10) / 9f
		};
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyNoPartyToMerge(Fusion.NetworkRunner,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_NotifyNoPartyToMerge_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_NotifyNoPartyToMerge(runner, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyMerging(Fusion.NetworkRunner,Fusion.PlayerRef,System.Int32[],Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_NotifyPartyMerging_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		int[] array = new int[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array, ptr + num) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_NotifyPartyMerging(runner, target, array, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyMemberIsAboutToGroupJoin(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PartyMemberIsAboutToGroupJoin_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_PartyMemberIsAboutToGroupJoin(runner, target, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PartyFormedSuccessfully(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],System.Boolean,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PartyFormedSuccessfully_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result) + 3) & -4) + num;
		short num2 = *(short*)(ptr + num);
		num += 5 & -4;
		short braceletColor = num2;
		int[] array = new int[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array, ptr + num) + 3) & -4) + num;
		bool num3 = ReadWriteUtilsForWeaver.ReadBoolean((int*)(ptr + num));
		num += 4;
		bool forceDebug = num3;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_PartyFormedSuccessfully(runner, target, result, braceletColor, array, forceDebug, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_AddPartyMembers(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,System.Int16,System.Int32[],Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_AddPartyMembers_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result) + 3) & -4) + num;
		short num2 = *(short*)(ptr + num);
		num += 5 & -4;
		short braceletColor = num2;
		int[] array = new int[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array, ptr + num) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_AddPartyMembers(runner, target, result, braceletColor, array, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_PlayerLeftParty(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PlayerLeftParty_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_PlayerLeftParty(runner, target, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_VerifyPartyMember(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_VerifyPartyMember_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_VerifyPartyMember(runner, target, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_RequestPartyGameMode(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_RequestPartyGameMode_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_RequestPartyGameMode(runner, target, result, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTagScripts.FriendshipGroupDetection::RPC_NotifyPartyGameModeChanged(Fusion.NetworkRunner,Fusion.PlayerRef,System.String,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_NotifyPartyGameModeChanged_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_NotifyPartyGameModeChanged(runner, target, result, info);
	}
}
