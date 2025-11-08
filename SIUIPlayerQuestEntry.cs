using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SIUIPlayerQuestEntry : MonoBehaviour
{
	public Image background;

	public SIUIProgressBar progress;

	public TextMeshProUGUI questDescription;

	public GameObject completeOverlay;

	public GameObject questInfo;

	public GameObject noQuestAvailable;

	public GameObject newQuestTag;

	public int lastQuestId;

	public int lastQuestProgress;

	private void Awake()
	{
		lastQuestId = -1;
		lastQuestProgress = -1;
	}
}
