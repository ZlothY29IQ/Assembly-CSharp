using UnityEngine;
using UnityEngine.UI;

public class GRUIBuyItem : MonoBehaviour
{
	[SerializeField]
	private GorillaPressableButton buyItemButton;

	[SerializeField]
	private Text itemInfoLabel;

	[SerializeField]
	private Transform spawnMarker;

	[SerializeField]
	private GameEntity entityPrefab;

	private int entityTypeId;

	private int standId;

	public void Setup(int standId)
	{
		this.standId = standId;
		buyItemButton.onPressButton.AddListener(OnBuyItem);
		entityTypeId = entityPrefab.gameObject.name.GetStaticHash();
	}

	public void OnBuyItem()
	{
	}

	public Transform GetSpawnMarker()
	{
		return spawnMarker;
	}
}
