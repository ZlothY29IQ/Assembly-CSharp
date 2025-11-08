using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SnapTurnOverrideOnEnable : MonoBehaviour, ISnapTurnOverride
{
	private GorillaSnapTurn snapTurn;

	private bool snapTurnOverride;

	private void OnEnable()
	{
		if (snapTurn == null && GorillaTagger.Instance != null)
		{
			snapTurn = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
		}
		if (snapTurn != null)
		{
			snapTurnOverride = true;
			snapTurn.SetTurningOverride(this);
		}
	}

	private void OnDisable()
	{
		if (snapTurnOverride)
		{
			snapTurnOverride = false;
			snapTurn.UnsetTurningOverride(this);
		}
	}

	bool ISnapTurnOverride.TurnOverrideActive()
	{
		return snapTurnOverride;
	}
}
