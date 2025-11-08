using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRToolPurchaseStation : MonoBehaviour
{
	[Serializable]
	public struct ToolEntry
	{
		public Transform displayToolParent;

		public GameEntity entityPrefab;

		public string toolName;

		public int toolCost;

		private int entityTypeId;

		private bool entityTypeIdSet;

		public int GetEntityTypeId()
		{
			if (!entityTypeIdSet)
			{
				entityTypeId = entityPrefab.gameObject.name.GetStaticHash();
				entityTypeIdSet = true;
			}
			return entityTypeId;
		}
	}

	[SerializeField]
	private List<ToolEntry> toolEntries = new List<ToolEntry>();

	[SerializeField]
	private Transform displayTransform;

	[SerializeField]
	private Transform depositTransform;

	[SerializeField]
	private Transform toolSpawnLocation;

	[SerializeField]
	private TMP_Text displayItemNameText;

	[SerializeField]
	private TMP_Text displayItemCostText;

	[SerializeField]
	private float nextToolAnimationTime = 0.5f;

	[SerializeField]
	private float toolDepositAnimationTime = 1f;

	[SerializeField]
	private Vector3 toolEntryPosOffset = new Vector3(0f, 0.25f, 0f);

	[SerializeField]
	private Vector3 toolEntryRotEuler = new Vector3(0f, 0f, 15f);

	[SerializeField]
	private float toolEntryRotDegrees = 15f;

	[SerializeField]
	private Vector3 toolExitPosOffset = new Vector3(0f, 0f, -0.25f);

	[SerializeField]
	private Vector3 toolExitRotEuler = new Vector3(180f, 0f, 0f);

	[SerializeField]
	private AnimationCurve toolEntryPosTimingCurve;

	[SerializeField]
	private AnimationCurve toolEntryRotTimingCurve;

	[SerializeField]
	private AnimationCurve toolExitPosTimingCurve;

	[SerializeField]
	private AnimationCurve toolExitRotTimingCurve;

	[SerializeField]
	private AnimationCurve toolDepositTimingCurve;

	[SerializeField]
	private AnimationCurve toolDepositMotionCurveY;

	[SerializeField]
	private AnimationCurve toolDepositMotionCurveZ;

	[SerializeField]
	private Transform depositLidTransform;

	[SerializeField]
	private Vector3 depositLidOpenEuler = new Vector3(65f, 0f, 0f);

	[SerializeField]
	private AnimationCurve depositLidTimingCurve;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip nextItemAudio;

	[SerializeField]
	private float nextItemVolume = 0.5f;

	[SerializeField]
	private AudioClip purchaseAudio;

	[SerializeField]
	private float purchaseVolume = 0.5f;

	[SerializeField]
	private AudioClip purchaseFailedAudio;

	[SerializeField]
	private float purchaseFailedVolume = 0.5f;

	[SerializeField]
	private IDCardScanner idCardScanner;

	private int activeEntryIndex = 1;

	private int displayedEntryIndex = -1;

	private float animationStartTime;

	private bool animatingDeposit;

	private bool animatingSwap;

	private int animPrevToolIndex;

	private int animNextToolIndex;

	private Quaternion depositLidOpenRot = Quaternion.identity;

	private Quaternion toolEntryRot = Quaternion.identity;

	private Quaternion toolExitRot = Quaternion.identity;

	private Coroutine vendingCoroutine;

	private bool debugIgnoreToolCost;

	[HideInInspector]
	public int PurchaseStationId;

	private GhostReactorManager grManager;

	private GhostReactor reactor;

	public int ActiveEntryIndex => activeEntryIndex;

	public void Init(GhostReactorManager grManager, GhostReactor reactor)
	{
		this.grManager = grManager;
		this.reactor = reactor;
	}

	public void RequestPurchaseButton(int actorNumber)
	{
		if (actorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			grManager.ToolPurchaseStationRequest(PurchaseStationId, GhostReactorManager.ToolPurchaseStationAction.TryPurchase);
		}
	}

	public void ShiftRightButton()
	{
		grManager.ToolPurchaseStationRequest(PurchaseStationId, GhostReactorManager.ToolPurchaseStationAction.ShiftRight);
	}

	public void ShiftLeftButton()
	{
		grManager.ToolPurchaseStationRequest(PurchaseStationId, GhostReactorManager.ToolPurchaseStationAction.ShiftLeft);
	}

	public void ShiftRightAuthority()
	{
		activeEntryIndex = (activeEntryIndex + 1) % toolEntries.Count;
	}

	public void ShiftLeftAuthority()
	{
		activeEntryIndex = ((activeEntryIndex > 0) ? (activeEntryIndex - 1) : (toolEntries.Count - 1));
	}

	public void DebugPurchase()
	{
		int entityTypeId = toolEntries[activeEntryIndex].GetEntityTypeId();
		Vector3 localPosition = toolEntries[activeEntryIndex].displayToolParent.GetChild(0).localPosition;
		Quaternion localRotation = toolEntries[activeEntryIndex].displayToolParent.GetChild(0).localRotation;
		Quaternion rotation = depositTransform.rotation * localRotation;
		Vector3 position = depositTransform.position + depositTransform.rotation * localPosition;
		grManager.gameEntityManager.RequestCreateItem(entityTypeId, position, rotation, 0L);
		OnPurchaseSucceeded();
	}

	public bool TryPurchaseAuthority(GRPlayer player, out int itemCost)
	{
		int entityTypeId = toolEntries[activeEntryIndex].GetEntityTypeId();
		itemCost = reactor.GetItemCost(entityTypeId);
		if (debugIgnoreToolCost || player.ShiftCredits >= itemCost)
		{
			Vector3 localPosition = toolEntries[activeEntryIndex].displayToolParent.GetChild(0).localPosition;
			Quaternion localRotation = toolEntries[activeEntryIndex].displayToolParent.GetChild(0).localRotation;
			Quaternion rotation = depositTransform.rotation * localRotation;
			Vector3 position = depositTransform.position + depositTransform.rotation * localPosition;
			grManager.gameEntityManager.RequestCreateItem(entityTypeId, position, rotation, 0L);
			return true;
		}
		return false;
	}

	public void OnSelectionUpdate(int newSelectedIndex)
	{
		activeEntryIndex = Mathf.Clamp(newSelectedIndex % toolEntries.Count, 0, toolEntries.Count - 1);
		audioSource.PlayOneShot(nextItemAudio, nextItemVolume);
		displayItemNameText.text = toolEntries[activeEntryIndex].toolName;
		displayItemCostText.text = toolEntries[activeEntryIndex].toolCost.ToString();
	}

	public void OnPurchaseSucceeded()
	{
		animatingDeposit = true;
		animationStartTime = Time.time;
		audioSource.PlayOneShot(purchaseAudio, purchaseVolume);
		idCardScanner.onSucceeded?.Invoke();
		if (displayedEntryIndex < 0 || displayedEntryIndex >= toolEntries.Count)
		{
			displayedEntryIndex = activeEntryIndex;
		}
	}

	public void OnPurchaseFailed()
	{
		audioSource.PlayOneShot(purchaseFailedAudio, purchaseFailedVolume);
		idCardScanner.onFailed?.Invoke();
	}

	public Transform GetSpawnMarker()
	{
		return toolSpawnLocation;
	}

	public string GetCurrentToolName()
	{
		return toolEntries[activeEntryIndex].toolName;
	}

	private void Awake()
	{
		depositLidOpenRot = Quaternion.Euler(depositLidOpenEuler);
		toolEntryRot = Quaternion.Euler(toolEntryRotEuler);
		toolExitRot = Quaternion.Euler(toolExitRotEuler);
	}

	private void Update()
	{
		if (!animatingSwap && !animatingDeposit && activeEntryIndex != displayedEntryIndex)
		{
			animatingSwap = true;
			animationStartTime = Time.time;
			animPrevToolIndex = displayedEntryIndex;
			animNextToolIndex = activeEntryIndex;
			toolEntryRot = Quaternion.AngleAxis(toolEntryRotDegrees, UnityEngine.Random.onUnitSphere);
		}
		if (animatingSwap)
		{
			float num = (Time.time - animationStartTime) / nextToolAnimationTime;
			Transform transform = null;
			if (animPrevToolIndex >= 0 && animPrevToolIndex < toolEntries.Count)
			{
				transform = toolEntries[animPrevToolIndex].displayToolParent;
				transform.localRotation = Quaternion.Slerp(Quaternion.identity, toolExitRot, toolExitRotTimingCurve.Evaluate(num));
				transform.localPosition = Vector3.Lerp(Vector3.zero, toolExitPosOffset, toolExitPosTimingCurve.Evaluate(num));
			}
			Transform displayToolParent = toolEntries[animNextToolIndex].displayToolParent;
			displayToolParent.localRotation = Quaternion.Slerp(toolEntryRot, Quaternion.identity, toolEntryRotTimingCurve.Evaluate(num));
			displayToolParent.localPosition = Vector3.Lerp(toolEntryPosOffset, Vector3.zero, toolEntryPosTimingCurve.Evaluate(num));
			displayToolParent.gameObject.SetActive(value: true);
			if (num >= 1f)
			{
				if (transform != null)
				{
					transform.gameObject.SetActive(value: false);
				}
				displayedEntryIndex = animNextToolIndex;
				animatingSwap = false;
			}
		}
		else if (animatingDeposit)
		{
			float num2 = (Time.time - animationStartTime) / toolDepositAnimationTime;
			Transform displayToolParent2 = toolEntries[displayedEntryIndex].displayToolParent;
			Vector3 localPosition = displayToolParent2.localPosition;
			localPosition.y = Mathf.Lerp(0f, depositTransform.localPosition.y, toolDepositMotionCurveY.Evaluate(toolDepositTimingCurve.Evaluate(num2)));
			localPosition.z = Mathf.Lerp(0f, depositTransform.localPosition.z, toolDepositMotionCurveZ.Evaluate(toolDepositTimingCurve.Evaluate(num2)));
			displayToolParent2.localPosition = localPosition;
			depositLidTransform.localRotation = Quaternion.Slerp(Quaternion.identity, depositLidOpenRot, depositLidTimingCurve.Evaluate(num2));
			if (num2 >= 1f)
			{
				depositLidTransform.localRotation = Quaternion.identity;
				displayToolParent2.gameObject.SetActive(value: false);
				displayedEntryIndex = -1;
				animatingDeposit = false;
			}
		}
	}
}
