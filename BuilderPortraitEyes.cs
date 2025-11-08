using UnityEngine;

public class BuilderPortraitEyes : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private Transform eyeCenter;

	[SerializeField]
	private GameObject eyes;

	[SerializeField]
	private float moveRadius = 0.5f;

	private float scale = 1f;

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		scale = base.transform.lossyScale.x;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		eyes.transform.position = eyeCenter.transform.position;
	}

	public void SliceUpdate()
	{
		if (!(GorillaTagger.Instance == null))
		{
			Vector3 vector = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(GorillaTagger.Instance.headCollider.transform.position - eyeCenter.position, eyeCenter.forward), moveRadius * scale);
			eyes.transform.position = eyeCenter.position + vector;
		}
	}
}
