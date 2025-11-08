using UnityEngine;
using UnityEngine.Events;

public class TransferrableObjectSyncedBool : TransferrableObject
{
	[SerializeField]
	private bool deprecatedWarning = true;

	[SerializeField]
	private UnityEvent OnItemStateSetTrue;

	[SerializeField]
	private UnityEvent OnItemStateSetFalse;

	internal override void OnEnable()
	{
		base.OnEnable();
		OnItemStateBoolFalse.AddListener(OnItemStateChanged);
		OnItemStateBoolTrue.AddListener(OnItemStateChanged);
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		OnItemStateBoolFalse.RemoveListener(OnItemStateChanged);
		OnItemStateBoolTrue.RemoveListener(OnItemStateChanged);
	}

	public void SetItemState(bool state)
	{
		SetItemStateBool(state);
	}

	public void ToggleItemState()
	{
		ToggleNetworkedItemStateBool();
	}

	private void OnItemStateChanged()
	{
		if (itemState == ItemStates.State0)
		{
			OnItemStateSetFalse?.Invoke();
		}
		else
		{
			OnItemStateSetTrue?.Invoke();
		}
	}
}
