using UnityEngine;

public class GreyZoneActivator : MonoBehaviour
{
	[SerializeField]
	private bool activateOnEnable;

	[SerializeField]
	private bool deactivateOnDisable;

	[Range(-5f, 5f)]
	[SerializeField]
	private float gMultiplier = 1f;

	private void OnEnable()
	{
		if (activateOnEnable)
		{
			Activate();
		}
	}

	private void OnDisable()
	{
		if (deactivateOnDisable)
		{
			Deactivate();
		}
	}

	public void Activate()
	{
		GreyZoneManager.Instance.LocalSimpleActivation(onOff: true, gMultiplier);
	}

	public void ActivateWithG(float g)
	{
		GreyZoneManager.Instance.LocalSimpleActivation(onOff: true, g);
	}

	public void Deactivate()
	{
		GreyZoneManager.Instance.LocalSimpleActivation(onOff: false, 1f);
	}
}
