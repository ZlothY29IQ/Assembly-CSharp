using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class KIDAgeGateConfirmation : MonoBehaviour
{
	[Header("Localization")]
	[SerializeField]
	private LocalizedText _localizedTextBody;

	private IntVariable _userAgeVar;

	private IntVariable UserAgeVar
	{
		get
		{
			if (_userAgeVar == null)
			{
				_userAgeVar = _localizedTextBody.StringReference["user-age"] as IntVariable;
				if (_userAgeVar == null)
				{
					Debug.LogError("[Localization::KID_AGE_GATE_CONFIRMATION] Failed to get [user-age] smart variable as IntVariable");
				}
			}
			return _userAgeVar;
		}
	}

	public KidAgeConfirmationResult Result { get; private set; }

	private void Start()
	{
		Result = KidAgeConfirmationResult.None;
	}

	public void OnConfirm()
	{
		Result = KidAgeConfirmationResult.Confirm;
	}

	public void OnBack()
	{
		Result = KidAgeConfirmationResult.Back;
	}

	public void Reset(int userAge)
	{
		Result = KidAgeConfirmationResult.None;
		if (UserAgeVar == null)
		{
			Debug.LogError("[LOCALIZATION::KID_AGE_GATE_CONFIRMATION] Unable to update [UserAgeVar] value, as it is null");
		}
		else
		{
			UserAgeVar.Value = userAge;
		}
	}
}
