using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class DicePhysics : MonoBehaviour
{
	private enum DiceType
	{
		D6,
		D20
	}

	[Serializable]
	private struct CosmeticRollOverride
	{
		public string cosmeticName;

		public int landingSide;

		public bool requireHolding;
	}

	[SerializeField]
	private DiceType diceType = DiceType.D20;

	[SerializeField]
	private float landingTime = 5f;

	[SerializeField]
	private float postLandingTime = 2f;

	[SerializeField]
	private LayerMask surfaceLayers;

	[SerializeField]
	private AnimationCurve angleDeltaVsStrengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve landingTimeVsStrengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float strength = 1f;

	[SerializeField]
	private float damping = 0.5f;

	[SerializeField]
	private bool forceLandingSide;

	[SerializeField]
	private int forcedLandingSide = 20;

	[SerializeField]
	private bool allowPickupFromGround = true;

	[SerializeField]
	private float bounceAmplification = 1f;

	[SerializeField]
	private CosmeticRollOverride[] cosmeticRollOverrides;

	[SerializeField]
	private UnityEvent onBestRoll;

	[SerializeField]
	private UnityEvent onWorstRoll;

	[SerializeField]
	private UnityEvent onRollFinished;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private InteractionPoint interactionPoint;

	private VRRig cachedLocalRig;

	private DiceHoldable holdableParent;

	private double throwStartTime = -1.0;

	private double throwSettledTime = -1.0;

	private int landingSide;

	private float scale;

	private Vector3 prevVelocity = Vector3.zero;

	private Vector3 velocity = Vector3.zero;

	private const float a = 38.833332f;

	private const float b = 77.66666f;

	private Vector3[] d20SideDirections = new Vector3[20]
	{
		Quaternion.AngleAxis(144f, Vector3.up) * Quaternion.AngleAxis(38.833332f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(324f, -Vector3.up) * Quaternion.AngleAxis(38.833332f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(288f, Vector3.up) * Quaternion.AngleAxis(38.833332f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(180f, -Vector3.up) * Quaternion.AngleAxis(38.833332f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(252f, -Vector3.up) * Quaternion.AngleAxis(77.66666f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(108f, -Vector3.up) * Quaternion.AngleAxis(77.66666f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(72f, Vector3.up) * Quaternion.AngleAxis(38.833332f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(36f, -Vector3.up) * Quaternion.AngleAxis(77.66666f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(216f, Vector3.up) * Quaternion.AngleAxis(77.66666f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(0f, Vector3.up) * Quaternion.AngleAxis(77.66666f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(180f, -Vector3.up) * Quaternion.AngleAxis(77.66666f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(324f, -Vector3.up) * Quaternion.AngleAxis(77.66666f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(144f, Vector3.up) * Quaternion.AngleAxis(77.66666f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(108f, -Vector3.up) * Quaternion.AngleAxis(38.833332f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(72f, Vector3.up) * Quaternion.AngleAxis(77.66666f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(288f, Vector3.up) * Quaternion.AngleAxis(77.66666f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(0f, Vector3.up) * Quaternion.AngleAxis(38.833332f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(252f, -Vector3.up) * Quaternion.AngleAxis(38.833332f, -Vector3.forward) * Vector3.up,
		Quaternion.AngleAxis(216f, Vector3.up) * Quaternion.AngleAxis(38.833332f, Vector3.forward) * -Vector3.up,
		Quaternion.AngleAxis(36f, -Vector3.up) * Quaternion.AngleAxis(38.833332f, -Vector3.forward) * Vector3.up
	};

	private Vector3[] d6SideDirections = new Vector3[6]
	{
		new Vector3(0f, -1f, 0f),
		new Vector3(-1f, 0f, 0f),
		new Vector3(0f, 0f, -1f),
		new Vector3(0f, 0f, 1f),
		new Vector3(1f, 0f, 0f),
		new Vector3(0f, 1f, 0f)
	};

	public int GetRandomSide()
	{
		if (diceType != DiceType.D6)
		{
			_ = 1;
			if (forceLandingSide)
			{
				return Mathf.Clamp(forcedLandingSide, 1, 20);
			}
			if (CheckCosmeticRollOverride(out var rollSide))
			{
				return Mathf.Clamp(rollSide, 1, 20);
			}
			return UnityEngine.Random.Range(1, 21);
		}
		if (forceLandingSide)
		{
			return Mathf.Clamp(forcedLandingSide, 1, 6);
		}
		if (CheckCosmeticRollOverride(out var rollSide2))
		{
			return Mathf.Clamp(rollSide2, 1, 6);
		}
		return UnityEngine.Random.Range(1, 7);
	}

	public Vector3 GetSideDirection(int side)
	{
		if (diceType != DiceType.D6)
		{
			_ = 1;
			int num = Mathf.Clamp(side - 1, 0, 19);
			return d20SideDirections[num];
		}
		int num2 = Mathf.Clamp(side - 1, 0, 5);
		return d6SideDirections[num2];
	}

	public void StartThrow(DiceHoldable holdable, Vector3 startPosition, Vector3 velocity, float playerScale, int side, double startTime)
	{
		holdableParent = holdable;
		base.transform.parent = null;
		base.transform.position = startPosition;
		base.transform.localScale = Vector3.one * playerScale;
		rb.isKinematic = false;
		rb.useGravity = true;
		rb.linearVelocity = velocity;
		if (!allowPickupFromGround && interactionPoint != null)
		{
			interactionPoint.enabled = false;
		}
		throwStartTime = ((startTime > 0.0) ? startTime : ((double)Time.time));
		throwSettledTime = -1.0;
		scale = playerScale;
		landingSide = Mathf.Clamp(side, 1, 20);
		prevVelocity = Vector3.zero;
		velocity = Vector3.zero;
		base.enabled = true;
	}

	public void EndThrow()
	{
		rb.isKinematic = true;
		rb.linearVelocity = Vector3.zero;
		if (holdableParent != null)
		{
			base.transform.parent = holdableParent.transform;
		}
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		scale = 1f;
		throwStartTime = -1.0;
		if (interactionPoint != null)
		{
			interactionPoint.enabled = true;
		}
		onRollFinished.Invoke();
		base.enabled = false;
	}

	private void FixedUpdate()
	{
		double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		float num2 = (float)(num - throwStartTime);
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 0.1f * scale, surfaceLayers.value, QueryTriggerInteraction.Ignore))
		{
			Vector3 normal = hitInfo.normal;
			Vector3 sideDirection = GetSideDirection(landingSide);
			Vector3 vector = base.transform.rotation * sideDirection;
			Vector3 normalized = Vector3.Cross(vector, normal).normalized;
			float num3 = Vector3.SignedAngle(vector, normal, normalized);
			float num4 = Mathf.Sign(num3) * angleDeltaVsStrengthCurve.Evaluate(Mathf.Clamp01(Mathf.Abs(num3) / 180f));
			float num5 = landingTimeVsStrengthCurve.Evaluate(Mathf.Clamp01(num2 / landingTime));
			float magnitude = rb.linearVelocity.magnitude;
			float num6 = Mathf.Clamp01(1f - Mathf.Min(magnitude, 1f));
			float num7 = Mathf.Max(num5, num6);
			Vector3 torque = strength * (num7 * num4 * normalized) - damping * rb.angularVelocity;
			rb.AddTorque(torque, ForceMode.Acceleration);
			if (!rb.isKinematic && magnitude < 0.01f && num3 < 2f)
			{
				rb.isKinematic = true;
				throwSettledTime = num;
				InvokeLandingEffects(landingSide);
			}
			else if (!rb.isKinematic && num2 > landingTime)
			{
				rb.isKinematic = true;
				throwSettledTime = num;
				base.transform.rotation = Quaternion.FromToRotation(vector, normal) * base.transform.rotation;
				InvokeLandingEffects(landingSide);
			}
		}
		if (num2 > landingTime + postLandingTime || (rb.isKinematic && (float)(num - throwSettledTime) > postLandingTime))
		{
			EndThrow();
		}
		prevVelocity = velocity;
		velocity = rb.linearVelocity;
	}

	private void OnCollisionEnter(Collision collision)
	{
		float magnitude = collision.impulse.magnitude;
		if (magnitude > 0.001f)
		{
			Vector3 vector = Vector3.Reflect(prevVelocity, collision.impulse / magnitude);
			rb.linearVelocity = vector * bounceAmplification;
		}
	}

	private void InvokeLandingEffects(int side)
	{
		if (diceType != DiceType.D6)
		{
			_ = 1;
			switch (side)
			{
			case 20:
				onBestRoll.Invoke();
				break;
			case 1:
				onWorstRoll.Invoke();
				break;
			}
		}
		else
		{
			switch (side)
			{
			case 6:
				onBestRoll.Invoke();
				break;
			case 1:
				onWorstRoll.Invoke();
				break;
			}
		}
	}

	private bool CheckCosmeticRollOverride(out int rollSide)
	{
		if (cosmeticRollOverrides.Length != 0)
		{
			if (cachedLocalRig == null)
			{
				if (PhotonNetwork.InRoom && VRRigCache.Instance.TryGetVrrig(PhotonNetwork.LocalPlayer, out var playerRig) && playerRig.Rig != null)
				{
					cachedLocalRig = playerRig.Rig;
				}
				else
				{
					cachedLocalRig = GorillaTagger.Instance.offlineVRRig;
				}
			}
			if (cachedLocalRig != null)
			{
				int num = -1;
				for (int i = 0; i < cosmeticRollOverrides.Length; i++)
				{
					if (cosmeticRollOverrides[i].cosmeticName != null && cachedLocalRig.cosmeticSet != null && cachedLocalRig.cosmeticSet.HasItem(cosmeticRollOverrides[i].cosmeticName) && (!cosmeticRollOverrides[i].requireHolding || (EquipmentInteractor.instance.leftHandHeldEquipment != null && EquipmentInteractor.instance.leftHandHeldEquipment.name == cosmeticRollOverrides[i].cosmeticName) || (EquipmentInteractor.instance.rightHandHeldEquipment != null && EquipmentInteractor.instance.rightHandHeldEquipment.name == cosmeticRollOverrides[i].cosmeticName)) && cosmeticRollOverrides[i].landingSide > num)
					{
						num = cosmeticRollOverrides[i].landingSide;
					}
				}
				if (num > 0)
				{
					rollSide = num;
					return true;
				}
			}
		}
		rollSide = 0;
		return false;
	}
}
