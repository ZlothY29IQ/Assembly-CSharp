using System;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class JoinTriggerUI : MonoBehaviour
{
	[SerializeField]
	private XSceneRef joinTriggerRef;

	private GorillaNetworkJoinTrigger joinTrigger;

	private bool joinTriggerResolved;

	[SerializeField]
	private XSceneRef friendColliderRef;

	private GorillaFriendCollider friendCollider;

	private bool friendColliderResolved;

	[SerializeField]
	private MeshRenderer milestoneRenderer;

	[SerializeField]
	private MeshRenderer screenBGRenderer;

	[SerializeField]
	private TextMeshPro screenText;

	[SerializeField]
	private JoinTriggerUITemplate template;

	private new bool didStart;

	public bool HasFriendCollider => friendColliderResolved;

	public GorillaFriendCollider FriendJoinCollider => friendCollider;

	private void Awake()
	{
		friendColliderResolved = friendColliderRef.TryResolve(out friendCollider) && friendCollider != null;
		joinTriggerResolved = joinTriggerRef.TryResolve(out joinTrigger) && joinTrigger != null;
	}

	private void Start()
	{
		didStart = true;
		OnEnable();
	}

	private void OnEnable()
	{
		if (didStart && IsValid())
		{
			joinTrigger.RegisterUI(this);
			if (friendColliderResolved)
			{
				friendCollider.RegisterUI(this);
			}
		}
	}

	private void OnDisable()
	{
		if (IsValid())
		{
			joinTrigger.UnregisterUI(this);
			if (friendColliderResolved)
			{
				friendCollider.UnregisterUI();
			}
		}
	}

	public void TriggerUpdateUI()
	{
		if (IsValid())
		{
			joinTrigger.UpdateUI();
		}
	}

	public void SetState(JoinTriggerVisualState state, Func<string> oldZone, Func<string> newZone, Func<string> oldGameMode, Func<string> newGameMode)
	{
		switch (state)
		{
		case JoinTriggerVisualState.ConnectionError:
			milestoneRenderer.sharedMaterial = template.Milestone_Error;
			screenBGRenderer.sharedMaterial = template.ScreenBG_Error;
			screenText.text = (template.showFullErrorMessages ? GorillaScoreboardTotalUpdater.instance.offlineTextErrorString : template.ScreenText_Error);
			break;
		case JoinTriggerVisualState.AlreadyInRoom:
			milestoneRenderer.sharedMaterial = template.Milestone_AlreadyInRoom;
			screenBGRenderer.sharedMaterial = template.ScreenBG_AlreadyInRoom;
			screenText.text = template.ScreenText_AlreadyInRoom.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.InPrivateRoom:
			milestoneRenderer.sharedMaterial = template.Milestone_InPrivateRoom;
			screenBGRenderer.sharedMaterial = template.ScreenBG_InPrivateRoom;
			screenText.text = template.ScreenText_InPrivateRoom.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.LeaveRoomAndPartyJoin:
			milestoneRenderer.sharedMaterial = template.Milestone_LeaveRoomAndGroupJoin;
			screenBGRenderer.sharedMaterial = template.ScreenBG_LeaveRoomAndGroupJoin;
			screenText.text = template.ScreenText_LeaveRoomAndGroupJoin.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.AbandonPartyAndSoloJoin:
			milestoneRenderer.sharedMaterial = template.Milestone_AbandonPartyAndSoloJoin;
			screenBGRenderer.sharedMaterial = template.ScreenBG_AbandonPartyAndSoloJoin;
			screenText.text = template.ScreenText_AbandonPartyAndSoloJoin.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.LeaveRoomAndSoloJoin:
			milestoneRenderer.sharedMaterial = template.Milestone_LeaveRoomAndSoloJoin;
			screenBGRenderer.sharedMaterial = template.ScreenBG_LeaveRoomAndSoloJoin;
			screenText.text = template.ScreenText_LeaveRoomAndSoloJoin.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.NotConnectedSoloJoin:
			milestoneRenderer.sharedMaterial = template.Milestone_NotConnectedSoloJoin;
			screenBGRenderer.sharedMaterial = template.ScreenBG_NotConnectedSoloJoin;
			screenText.text = template.ScreenText_NotConnectedSoloJoin.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		case JoinTriggerVisualState.ChangingGameModeSoloJoin:
			milestoneRenderer.sharedMaterial = template.Milestone_ChangingGameModeSoloJoin;
			screenBGRenderer.sharedMaterial = template.ScreenBG_ChangingGameModeSoloJoin;
			screenText.text = template.ScreenText_ChangingGameModeSoloJoin.GetText(oldZone, newZone, oldGameMode, newGameMode);
			break;
		}
	}

	private bool IsValid()
	{
		_ = joinTriggerResolved;
		return joinTriggerResolved;
	}
}
