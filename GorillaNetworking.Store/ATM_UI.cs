using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaNetworking.Store;

public class ATM_UI : MonoBehaviour
{
	public GameObject creatorCodeObject;

	public TMP_Text atmText;

	public TMP_Text creatorCodeTitle;

	public TMP_Text creatorCodeField;

	public TMP_Text[] ATM_RightColumnButtonText;

	public TMP_Text[] ATM_RightColumnArrowText;

	private Scene customMapScene;

	private void Start()
	{
		if (ATM_Manager.instance != null && !ATM_Manager.instance.atmUIs.Contains(this))
		{
			ATM_Manager.instance.AddATM(this);
		}
	}

	public void SetCustomMapScene(Scene scene)
	{
		customMapScene = scene;
	}

	public bool IsFromCustomMapScene(Scene scene)
	{
		return customMapScene == scene;
	}
}
