using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class GorillaPlayerLineButton : MonoBehaviour
{
	public enum ButtonType
	{
		HateSpeech,
		Cheating,
		Toxicity,
		Mute,
		Report,
		Cancel
	}

	public GorillaPlayerScoreboardLine parentLine;

	public ButtonType buttonType;

	public bool isOn;

	public bool isAutoOn;

	public Material offMaterial;

	public Material onMaterial;

	public Material autoOnMaterial;

	public string offText;

	public string onText;

	public string autoOnText;

	public Text myText;

	public float debounceTime = 0.25f;

	public float touchTime;

	public bool testPress;

	private void OnEnable()
	{
		if (Application.isEditor)
		{
			StartCoroutine(TestPressCheck());
		}
	}

	private void OnDisable()
	{
		if (Application.isEditor)
		{
			StopAllCoroutines();
		}
	}

	private IEnumerator TestPressCheck()
	{
		while (true)
		{
			if (testPress)
			{
				testPress = false;
				if (buttonType == ButtonType.Mute)
				{
					isOn = !isOn;
				}
				parentLine.PressButton(isOn, buttonType);
			}
			yield return new WaitForSeconds(1f);
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (!base.enabled || !(touchTime + debounceTime < Time.time) || !(collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() != null))
		{
			return;
		}
		touchTime = Time.time;
		GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (buttonType == ButtonType.Mute)
		{
			if (isAutoOn)
			{
				isOn = false;
			}
			else
			{
				isOn = !isOn;
			}
		}
		if (buttonType != ButtonType.Mute && buttonType != ButtonType.HateSpeech && buttonType != ButtonType.Cheating && buttonType != ButtonType.Cancel && !parentLine.canPressNextReportButton)
		{
			return;
		}
		parentLine.PressButton(isOn, buttonType);
		if (component != null)
		{
			GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, component.isLeftHand, 0.05f);
			if (PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, component.isLeftHand, 0.05f);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (buttonType != ButtonType.Mute && other.GetComponentInParent<GorillaTriggerColliderHandIndicator>() != null)
		{
			parentLine.canPressNextReportButton = true;
		}
	}

	public void UpdateColor()
	{
		if (isOn)
		{
			GetComponent<MeshRenderer>().material = onMaterial;
			myText.text = onText;
		}
		else if (isAutoOn)
		{
			GetComponent<MeshRenderer>().material = autoOnMaterial;
			myText.text = autoOnText;
		}
		else
		{
			GetComponent<MeshRenderer>().material = offMaterial;
			myText.text = offText;
		}
	}
}
