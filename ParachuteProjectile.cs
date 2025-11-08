using GorillaTag.Cosmetics;
using UnityEngine;

public class ParachuteProjectile : MonoBehaviour, IProjectile, ITickSystemTick
{
	[SerializeField]
	private MeshFilter monkeMeshFilter;

	[SerializeField]
	private GameObject parachute;

	[SerializeField]
	private Mesh launchMesh;

	[SerializeField]
	private Mesh parachutingMesh;

	[SerializeField]
	private Mesh landedMesh;

	[Tooltip("time to wait after launch before deploying the parachute")]
	[SerializeField]
	private float parachuteDeployDelay = 1f;

	[Tooltip("time to wait after landing before destroying")]
	[SerializeField]
	private float destroyOnLandDelay = 3f;

	[Tooltip("How far from the collision point should the projectile sit when landed")]
	[SerializeField]
	private float groundOffset;

	[Tooltip("Acceptable angle in degrees of surface from world up to be considered the ground")]
	[SerializeField]
	private float groudUpThreshold = 45f;

	[Tooltip("Drag before the parachute is deployed.")]
	[SerializeField]
	private float initialDrag;

	[Tooltip("Drag before the parachute is deployed.")]
	[SerializeField]
	private float initialAngularDrag = 0.05f;

	[Tooltip("Drag after the parachute is deployed.")]
	[SerializeField]
	private float parachuteDrag = 5f;

	[Tooltip("Drag after the parachute is deployed.")]
	[SerializeField]
	private float parachuteAngularDrag = 10f;

	[SerializeField]
	private GameObject impactEffect;

	[SerializeField]
	private float impactEffectScaleMultiplier = 1f;

	[Tooltip("Distance from the surface that the particle should spawn.")]
	[SerializeField]
	private float impactEffectOffset;

	private Rigidbody rb;

	private bool launched;

	private float launchedTime;

	private float landTime;

	private float peakTime = float.MaxValue;

	private bool parachuteDeployed;

	private bool landed;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void OnEnable()
	{
		launched = false;
		landTime = 0f;
		launchedTime = 0f;
		peakTime = float.MaxValue;
		monkeMeshFilter.mesh = launchMesh;
		parachute.SetActive(value: false);
		if (!TickRunning)
		{
			TickSystem<object>.AddCallbackTarget(this);
		}
	}

	private void OnDisable()
	{
		launched = false;
		if (TickRunning)
		{
			TickSystem<object>.RemoveCallbackTarget(this);
		}
	}

	public void Launch(Vector3 startPosition, Quaternion startRotation, Vector3 velocity, float chargeFrac, VRRig ownerRig, int progress)
	{
		parachuteDeployed = false;
		landed = false;
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		rb.position = startPosition;
		rb.rotation = startRotation;
		ChangeUp(Vector3.up);
		rb.freezeRotation = true;
		if (ownerRig == null)
		{
			base.transform.localScale = Vector3.one;
		}
		else
		{
			base.transform.localScale = Vector3.one * ownerRig.scaleFactor;
		}
		rb.isKinematic = false;
		rb.linearVelocity = velocity;
		rb.linearDamping = initialDrag;
		rb.angularDamping = initialAngularDrag;
		launchedTime = Time.time;
		monkeMeshFilter.mesh = launchMesh;
		parachute.SetActive(value: false);
		if (velocity.y > 0f)
		{
			peakTime = velocity.y / (-1f * Physics.gravity.y);
		}
		else
		{
			peakTime = 0f;
		}
		launched = true;
	}

	private void OnPeakReached()
	{
		parachuteDeployed = true;
		parachute.SetActive(value: true);
		monkeMeshFilter.mesh = parachutingMesh;
		ChangeUp(Vector3.up);
		rb.linearDamping = parachuteDrag;
		rb.angularDamping = parachuteAngularDrag;
	}

	private void OnLanded(Collision collision)
	{
		landTime = Time.time;
		landed = true;
		ContactPoint contact = collision.GetContact(0);
		rb.isKinematic = true;
		rb.position = contact.point + contact.normal * (groundOffset * base.transform.localScale.x);
		ChangeUp(contact.normal);
		monkeMeshFilter.mesh = landedMesh;
		parachute.SetActive(value: false);
	}

	private void ChangeUp(Vector3 newUp)
	{
		Vector3 forward = Vector3.Cross(rb.transform.right, newUp);
		if (forward.sqrMagnitude < float.Epsilon)
		{
			forward = Vector3.Cross(Vector3.Cross(newUp, rb.transform.forward), newUp);
		}
		rb.rotation = Quaternion.LookRotation(forward, newUp);
	}

	private void PlayImpactEffects(Vector3 position, Vector3 normal)
	{
		if (impactEffect != null)
		{
			Vector3 position2 = position + impactEffectOffset * normal;
			GameObject obj = ObjectPools.instance.Instantiate(impactEffect, position2);
			obj.transform.localScale = base.transform.localScale * impactEffectScaleMultiplier;
			obj.transform.up = normal;
		}
		ObjectPools.instance.Destroy(base.gameObject);
	}

	public void OnTriggerEvent(bool isLeft, Collider col)
	{
		if (parachuteDeployed)
		{
			PlayImpactEffects(base.transform.position, Vector3.up);
			GorillaTriggerColliderHandIndicator componentInParent = col.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
			if (componentInParent != null)
			{
				float amplitude = GorillaTagger.Instance.tapHapticStrength / 2f;
				float fixedDeltaTime = Time.fixedDeltaTime;
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, amplitude, fixedDeltaTime);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (launched && !landed)
		{
			ContactPoint contact = collision.GetContact(0);
			if (collision.collider.attachedRigidbody != null)
			{
				PlayImpactEffects(contact.point, contact.normal);
			}
			else if (collision.collider.gameObject.IsOnLayer(UnityLayer.GorillaThrowable))
			{
				PlayImpactEffects(contact.point, contact.normal);
			}
			else if (!parachuteDeployed)
			{
				PlayImpactEffects(contact.point, contact.normal);
			}
			else if (Vector3.Angle(contact.normal, Vector3.up) < groudUpThreshold)
			{
				OnLanded(collision);
			}
			else
			{
				PlayImpactEffects(contact.point, contact.normal);
			}
		}
	}

	public void Tick()
	{
		if (!parachuteDeployed && Time.time > launchedTime + parachuteDeployDelay && Time.time >= launchedTime + peakTime)
		{
			OnPeakReached();
		}
		if (landed && Time.time > landTime + destroyOnLandDelay)
		{
			PlayImpactEffects(base.transform.position, base.transform.up);
		}
	}
}
