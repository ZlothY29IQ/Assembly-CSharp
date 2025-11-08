using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class KIDUI_AgeDiscrepancyScreen : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _descriptionText;

	[Header("Localization")]
	[SerializeField]
	private LocalizedText _bodyTextLoc;

	private bool _hasCompleted;

	private LocalizedString _bodyLocStr;

	private IntVariable _userAgeVar;

	private IntVariable _accountAgeVar;

	private IntVariable _lowestAgeVar;

	private void Awake()
	{
		CheckLocalizationReferences();
	}

	public async Task ShowAgeDiscrepancyScreenWithAwait(string description)
	{
		base.gameObject.SetActive(value: true);
		CheckLocalizationReferences();
		_descriptionText.text = description;
		await WaitForCompletion();
	}

	public async Task ShowAgeDiscrepancyScreenWithAwait(int userAge, int accAge, int lowestAge)
	{
		base.gameObject.SetActive(value: true);
		CheckLocalizationReferences();
		_userAgeVar.Value = userAge;
		_accountAgeVar.Value = accAge;
		_lowestAgeVar.Value = lowestAge;
		await WaitForCompletion();
	}

	private async Task WaitForCompletion()
	{
		do
		{
			await Task.Yield();
		}
		while (!_hasCompleted);
	}

	public void OnHoldComplete()
	{
		_hasCompleted = true;
	}

	public void OnQuitPressed()
	{
		Application.Quit();
	}

	private void CheckLocalizationReferences()
	{
		if (_bodyLocStr == null || _userAgeVar == null || _accountAgeVar == null || _lowestAgeVar == null)
		{
			if (_bodyTextLoc == null)
			{
				Debug.LogError("[LOCALIZATION::KIDUI_AGE_DISCREPANCY_SCREEN] [_bodyTextLoc] is not set, unable to localize smart string");
				return;
			}
			_bodyLocStr = _bodyTextLoc.StringReference;
			_userAgeVar = _bodyLocStr["user-age"] as IntVariable;
			_accountAgeVar = _bodyLocStr["account-age"] as IntVariable;
			_lowestAgeVar = _bodyLocStr["lowest-age"] as IntVariable;
		}
	}
}
