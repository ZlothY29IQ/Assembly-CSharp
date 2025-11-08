using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineRenderVelocityMapper : MonoBehaviour
{
	[SerializeField]
	private GorillaVelocityEstimator velocityEstimator;

	private LineRenderer _lr;

	private void Awake()
	{
		_lr = GetComponent<LineRenderer>();
		_lr.useWorldSpace = true;
	}

	private void LateUpdate()
	{
		if (!(velocityEstimator == null))
		{
			_lr.SetPosition(0, velocityEstimator.transform.position);
			if (velocityEstimator.linearVelocity.sqrMagnitude > 0.1f)
			{
				_lr.SetPosition(1, velocityEstimator.transform.position + velocityEstimator.linearVelocity.normalized * 0.2f);
			}
			else
			{
				_lr.SetPosition(1, velocityEstimator.transform.position);
			}
		}
	}
}
