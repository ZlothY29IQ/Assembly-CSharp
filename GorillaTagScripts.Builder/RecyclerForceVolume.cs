using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTagScripts.Builder;

public class RecyclerForceVolume : MonoBehaviour
{
	[SerializeField]
	public bool scaleWithSize = true;

	[SerializeField]
	private float accel;

	[SerializeField]
	private float maxDepth = -1f;

	[SerializeField]
	private float maxSpeed;

	[SerializeField]
	private bool disableGrip;

	[SerializeField]
	private bool dampenLateralVelocity = true;

	[SerializeField]
	private float dampenXVelPerc;

	[FormerlySerializedAs("dampenZVelPerc")]
	[SerializeField]
	private float dampenYVelPerc;

	[SerializeField]
	private bool applyPullToCenterAcceleration = true;

	[SerializeField]
	private float pullToCenterAccel;

	[SerializeField]
	private float pullToCenterMaxSpeed;

	[SerializeField]
	private float pullTOCenterMinDistance = 0.1f;

	private Collider volume;

	public GameObject windSFX;

	[SerializeField]
	private MeshRenderer windEffectRenderer;

	private bool hasWindFX;

	private Vector3 enterPos;

	private void Awake()
	{
		volume = GetComponent<Collider>();
		hasWindFX = windEffectRenderer != null;
		if (hasWindFX)
		{
			windEffectRenderer.enabled = false;
		}
	}

	private bool TriggerFilter(Collider other, out Rigidbody rb, out Transform xf)
	{
		rb = null;
		xf = null;
		if (other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			rb = GorillaTagger.Instance.GetComponent<Rigidbody>();
			xf = GorillaTagger.Instance.headCollider.GetComponent<Transform>();
		}
		if (rb != null)
		{
			return xf != null;
		}
		return false;
	}

	public void OnTriggerEnter(Collider other)
	{
		Rigidbody rb = null;
		Transform xf = null;
		if (TriggerFilter(other, out rb, out xf))
		{
			enterPos = xf.position;
			ObjectPools.instance.Instantiate(windSFX, enterPos);
			if (hasWindFX)
			{
				windEffectRenderer.transform.position = base.transform.position + Vector3.Dot(enterPos - base.transform.position, base.transform.right) * base.transform.right;
				windEffectRenderer.enabled = true;
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		Rigidbody rb = null;
		Transform xf = null;
		if (TriggerFilter(other, out rb, out xf) && hasWindFX)
		{
			windEffectRenderer.enabled = false;
		}
	}

	public void OnTriggerStay(Collider other)
	{
		Rigidbody rb = null;
		Transform xf = null;
		if (!TriggerFilter(other, out rb, out xf))
		{
			return;
		}
		if (disableGrip)
		{
			GTPlayer.Instance.SetMaximumSlipThisFrame();
		}
		SizeManager sizeManager = null;
		if (scaleWithSize)
		{
			sizeManager = rb.GetComponent<SizeManager>();
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (scaleWithSize && (bool)sizeManager)
		{
			linearVelocity /= sizeManager.currentScale;
		}
		Vector3 vector = Vector3.Dot(base.transform.position - xf.position, base.transform.up) * base.transform.up;
		float num = vector.magnitude + 0.0001f;
		Vector3 vector2 = vector / num;
		float num2 = Vector3.Dot(linearVelocity, vector2);
		float num3 = accel;
		if (maxDepth > -1f)
		{
			float num4 = Vector3.Dot(xf.position - enterPos, vector2);
			float num5 = maxDepth - num4;
			float b = 0f;
			if (num5 > 0.0001f)
			{
				b = num2 * num2 / num5;
			}
			num3 = Mathf.Max(accel, b);
		}
		float deltaTime = Time.deltaTime;
		Vector3 vector3 = base.transform.forward * num3 * deltaTime;
		linearVelocity += vector3;
		Vector3 vector4 = Vector3.Dot(linearVelocity, base.transform.up) * base.transform.up;
		Vector3 vector5 = Vector3.Dot(linearVelocity, base.transform.right) * base.transform.right;
		Vector3 vector6 = Mathf.Clamp(Vector3.Dot(linearVelocity, base.transform.forward), -1f * maxSpeed, maxSpeed) * base.transform.forward;
		float num6 = 1f;
		float num7 = 1f;
		if (dampenLateralVelocity)
		{
			num6 = 1f - dampenXVelPerc * 0.01f * deltaTime;
			num7 = 1f - dampenYVelPerc * 0.01f * deltaTime;
		}
		linearVelocity = num7 * vector4 + num6 * vector5 + vector6;
		if (applyPullToCenterAcceleration && pullToCenterAccel > 0f && pullToCenterMaxSpeed > 0f)
		{
			linearVelocity -= num2 * vector2;
			if (num > pullTOCenterMinDistance)
			{
				num2 += pullToCenterAccel * deltaTime;
				float num8 = Mathf.Min(pullToCenterMaxSpeed, num / deltaTime);
				num2 = Mathf.Clamp(num2, -1f * num8, num8);
			}
			else
			{
				num2 = 0f;
			}
			linearVelocity += num2 * vector2;
		}
		if (scaleWithSize && (bool)sizeManager)
		{
			linearVelocity *= sizeManager.currentScale;
		}
		rb.linearVelocity = linearVelocity;
	}
}
