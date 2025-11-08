using UnityEngine;

public class CloudUmbrellaCloud : MonoBehaviour
{
	public UmbrellaItem umbrella;

	public Transform cloudRotateXform;

	public Renderer cloudRenderer;

	public AnimationCurve scaleCurve;

	private const float kHideAtScale = 0.1f;

	private const float kHideAtScaleTolerance = 0.01f;

	private bool rendererOn;

	private Transform umbrellaXform;

	private Transform cloudScaleXform;

	protected void Awake()
	{
		umbrellaXform = umbrella.transform;
		cloudScaleXform = cloudRenderer.transform;
	}

	protected void LateUpdate()
	{
		float time = Vector3.Dot(umbrellaXform.up, Vector3.up);
		float num = Mathf.Clamp01(scaleCurve.Evaluate(time));
		bool flag = num > 0.09f && num < 0.1f;
		rendererOn = (flag ? rendererOn : (num > 0.1f));
		cloudRenderer.enabled = rendererOn;
		cloudScaleXform.localScale = new Vector3(num, num, num);
		cloudRotateXform.up = Vector3.up;
	}
}
