using System;
using System.Threading.Tasks;
using GorillaNetworking;
using TMPro;
using UnityEngine;

namespace Cosmetics;

public class CreatorCodeTerminal : MonoBehaviour, ICreatorCodeProvider, IBuildValidation
{
	private string termId;

	[SerializeField]
	private TMP_Text creatorCodeField;

	[SerializeField]
	private TMP_Text creatorCodeTitle;

	[SerializeField]
	private NexusGroupId[] nexusGroups;

	public NexusGroupId[] NexusGroups => nexusGroups;

	public string TerminalId => termId;

	GameObject ICreatorCodeProvider.GameObject => base.gameObject;

	public void Awake()
	{
		termId = string.Empty;
		for (int i = 0; i < nexusGroups.Length; i++)
		{
			termId += nexusGroups[i].Code;
		}
		HookupToCreatorCodes();
	}

	private void OnDestroy()
	{
		UnhookFromCreatorCodes();
	}

	public void HookupToCreatorCodes()
	{
		CreatorCodes.InitializedEvent += OnCreatorCodesInitialized;
		CreatorCodes.OnCreatorCodeChangedEvent += OnCreatorCodeChanged;
		CreatorCodes.OnCreatorCodeFailureEvent += OnCreatorCodeFailure;
		if (CreatorCodes.Intialized)
		{
			OnCreatorCodesInitialized();
		}
		CosmeticsController.PushTerminalMessage = (Action<string, string>)Delegate.Combine(CosmeticsController.PushTerminalMessage, new Action<string, string>(OnTerminalMessage));
	}

	private async void OnTerminalMessage(string termId, string msg)
	{
		if (!(termId != this.termId))
		{
			creatorCodeTitle.text = msg;
			while (Application.isPlaying && (VRRig.LocalRig.transform.position - base.transform.position).sqrMagnitude < 4f)
			{
				await Task.Yield();
			}
			creatorCodeTitle.text = "CREATOR CODE: VALID";
		}
	}

	public void UnhookFromCreatorCodes()
	{
		CreatorCodes.InitializedEvent -= OnCreatorCodesInitialized;
		CreatorCodes.OnCreatorCodeChangedEvent -= OnCreatorCodeChanged;
		CreatorCodes.OnCreatorCodeFailureEvent -= OnCreatorCodeFailure;
		CosmeticsController.PushTerminalMessage = (Action<string, string>)Delegate.Remove(CosmeticsController.PushTerminalMessage, new Action<string, string>(OnTerminalMessage));
	}

	private void OnCreatorCodesInitialized()
	{
		OnCreatorCodeChanged(termId);
	}

	public void OnCreatorCodeChanged(string id)
	{
		if (!(id != termId))
		{
			creatorCodeField.text = CreatorCodes.getCurrentCreatorCode(termId);
			string text = "CREATOR CODE:";
			switch (CreatorCodes.getCurrentCreatorCodeStatus(termId))
			{
			case CreatorCodes.CreatorCodeStatus.Valid:
				text += " VALID";
				break;
			case CreatorCodes.CreatorCodeStatus.Validating:
				text += " VALIDATING";
				break;
			}
			creatorCodeTitle.text = text;
		}
	}

	public void CreatorCodeInput(string character)
	{
		CreatorCodes.AppendKey(termId, character);
	}

	public void CreatorCodeDelete()
	{
		CreatorCodes.DeleteCharacter(termId);
	}

	public void OnCreatorCodeValid(string id, string s, NexusGroupId ngid)
	{
		if (!(id != termId))
		{
			creatorCodeTitle.text = "CREATOR CODE: VALID";
		}
	}

	public void OnCreatorCodeValidating(string id)
	{
		if (!(id != termId))
		{
			creatorCodeTitle.text = "CREATOR CODE: VALIDATING";
		}
	}

	public void CreatorCodeInvalid(string id)
	{
		if (!(id != termId))
		{
			creatorCodeTitle.text = "CREATOR CODE: INVALID";
		}
	}

	public void OnCreatorCodeFailure(string id)
	{
		if (!(id != termId))
		{
			creatorCodeTitle.text = "CREATOR CODE: INVALID";
		}
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (nexusGroups.Length == 0)
		{
			Debug.LogError("You have to set at least one nexus group in " + base.name + " or things will not work!");
			return false;
		}
		return true;
	}

	public void GetCreatorCode(out string code, out NexusGroupId[] groups)
	{
		code = CreatorCodes.getCurrentCreatorCode(termId);
		groups = nexusGroups;
	}
}
