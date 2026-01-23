using System;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor;

[RequireComponent(typeof(GorillaPressableButton))]
public sealed class GRDelveDeeperButton : MonoBehaviour
{
	[SerializeField]
	private GhostReactorShiftManager _shiftManager;

	[SerializeField]
	private TextMeshPro _text;

	private bool _lastAuthorizedToDelveDeeper;

	private GorillaPressableButton _button;

	private void OnEnable()
	{
		if (_shiftManager == null)
		{
			throw new Exception("_shiftManager unset for GREndShiftButton.");
		}
		_button = GetComponent<GorillaPressableButton>();
		UpdateButton();
	}

	private void LateUpdate()
	{
		if (_lastAuthorizedToDelveDeeper != _shiftManager.authorizedToDelveDeeper)
		{
			UpdateButton();
		}
	}

	private void UpdateButton()
	{
		_lastAuthorizedToDelveDeeper = _shiftManager.authorizedToDelveDeeper;
		if (_lastAuthorizedToDelveDeeper)
		{
			_button.enabled = true;
			_text.text = "DELVE\nNOW";
		}
		else
		{
			_button.enabled = false;
			_text.text = "DISABLED";
		}
	}

	public void DelveDeeper()
	{
		_shiftManager.EndShift();
	}
}
