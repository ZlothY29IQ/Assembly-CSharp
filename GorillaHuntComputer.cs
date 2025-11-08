using System;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using UnityEngine;
using UnityEngine.UI;

public class GorillaHuntComputer : MonoBehaviour
{
	public Text text;

	public Image material;

	public Image hat;

	public Image face;

	public Image badge;

	public Image leftHand;

	public Image rightHand;

	public NetPlayer myTarget;

	public NetPlayer tempTarget;

	[DebugReadout]
	public VRRig myRig;

	public Sprite tempSprite;

	public CosmeticsController.CosmeticItem tempItem;

	private GorillaHuntManager huntManager;

	private void Update()
	{
		if (!NetworkSystem.Instance.InRoom || GorillaGameManager.instance == null)
		{
			GorillaTagger.Instance.offlineVRRig.huntComputer.SetActive(value: false);
			return;
		}
		if (huntManager == null)
		{
			huntManager = GorillaGameManager.instance.gameObject.GetComponent<GorillaHuntManager>();
			if (huntManager == null)
			{
				GorillaTagger.Instance.offlineVRRig.huntComputer.SetActive(value: false);
				return;
			}
		}
		if (!huntManager.huntStarted)
		{
			if (huntManager.waitingToStartNextHuntGame && huntManager.currentTarget.Contains(NetworkSystem.Instance.LocalPlayer) && !huntManager.currentHunted.Contains(NetworkSystem.Instance.LocalPlayer) && huntManager.countDownTime == 0)
			{
				material.gameObject.SetActive(value: false);
				hat.gameObject.SetActive(value: false);
				face.gameObject.SetActive(value: false);
				badge.gameObject.SetActive(value: false);
				leftHand.gameObject.SetActive(value: false);
				rightHand.gameObject.SetActive(value: false);
				text.text = "YOU WON! CONGRATS, HUNTER!";
			}
			else if (huntManager.countDownTime != 0)
			{
				material.gameObject.SetActive(value: false);
				hat.gameObject.SetActive(value: false);
				face.gameObject.SetActive(value: false);
				badge.gameObject.SetActive(value: false);
				leftHand.gameObject.SetActive(value: false);
				rightHand.gameObject.SetActive(value: false);
				text.text = "GAME STARTING IN:\n" + huntManager.countDownTime + "...";
			}
			else
			{
				material.gameObject.SetActive(value: false);
				hat.gameObject.SetActive(value: false);
				face.gameObject.SetActive(value: false);
				badge.gameObject.SetActive(value: false);
				leftHand.gameObject.SetActive(value: false);
				rightHand.gameObject.SetActive(value: false);
				text.text = "WAITING TO START";
			}
			return;
		}
		myTarget = huntManager.GetTargetOf(NetworkSystem.Instance.LocalPlayer);
		RigContainer playerRig;
		if (myTarget == null || !myTarget.InRoom)
		{
			material.gameObject.SetActive(value: false);
			hat.gameObject.SetActive(value: false);
			face.gameObject.SetActive(value: false);
			badge.gameObject.SetActive(value: false);
			leftHand.gameObject.SetActive(value: false);
			rightHand.gameObject.SetActive(value: false);
			text.text = "YOU ARE DEAD\nTAG OTHERS\nTO SLOW THEM";
		}
		else if (VRRigCache.Instance.TryGetVrrig(myTarget, out playerRig))
		{
			myRig = playerRig.Rig;
			if (myRig != null)
			{
				material.material = myRig.materialsToChangeTo[myRig.setMatIndex];
				text.text = "TARGET:\n" + NormalizeName(doIt: true, myRig.creator?.NickName) + "\nDISTANCE: " + Mathf.CeilToInt((GTPlayer.Instance.headCollider.transform.position - myRig.transform.position).magnitude) + "M";
				SetImage(myRig.cosmeticSet.items[0].displayName, ref hat);
				SetImage(myRig.cosmeticSet.items[2].displayName, ref face);
				SetImage(myRig.cosmeticSet.items[1].displayName, ref badge);
				SetImage(GetPrioritizedItemForHand(myRig, forLeftHand: true).displayName, ref leftHand);
				SetImage(GetPrioritizedItemForHand(myRig, forLeftHand: false).displayName, ref rightHand);
				material.gameObject.SetActive(value: true);
			}
		}
	}

	private void SetImage(string itemDisplayName, ref Image image)
	{
		tempItem = CosmeticsController.instance.GetItemFromDict(CosmeticsController.instance.GetItemNameFromDisplayName(itemDisplayName));
		if (tempItem.displayName != "NOTHING" && myRig != null && myRig.IsItemAllowed(tempItem.itemName))
		{
			image.gameObject.SetActive(value: true);
			image.sprite = tempItem.itemPicture;
		}
		else
		{
			image.gameObject.SetActive(value: false);
		}
	}

	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			if (GorillaComputer.instance.CheckAutoBanListForName(text))
			{
				text = new string(Array.FindAll(text.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
				if (text.Length > 12)
				{
					text = text.Substring(0, 11);
				}
				text = text.ToUpper();
			}
			else
			{
				text = "BADGORILLA";
			}
		}
		return text;
	}

	public CosmeticsController.CosmeticItem GetPrioritizedItemForHand(VRRig targetRig, bool forLeftHand)
	{
		CosmeticsController.CosmeticItem result;
		if (forLeftHand)
		{
			result = targetRig.cosmeticSet.items[7];
			if (result.displayName != "null")
			{
				return result;
			}
			result = targetRig.cosmeticSet.items[4];
			if (result.displayName != "null")
			{
				return result;
			}
			return targetRig.cosmeticSet.items[5];
		}
		result = targetRig.cosmeticSet.items[8];
		if (result.displayName != "null")
		{
			return result;
		}
		result = targetRig.cosmeticSet.items[3];
		if (result.displayName != "null")
		{
			return result;
		}
		return targetRig.cosmeticSet.items[6];
	}
}
