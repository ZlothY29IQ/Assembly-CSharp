using UnityEngine;

[RequireComponent(typeof(SIGadgetBlasterProjectile))]
public class SIGadgetProjectileStretchVisuals : MonoBehaviourTick
{
	private SIGadgetBlasterProjectile projectile;

	public GameObject baseVisuals;

	public Transform frontStretch;

	public Transform rearStretch;

	public float framesPerPosition;

	private float totalLength;

	private float distancePerFrame;

	private float maxStretchRatio;

	private bool maxSizeReached;

	private float frontDistance;

	private float timeSpawned;

	public new void OnEnable()
	{
		base.OnEnable();
		projectile = GetComponent<SIGadgetBlasterProjectile>();
		totalLength = (frontStretch.position - rearStretch.position).magnitude;
		distancePerFrame = projectile.startingVelocity * Time.fixedDeltaTime;
		maxStretchRatio = distancePerFrame / totalLength * framesPerPosition;
		timeSpawned = Time.time;
		maxSizeReached = false;
		baseVisuals.transform.localPosition = new Vector3(0f, 0f, 0f);
		baseVisuals.transform.localScale = new Vector3(1f, 1f, 1f);
		frontDistance = (frontStretch.position - base.transform.position).magnitude;
	}

	public override void Tick()
	{
		if (!maxSizeReached)
		{
			float num = (Time.time - timeSpawned) * projectile.startingVelocity / totalLength / 2f + 1f;
			if (num >= maxStretchRatio)
			{
				num = maxStretchRatio;
				maxSizeReached = true;
			}
			baseVisuals.transform.localPosition = new Vector3(0f, 0f, (0f - (num - 1f)) * frontDistance);
			baseVisuals.transform.localScale = new Vector3(1f, 1f, num);
		}
	}
}
