using System.Collections;
using GorillaTag.CosmeticSystem;
using TMPro;
using UnityEngine;

public class PropHuntDebugHelper : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static PropHuntDebugHelper instance;

	[SerializeField]
	private GorillaPropHuntGameManager _propHuntManager;

	[SerializeField]
	private PropHuntHandFollower _localPropHuntHandFollower;

	[SerializeField]
	private TextMeshPro _propsText;

	[SerializeField]
	private AllCosmeticsArraySO _allCosmetics;

	private string[] _cachedAllPropIDs;

	private int _selectedPropIndex = -1;

	protected void Awake()
	{
		if (instance != null)
		{
			Object.Destroy(this);
		}
		else
		{
			instance = this;
		}
	}

	private IEnumerator Start()
	{
		yield return null;
		yield return null;
		_propHuntManager = Object.FindAnyObjectByType<GorillaPropHuntGameManager>();
		if (_propHuntManager != null)
		{
			Debug.Log("PropHuntDebugHelper :: Found number of props " + PropHuntPools.AllPropCosmeticIds.Length);
			_cachedAllPropIDs = PropHuntPools.AllPropCosmeticIds;
			_localPropHuntHandFollower = VRRig.LocalRig.GetComponent<PropHuntHandFollower>();
			UpdatePropsText();
		}
	}

	public void UpdatePropsText()
	{
		string selectedPropID = GetSelectedPropID(_selectedPropIndex);
		string text = string.Empty;
		if (_selectedPropIndex != -1)
		{
			CosmeticSO cosmeticSO = _allCosmetics.SearchForCosmeticSO(selectedPropID);
			if (cosmeticSO != null)
			{
				text = cosmeticSO.info.displayName;
			}
		}
		_propsText.text = "Current Prop: " + GetCurrentPropInfo() + "\n" + $"Selected Prop: {selectedPropID} - {text} ({_selectedPropIndex}/{_cachedAllPropIDs.Length})";
	}

	private string GetCurrentPropInfo()
	{
		return string.Empty;
	}

	private string GetSelectedPropID(int index)
	{
		if (index <= -1)
		{
			return "None";
		}
		return _cachedAllPropIDs[index];
	}

	[ContextMenu("Prev Prop")]
	public void PrevProp()
	{
		_selectedPropIndex--;
		if (_selectedPropIndex < -1)
		{
			_selectedPropIndex = _cachedAllPropIDs.Length - 1;
		}
		string newPropId = ((_selectedPropIndex <= -1) ? string.Empty : (newPropId = GetSelectedPropID(_selectedPropIndex)));
		SendForcePropHandRPC(newPropId);
		UpdatePropsText();
	}

	[ContextMenu("Next Prop")]
	public void NextProp()
	{
		_selectedPropIndex++;
		if (_selectedPropIndex >= _cachedAllPropIDs.Length)
		{
			_selectedPropIndex = -1;
		}
		string newPropId = ((_selectedPropIndex <= -1) ? string.Empty : (newPropId = GetSelectedPropID(_selectedPropIndex)));
		SendForcePropHandRPC(newPropId);
		UpdatePropsText();
	}

	private void SendForcePropHandRPC(string newPropId)
	{
	}

	[ContextMenu("Toggle Round")]
	public void ToggleRound()
	{
	}
}
