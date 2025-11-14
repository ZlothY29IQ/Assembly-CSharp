using GorillaTagScripts.GhostReactor;
using TMPro;
using UnityEngine;

public class GRRecyclerScanner : MonoBehaviour
{
	public GRRecycler recycler;

	[SerializeField]
	private TextMeshPro titleText;

	[SerializeField]
	private TextMeshPro descriptionText;

	[SerializeField]
	private TextMeshPro annotationText;

	[SerializeField]
	private TextMeshPro recycleValueText;

	public AudioSource audioSource;

	public AudioClip recyclerBarcodeAudio;

	public float recyclerBarcodeAudioVolume = 0.5f;

	private void Awake()
	{
		titleText.text = "";
		descriptionText.text = "";
		annotationText.text = "";
		recycleValueText.text = "";
	}

	public void ScanItem(GameEntityId id)
	{
		if (!(recycler != null) || !(recycler.reactor != null) || !(recycler.reactor.grManager != null) || !(recycler.reactor.grManager.gameEntityManager != null))
		{
			return;
		}
		GameEntity gameEntity = recycler.reactor.grManager.gameEntityManager.GetGameEntity(id);
		if (!(gameEntity == null))
		{
			GRScannable component = gameEntity.GetComponent<GRScannable>();
			if (!(component == null))
			{
				titleText.text = component.GetTitleText(recycler.reactor);
				descriptionText.text = component.GetBodyText(recycler.reactor);
				annotationText.text = component.GetAnnotationText(recycler.reactor);
				recycleValueText.text = $"Recycle value: {recycler.GetRecycleValue(gameEntity.gameObject.GetToolType())}";
				audioSource.volume = recyclerBarcodeAudioVolume;
				audioSource.PlayOneShot(recyclerBarcodeAudio);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(recycler.reactor == null) && recycler.reactor.grManager.IsAuthority())
		{
			GRScannable componentInParent = other.gameObject.GetComponentInParent<GRScannable>();
			if (!(componentInParent == null))
			{
				recycler.reactor.grManager.RequestRecycleScanItem(componentInParent.gameEntity.id);
			}
		}
	}
}
