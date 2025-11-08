using TMPro;
using UnityEngine;

public class GRRecyclerScanner : MonoBehaviour
{
	public GRRecycler recycler;

	[SerializeField]
	private TextMeshPro toolText;

	[SerializeField]
	private TextMeshPro ratesText;

	public AudioSource audioSource;

	public AudioClip recyclerBarcodeAudio;

	public float recyclerBarcodeAudioVolume = 0.5f;

	private void Awake()
	{
		toolText.text = "";
		ratesText.text = "";
	}

	public void ScanItem(GRTool.GRToolType toolType)
	{
		int num = 0;
		switch (toolType)
		{
		case GRTool.GRToolType.Club:
			num = recycler.GetRecycleValue(GRTool.GRToolType.Club);
			break;
		case GRTool.GRToolType.Collector:
			num = recycler.GetRecycleValue(GRTool.GRToolType.Collector);
			break;
		case GRTool.GRToolType.Flash:
			num = recycler.GetRecycleValue(GRTool.GRToolType.Flash);
			break;
		case GRTool.GRToolType.Lantern:
			num = recycler.GetRecycleValue(GRTool.GRToolType.Lantern);
			break;
		case GRTool.GRToolType.Revive:
			num = recycler.GetRecycleValue(GRTool.GRToolType.Revive);
			break;
		case GRTool.GRToolType.ShieldGun:
			num = recycler.GetRecycleValue(GRTool.GRToolType.ShieldGun);
			break;
		case GRTool.GRToolType.DirectionalShield:
			num = recycler.GetRecycleValue(GRTool.GRToolType.DirectionalShield);
			break;
		case GRTool.GRToolType.HockeyStick:
			num = recycler.GetRecycleValue(GRTool.GRToolType.HockeyStick);
			break;
		case GRTool.GRToolType.DockWrist:
			num = recycler.GetRecycleValue(GRTool.GRToolType.DockWrist);
			break;
		}
		toolText.text = GRUtils.GetToolName(toolType);
		ratesText.text = num.ToString("D2") ?? "";
		audioSource.volume = recyclerBarcodeAudioVolume;
		audioSource.PlayOneShot(recyclerBarcodeAudio);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (recycler.reactor == null || !recycler.reactor.grManager.IsAuthority())
		{
			return;
		}
		GRTool componentInParent = other.gameObject.GetComponentInParent<GRTool>();
		if (!(componentInParent == null))
		{
			GRTool.GRToolType toolType = GRTool.GRToolType.None;
			if (other.gameObject.GetComponentInParent<GRToolClub>() != null)
			{
				toolType = GRTool.GRToolType.Club;
			}
			else if (other.gameObject.GetComponentInParent<GRToolCollector>() != null)
			{
				toolType = GRTool.GRToolType.Collector;
			}
			else if (other.gameObject.GetComponentInParent<GRToolFlash>() != null)
			{
				toolType = GRTool.GRToolType.Flash;
			}
			else if (other.gameObject.GetComponentInParent<GRToolLantern>() != null)
			{
				toolType = GRTool.GRToolType.Lantern;
			}
			else if (other.gameObject.GetComponentInParent<GRToolRevive>() != null)
			{
				toolType = GRTool.GRToolType.Revive;
			}
			else if (other.gameObject.GetComponentInParent<GRToolShieldGun>() != null)
			{
				toolType = GRTool.GRToolType.ShieldGun;
			}
			else if (other.gameObject.GetComponentInParent<GRToolDirectionalShield>() != null)
			{
				toolType = GRTool.GRToolType.DirectionalShield;
			}
			else if (componentInParent.toolType == GRTool.GRToolType.HockeyStick || componentInParent.toolType == GRTool.GRToolType.DockWrist)
			{
				toolType = componentInParent.toolType;
			}
			recycler.reactor.grManager.RequestRecycleScanItem(toolType);
		}
	}
}
