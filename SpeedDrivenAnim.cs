using UnityEngine;

public class SpeedDrivenAnim : MonoBehaviour
{
	[SerializeField]
	private float speed0;

	[SerializeField]
	private float speed1 = 1f;

	[SerializeField]
	private float maxChangePerSecond = 1f;

	[SerializeField]
	private string animKey = "speed";

	private GorillaVelocityEstimator velocityEstimator;

	private Animator animator;

	private int keyHash;

	private float currentBlend;

	private void Start()
	{
		velocityEstimator = GetComponent<GorillaVelocityEstimator>();
		animator = GetComponent<Animator>();
		keyHash = Animator.StringToHash(animKey);
	}

	private void Update()
	{
		float target = Mathf.InverseLerp(speed0, speed1, velocityEstimator.linearVelocity.magnitude);
		currentBlend = Mathf.MoveTowards(currentBlend, target, maxChangePerSecond * Time.deltaTime);
		animator.SetFloat(keyHash, currentBlend);
	}
}
