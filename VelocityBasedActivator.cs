using UnityEngine;

[RequireComponent(typeof(GorillaVelocityEstimator))]
public class VelocityBasedActivator : MonoBehaviour
{
	[SerializeField]
	private GameObject[] activationTargets;

	private GorillaVelocityEstimator velocityEstimator;

	private float k;

	private bool active;

	[SerializeField]
	private float decay = 1f;

	[SerializeField]
	private float threshold = 1f;

	private void Start()
	{
		velocityEstimator = GetComponent<GorillaVelocityEstimator>();
	}

	private void Update()
	{
		k += velocityEstimator.linearVelocity.sqrMagnitude;
		k = Mathf.Max(k - Time.deltaTime * decay, 0f);
		if (!active && k > threshold)
		{
			activate(v: true);
		}
		if (active && k < threshold)
		{
			activate(v: false);
		}
	}

	private void activate(bool v)
	{
		active = v;
		for (int i = 0; i < activationTargets.Length; i++)
		{
			activationTargets[i].SetActive(v);
		}
	}

	private void OnDisable()
	{
		if (active)
		{
			activate(v: false);
		}
	}
}
