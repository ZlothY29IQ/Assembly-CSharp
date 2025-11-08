using UnityEngine;

public class WingsWearable : MonoBehaviour, IGorillaSliceableSimple
{
	[Tooltip("This animator must have a parameter called 'FlapSpeed'")]
	public Animator animator;

	[Tooltip("X axis is move speed, Y axis is flap speed")]
	public AnimationCurve flapSpeedCurve;

	private Transform xform;

	private Vector3 oldPos;

	private readonly int flapSpeedParamID = Animator.StringToHash("FlapSpeed");

	private void Awake()
	{
		if (animator == null)
		{
			GTDev.LogError("WingsWearable on " + base.gameObject.name + " missing animator");
		}
		else
		{
			xform = animator.transform;
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		oldPos = xform.localPosition;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		Vector3 position = xform.position;
		float f = (position - oldPos).magnitude / Time.deltaTime;
		float value = flapSpeedCurve.Evaluate(Mathf.Abs(f));
		animator.SetFloat(flapSpeedParamID, value);
		oldPos = position;
	}
}
