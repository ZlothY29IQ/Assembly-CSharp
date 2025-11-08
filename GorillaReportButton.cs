using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class GorillaReportButton : MonoBehaviour
{
	[SerializeField]
	public enum MetaReportReason
	{
		HateSpeech,
		Cheating,
		Toxicity,
		Bullying,
		Doxing,
		Impersonation,
		Submit,
		Cancel
	}

	public MetaReportReason metaReportType;

	public GorillaPlayerLineButton.ButtonType buttonType;

	public GorillaPlayerScoreboardLine parentLine;

	public bool isOn;

	public Material offMaterial;

	public Material onMaterial;

	public string offText;

	public string onText;

	public Text myText;

	public float debounceTime = 0.25f;

	public float touchTime;

	public bool testPress;

	public bool selected;

	public void AssignParentLine(GorillaPlayerScoreboardLine parent)
	{
		parentLine = parent;
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (base.enabled && touchTime + debounceTime < Time.time)
		{
			isOn = !isOn;
			UpdateColor();
			selected = !selected;
			touchTime = Time.time;
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, isLeftHand: false, 0.05f);
			if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, false, 0.05f);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (metaReportType != MetaReportReason.Cancel)
		{
			_ = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>() != null;
		}
	}

	public void UpdateColor()
	{
		if (isOn)
		{
			GetComponent<MeshRenderer>().material = onMaterial;
		}
		else
		{
			GetComponent<MeshRenderer>().material = offMaterial;
		}
	}
}
