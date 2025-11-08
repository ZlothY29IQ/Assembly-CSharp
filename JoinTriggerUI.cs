using System;
using GorillaExtensions;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class JoinTriggerUI : MonoBehaviour
{
	[SerializeField]
	private XSceneRef joinTriggerRef;

	private GorillaNetworkJoinTrigger joinTrigger;

	private bool joinTrigger_isRefResolved;

	[SerializeField]
	private MeshRenderer milestoneRenderer;

	[SerializeField]
	private MeshRenderer screenBGRenderer;

	[SerializeField]
	private TextMeshPro screenText;

	[SerializeField]
	private JoinTriggerUITemplate template;

	private new bool didStart;

	private void Awake()
	{
		joinTrigger_isRefResolved = joinTriggerRef.TryResolve(out joinTrigger) && joinTrigger != null;
	}

	private void Start()
	{
		didStart = true;
		OnEnable();
	}

	private void OnEnable()
	{
		if (didStart && _IsValid())
		{
			joinTrigger.RegisterUI(this);
		}
	}

	private void OnDisable()
	{
		if (_IsValid())
		{
			joinTrigger.UnregisterUI(this);
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

	private bool _IsValid()
	{
		if (!joinTrigger_isRefResolved)
		{
			if (joinTriggerRef.TargetID == 0)
			{
				Debug.LogError("ERROR!!!  JoinTriggerUI: XSceneRef `joinTriggerRef` is not assigned so could not resolve. Path=" + base.transform.GetPathQ(), this);
			}
			else
			{
				Debug.LogError("ERROR!!!  JoinTriggerUI: XSceneRef `joinTriggerRef` could not be resolved. Path=" + base.transform.GetPathQ(), this);
			}
		}
		return joinTrigger_isRefResolved;
	}
}
