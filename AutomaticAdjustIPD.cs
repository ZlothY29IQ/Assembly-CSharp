using UnityEngine;
using UnityEngine.XR;

public class AutomaticAdjustIPD : MonoBehaviour, IGorillaSliceableSimple
{
	public InputDevice headset;

	public float currentIPD;

	public Vector3 leftEyePosition;

	public Vector3 rightEyePosition;

	public bool testOverride;

	public Transform[] adjustXScaleObjects;

	public float sizeAt58mm = 1f;

	public float sizeAt63mm = 1.12f;

	public float lastIPD;

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		if (!headset.isValid)
		{
			headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		}
		if (!headset.isValid || !headset.TryGetFeatureValue(CommonUsages.leftEyePosition, out leftEyePosition) || !headset.TryGetFeatureValue(CommonUsages.rightEyePosition, out rightEyePosition))
		{
			return;
		}
		currentIPD = (rightEyePosition - leftEyePosition).magnitude;
		if (Mathf.Abs(lastIPD - currentIPD) < 0.01f)
		{
			return;
		}
		lastIPD = currentIPD;
		for (int i = 0; i < adjustXScaleObjects.Length; i++)
		{
			Transform transform = adjustXScaleObjects[i];
			if (!transform)
			{
				break;
			}
			transform.localScale = new Vector3(Mathf.LerpUnclamped(1f, 1.12f, (currentIPD - 0.058f) / 0.0050000027f), 1f, 1f);
		}
	}
}
