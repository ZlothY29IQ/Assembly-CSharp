using System;
using GorillaExtensions;
using UnityEngine;

public class CosmeticCritterShadeFleeing : CosmeticCritter
{
	[Serializable]
	private class ModelSwap
	{
		public float relativeProbability;

		public GameObject gameObject;
	}

	[Tooltip("Randomly selects one of these models when spawned, accounting for relative probabilities. For example, if one model has a probability of 1 and another a probability of 2, the second is twice as likely to be picked (and thus will be picked 67% of the time).")]
	[SerializeField]
	private ModelSwap[] modelSwaps;

	[Space]
	[Tooltip("Despawn the Shade after it has fled (fleed?) this many meters.")]
	[SerializeField]
	private float fleeDistanceToDespawn = 10f;

	[Tooltip("Flee away from the spotter at this many meters per second.")]
	[SerializeField]
	private float fleeSpeed;

	[Tooltip("The maximum strength the shade can move bob around in the horizontal and vertical axes, with final value chosen randomly.")]
	[SerializeField]
	private Vector2 fleeBobMagnitudeXYMax;

	[Tooltip("The maximum frequency the shade can move bob around in the horizontal and vertical axes, with final value chosen randomly.")]
	[SerializeField]
	private Vector2 fleeBobFrequencyXYMax;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private ParticleSystem spawnFX;

	[SerializeField]
	private AudioSource spawnAudioSource;

	[SerializeField]
	private AudioClip[] spawnAudioClips;

	[HideInInspector]
	public Vector3 pullVector;

	private Vector3 origin;

	private Vector3 fleeForward;

	private Vector3 fleeRight;

	private Vector3 fleeUp = Vector3.up;

	private Vector2 fleeBobFrequencyXY;

	private Vector2 fleeBobMagnitudeXY;

	private Vector3 trailingPosition;

	private float closestCatcherDistance;

	private int animatorProperty = Animator.StringToHash("Distance");

	public override void OnSpawn()
	{
		spawnFX.Play();
		spawnAudioSource.clip = spawnAudioClips.GetRandomItem();
		spawnAudioSource.GTPlay();
		pullVector = Vector3.zero;
	}

	public void SetFleePosition(Vector3 position, Vector3 fleeFrom)
	{
		origin = position;
		Vector3 vector = position - fleeFrom;
		fleeForward = vector.normalized;
		fleeRight = Vector3.Cross(fleeForward, Vector3.up);
		fleeUp = Vector3.Cross(fleeForward, fleeRight);
		trailingPosition = position + vector.normalized * 3f;
	}

	public override void SetRandomVariables()
	{
		float num = 0f;
		for (int i = 0; i < modelSwaps.Length; i++)
		{
			num += modelSwaps[i].relativeProbability;
			modelSwaps[i].gameObject.SetActive(value: false);
		}
		float num2 = UnityEngine.Random.value * num;
		for (int j = 0; j < modelSwaps.Length; j++)
		{
			if (num2 >= modelSwaps[j].relativeProbability)
			{
				num2 -= modelSwaps[j].relativeProbability;
				continue;
			}
			modelSwaps[j].gameObject.SetActive(value: true);
			break;
		}
		fleeBobFrequencyXY = new Vector2(UnityEngine.Random.Range(-1f, 1f) * fleeBobFrequencyXYMax.x, UnityEngine.Random.Range(-1f, 1f) * fleeBobFrequencyXYMax.y);
		fleeBobMagnitudeXY = new Vector2(UnityEngine.Random.Range(-1f, 1f) * fleeBobMagnitudeXYMax.x, UnityEngine.Random.Range(-1f, 1f) * fleeBobMagnitudeXYMax.y);
	}

	public override void Tick()
	{
		float num = (float)GetAliveTime();
		Vector3 vector = origin + num * fleeForward + pullVector + Mathf.Sin(fleeBobFrequencyXY.x * num) * fleeBobMagnitudeXY.x * fleeRight + Mathf.Sin(fleeBobFrequencyXY.y * num) * fleeBobMagnitudeXY.y * fleeUp;
		Quaternion rotation = Quaternion.LookRotation((vector - trailingPosition).normalized, Vector3.up);
		trailingPosition = Vector3.Lerp(trailingPosition, vector, 0.05f);
		base.transform.SetPositionAndRotation(vector, rotation);
		animator.SetFloat(animatorProperty, Mathf.Sin(num * 3f) * 0.5f + 0.5f);
	}
}
