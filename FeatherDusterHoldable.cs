using UnityEngine;

public class FeatherDusterHoldable : MonoBehaviour, IGorillaSliceableSimple
{
	public LayerMask collisionLayer;

	public float overlapSphereRadius = 0.08f;

	[Tooltip("Collision is not tested until this speed requirement is met.")]
	private float collideMinSpeed = 1f;

	public ParticleSystem particleFx;

	public SoundBankPlayer soundBankPlayer;

	[SerializeField]
	private float soundCooldown = 0.8f;

	private ParticleSystem.EmissionModule emissionModule;

	private float initialRateOverTime;

	private float timeSinceLastSound;

	private Vector3 lastWorldPos;

	private float lastSliceTime;

	private Collider[] colliderResult = new Collider[1];

	public void Awake()
	{
		timeSinceLastSound = soundCooldown;
		emissionModule = particleFx.emission;
		initialRateOverTime = emissionModule.rateOverTimeMultiplier;
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		lastWorldPos = base.transform.position;
		lastSliceTime = Time.unscaledTime;
		emissionModule.rateOverTimeMultiplier = 0f;
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		float unscaledTime = Time.unscaledTime;
		float num = Mathf.Max(unscaledTime - lastSliceTime, Mathf.Epsilon);
		lastSliceTime = unscaledTime;
		timeSinceLastSound += num;
		Transform transform = base.transform;
		Vector3 position = transform.position;
		float num2 = (position - lastWorldPos).sqrMagnitude / num;
		emissionModule.rateOverTimeMultiplier = 0f;
		if (num2 >= collideMinSpeed * collideMinSpeed && Physics.OverlapSphereNonAlloc(position, overlapSphereRadius * transform.localScale.x, colliderResult, collisionLayer) > 0)
		{
			emissionModule.rateOverTimeMultiplier = initialRateOverTime;
			if (timeSinceLastSound >= soundCooldown)
			{
				soundBankPlayer.Play();
				timeSinceLastSound = 0f;
			}
		}
		lastWorldPos = position;
	}
}
