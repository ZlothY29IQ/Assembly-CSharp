using System;
using System.Collections;
using GorillaLocomotion;
using Oculus.Platform;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class GorillaMetaReport : MonoBehaviour
{
	[SerializeField]
	private GameObject occluder;

	[SerializeField]
	private GameObject reportScoreboard;

	[SerializeField]
	private GameObject ReportText;

	[SerializeField]
	private LayerMask visibleLayers;

	[SerializeField]
	private GorillaReportButton closeButton;

	[SerializeField]
	private GameObject leftHandObject;

	[SerializeField]
	private GameObject rightHandObject;

	[SerializeField]
	private Vector3 playerLocalScreenPosition;

	private float blockButtonsUntilTimestamp;

	[SerializeField]
	private GorillaScoreBoard currentScoreboard;

	private int savedCullingLayers;

	private bool hasSavedCullingMask;

	public bool testPress;

	public bool isMoving;

	private float movementTime;

	private GTPlayer localPlayer => GTPlayer.Instance;

	private void Start()
	{
		localPlayer.inOverlay = false;
		MothershipClientApiUnity.OnMessageNotificationSocket += new Action<NotificationsMessageResponse, IntPtr>(OnNotification);
		base.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			localPlayer.inOverlay = false;
			StopAllCoroutines();
		}
	}

	private void OnReportButtonIntentNotif(Message<string> message)
	{
		if (message.IsError)
		{
			AbuseReport.ReportRequestHandled(ReportRequestResponse.Unhandled);
		}
		else if (!PhotonNetwork.InRoom)
		{
			ReportText.SetActive(value: true);
			AbuseReport.ReportRequestHandled(ReportRequestResponse.Handled);
			StartOverlay();
		}
		else if (!message.IsError)
		{
			AbuseReport.ReportRequestHandled(ReportRequestResponse.Handled);
			StartOverlay();
		}
	}

	private void OnNotification(NotificationsMessageResponse notification, nint _)
	{
		switch (notification.Title)
		{
		case "Warning":
			OnWarning(notification.Body);
			GorillaTelemetry.PostNotificationEvent("Warning");
			break;
		case "Mute":
			OnMuteSanction(notification.Body);
			GorillaTelemetry.PostNotificationEvent("Mute");
			break;
		case "Unmute":
			if (GorillaTagger.hasInstance)
			{
				GorillaTagger.moderationMutedTime = -1f;
			}
			GorillaTelemetry.PostNotificationEvent("Unmute");
			break;
		}
	}

	private void OnWarning(string warningNotification)
	{
		string[] array = warningNotification.Split('|');
		if (array.Length != 2)
		{
			Debug.LogError("Invalid warning notification");
			return;
		}
		string text = array[0];
		string[] list = array[1].Split(',');
		if (list.Length == 0)
		{
			Debug.LogError("Missing warning notification reasons");
			return;
		}
		string text2 = FormatListToString(in list);
		ReportText.GetComponent<Text>().text = text.ToUpper() + " WARNING FOR " + text2.ToUpper();
		StartOverlay(isSanction: true);
	}

	private void OnMuteSanction(string muteNotification)
	{
		string[] array = muteNotification.Split('|');
		if (array.Length != 3)
		{
			Debug.LogError("Invalid mute notification");
		}
		else
		{
			if (!array[0].Equals("voice", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			if (array[2].Length > 0 && int.TryParse(array[2], out var result))
			{
				int num = result / 60;
				ReportText.GetComponent<Text>().text = $"MUTED FOR {num} MINUTES\nBAD MONKE";
				if (GorillaTagger.hasInstance)
				{
					GorillaTagger.moderationMutedTime = result;
				}
			}
			else
			{
				ReportText.GetComponent<Text>().text = "MUTED FOREVER";
				if (GorillaTagger.hasInstance)
				{
					GorillaTagger.moderationMutedTime = float.PositiveInfinity;
				}
			}
			StartOverlay(isSanction: true);
		}
	}

	private static string FormatListToString(in string[] list)
	{
		return list.Length switch
		{
			1 => list[0], 
			2 => list[0] + " AND " + list[1], 
			_ => list[..^1].Join(", ") + ", AND " + list[^1], 
		};
	}

	private IEnumerator Submitted()
	{
		yield return new WaitForSeconds(1.5f);
		Teardown();
	}

	private void DuplicateScoreboard()
	{
		currentScoreboard.gameObject.SetActive(value: true);
		if (GorillaScoreboardTotalUpdater.instance != null)
		{
			GorillaScoreboardTotalUpdater.instance.UpdateScoreboard(currentScoreboard);
		}
		GetIdealScreenPositionRotation(out var position, out var rotation, out var _);
		currentScoreboard.transform.SetPositionAndRotation(position, rotation);
		reportScoreboard.transform.SetPositionAndRotation(position, rotation);
	}

	private void ToggleLevelVisibility(bool state)
	{
		Camera component = GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
		if (state)
		{
			if (hasSavedCullingMask)
			{
				component.cullingMask = savedCullingLayers;
				hasSavedCullingMask = false;
			}
			return;
		}
		if (!hasSavedCullingMask)
		{
			savedCullingLayers = component.cullingMask;
			hasSavedCullingMask = true;
		}
		component.cullingMask = visibleLayers;
	}

	private void Teardown()
	{
		ReportText.GetComponent<Text>().text = "NOT CURRENTLY CONNECTED TO A ROOM";
		ReportText.SetActive(value: false);
		localPlayer.inOverlay = false;
		localPlayer.disableMovement = false;
		closeButton.selected = false;
		closeButton.isOn = false;
		closeButton.UpdateColor();
		localPlayer.InReportMenu = false;
		ToggleLevelVisibility(state: true);
		base.gameObject.SetActive(value: false);
		foreach (GorillaPlayerScoreboardLine line in currentScoreboard.lines)
		{
			line.doneReporting = false;
		}
		GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();
	}

	private void CheckReportSubmit()
	{
		if (currentScoreboard == null)
		{
			return;
		}
		foreach (GorillaPlayerScoreboardLine line in currentScoreboard.lines)
		{
			if (line.doneReporting)
			{
				ReportText.SetActive(value: true);
				ReportText.GetComponent<Text>().text = "REPORTED " + line.playerNameVisible;
				currentScoreboard.gameObject.SetActive(value: false);
				StartCoroutine(Submitted());
			}
		}
	}

	private void GetIdealScreenPositionRotation(out Vector3 position, out Quaternion rotation, out Vector3 scale)
	{
		GameObject mainCamera = GorillaTagger.Instance.mainCamera;
		rotation = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f);
		scale = localPlayer.turnParent.transform.localScale;
		position = mainCamera.transform.position + rotation * playerLocalScreenPosition * scale.x;
	}

	private void StartOverlay(bool isSanction = false)
	{
		if (localPlayer.InReportMenu)
		{
			return;
		}
		GetIdealScreenPositionRotation(out var position, out var rotation, out var scale);
		currentScoreboard.transform.localScale = scale * 2f;
		reportScoreboard.transform.localScale = scale;
		leftHandObject.transform.localScale = scale;
		rightHandObject.transform.localScale = scale;
		occluder.transform.localScale = scale;
		if (PhotonNetwork.InRoom)
		{
			localPlayer.InReportMenu = true;
			localPlayer.disableMovement = true;
			localPlayer.inOverlay = true;
			base.gameObject.SetActive(value: true);
			if (PhotonNetwork.InRoom && !isSanction)
			{
				DuplicateScoreboard();
			}
			else
			{
				ReportText.SetActive(value: true);
				reportScoreboard.transform.SetPositionAndRotation(position, rotation);
				currentScoreboard.transform.SetPositionAndRotation(position, rotation);
			}
			ToggleLevelVisibility(state: false);
			Transform controllerTransform = localPlayer.GetControllerTransform(isLeftHand: true);
			Transform controllerTransform2 = localPlayer.GetControllerTransform(isLeftHand: false);
			rightHandObject.transform.SetPositionAndRotation(controllerTransform2.position, controllerTransform2.rotation);
			leftHandObject.transform.SetPositionAndRotation(controllerTransform.position, controllerTransform.rotation);
			if (isSanction)
			{
				currentScoreboard.gameObject.SetActive(value: false);
			}
			else
			{
				currentScoreboard.gameObject.SetActive(value: true);
			}
		}
	}

	private void CheckDistance()
	{
		GetIdealScreenPositionRotation(out var position, out var rotation, out var _);
		float num = Vector3.Distance(reportScoreboard.transform.position, position);
		float num2 = 1f;
		if (num > num2 && !isMoving)
		{
			isMoving = true;
			movementTime = 0f;
		}
		if (isMoving)
		{
			movementTime += Time.deltaTime;
			float num3 = movementTime;
			reportScoreboard.transform.SetPositionAndRotation(Vector3.Lerp(reportScoreboard.transform.position, position, num3), Quaternion.Lerp(reportScoreboard.transform.rotation, rotation, num3));
			if (currentScoreboard != null)
			{
				currentScoreboard.transform.SetPositionAndRotation(Vector3.Lerp(currentScoreboard.transform.position, position, num3), Quaternion.Lerp(currentScoreboard.transform.rotation, rotation, num3));
			}
			if (num3 >= 1f)
			{
				isMoving = false;
				movementTime = 0f;
			}
		}
	}

	private void Update()
	{
		if (!(blockButtonsUntilTimestamp > Time.time))
		{
			if (SteamVR_Actions.gorillaTag_System.GetState(SteamVR_Input_Sources.LeftHand) && localPlayer.InReportMenu)
			{
				Teardown();
				blockButtonsUntilTimestamp = Time.time + 0.75f;
			}
			if (localPlayer.InReportMenu)
			{
				localPlayer.inOverlay = true;
				occluder.transform.position = GorillaTagger.Instance.mainCamera.transform.position;
				Transform controllerTransform = localPlayer.GetControllerTransform(isLeftHand: true);
				Transform controllerTransform2 = localPlayer.GetControllerTransform(isLeftHand: false);
				rightHandObject.transform.SetPositionAndRotation(controllerTransform2.position, controllerTransform2.rotation);
				leftHandObject.transform.SetPositionAndRotation(controllerTransform.position, controllerTransform.rotation);
				CheckDistance();
				CheckReportSubmit();
			}
			if (closeButton.selected)
			{
				Teardown();
			}
			if (testPress)
			{
				testPress = false;
				StartOverlay();
			}
		}
	}
}
