using UnityEngine;

public class GreyZoneActivator : MonoBehaviour
{
	[SerializeField]
	private bool activateOnEnable;

	[SerializeField]
	private bool deactivateOnDisable;

	[SerializeField]
	private bool visualOnly;

	[Range(-5f, 5f)]
	[SerializeField]
	private float gMultiplier = 1f;

	private ShaderHashId _GreyZoneActive = new ShaderHashId("_GreyZoneActive");

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
		if (visualOnly)
		{
			Shader.SetGlobalInt(_GreyZoneActive, 1);
		}
		else
		{
			GreyZoneManager.Instance.LocalSimpleActivation(onOff: true, gMultiplier);
		}
	}

	public void ActivateWithG(float g)
	{
		GreyZoneManager.Instance.LocalSimpleActivation(onOff: true, g);
	}

	public void Deactivate()
	{
		if (visualOnly)
		{
			Shader.SetGlobalInt(_GreyZoneActive, 0);
		}
		else
		{
			GreyZoneManager.Instance.LocalSimpleActivation(onOff: false, 1f);
		}
	}
}
