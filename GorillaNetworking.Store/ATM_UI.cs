using System;
using System.Threading.Tasks;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaNetworking.Store;

public class ATM_UI : MonoBehaviour
{
	public TMP_Text atmText;

	public TMP_Text[] ATM_RightColumnButtonText;

	public TMP_Text[] ATM_RightColumnArrowText;

	[SerializeField]
	private string purchaseLocation;

	[SerializeField]
	private GameObject creatorCodeObject;

	[SerializeField]
	private TMP_Text creatorCodeTitle;

	[SerializeField]
	private TMP_Text creatorCodeField;

	[SerializeField]
	private string memberCode;

	[SerializeField]
	private NexusGroupId groupId;

	[SerializeField]
	private string memberCodeTitleDataKey;

	private Scene customMapScene;

	public string PurchaseLocation => purchaseLocation;

	private void Start()
	{
		if (!(ATM_Manager.instance == null) && !ATM_Manager.instance.atmUIs.Contains(this))
		{
			if (!memberCodeTitleDataKey.IsNullOrEmpty())
			{
				loadMemberCodeFromTitleDate(memberCodeTitleDataKey);
			}
			else if (!memberCode.IsNullOrEmpty() && groupId != null)
			{
				ATM_Manager.instance.AddATM(this, new Tuple<string, string>(memberCode, groupId.Code));
			}
			else
			{
				ATM_Manager.instance.AddATM(this, null);
			}
		}
	}

	private async void loadMemberCodeFromTitleDate(string memberCodeTitleDataKey)
	{
		while (PlayFabTitleDataCache.Instance == null)
		{
			await Task.Yield();
		}
		PlayFabTitleDataCache.Instance.GetTitleData(memberCodeTitleDataKey, onTD, onTDError);
	}

	private void onTD(string result)
	{
		if (result.Contains("$"))
		{
			string[] array = result.Split('$');
			ATM_Manager.instance.AddATM(this, new Tuple<string, string>(array[0], array[1]));
		}
		else if (groupId != null)
		{
			Debug.LogError("ATM_UI(" + AssetUtils.GetGameObjectPath(base.gameObject) + ") :: Title Data missing group code. Using \"" + result + "$" + groupId.Code + "\". Expected format: \"<MemberCode>$<GroupCode>\" Got: \"" + result + "\"");
			ATM_Manager.instance.AddATM(this, new Tuple<string, string>(result, groupId.Code));
		}
		else
		{
			Debug.LogError("ATM_UI(" + AssetUtils.GetGameObjectPath(base.gameObject) + ") :: Title Data missing group code. No code is set. Expected format: \"<MemberCode>$<GroupCode>\" Got: \"" + result + "\"");
			ATM_Manager.instance.AddATM(this, null);
		}
	}

	private void onTDError(PlayFabError error)
	{
		Debug.LogError($"ATM_UI({AssetUtils.GetGameObjectPath(base.gameObject)}) :: PlayFabError :: {error}");
		ATM_Manager.instance.AddATM(this, null);
	}

	public void SetCustomMapScene(Scene scene)
	{
		customMapScene = scene;
	}

	public bool IsFromCustomMapScene(Scene scene)
	{
		return customMapScene == scene;
	}

	internal void SetCreatorCodeTitle(string result)
	{
		if (creatorCodeTitle != null)
		{
			creatorCodeTitle.text = result;
		}
	}

	internal void SetCreatorCodeField(string v)
	{
		if (creatorCodeField != null)
		{
			creatorCodeField.text = v;
		}
	}

	internal void HideCreatorCode()
	{
		if (creatorCodeObject != null)
		{
			creatorCodeObject.SetActive(value: false);
		}
	}

	internal void ShowCreatorCode()
	{
		if (creatorCodeObject != null)
		{
			creatorCodeObject.SetActive(value: true);
		}
	}
}
