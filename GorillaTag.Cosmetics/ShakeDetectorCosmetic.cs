using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class ShakeDetectorCosmetic : MonoBehaviour
{
	[SerializeField]
	private TransferrableObject parentTransferrable;

	[Tooltip("for velocity equal or above this, we fire a Shake Start event")]
	[SerializeField]
	private float shakeStartVelocityThreshold;

	[Tooltip("for velocity under this, we fire a Shake End event")]
	[SerializeField]
	private float shakeEndVelocityThreshold;

	[Tooltip("cooldown starts when shaking ends")]
	[SerializeField]
	private float cooldown;

	[Tooltip("Use for clamping hand velocity value")]
	[SerializeField]
	private float maxHandVelocity = 20f;

	[FormerlySerializedAs("onShakeStart")]
	public UnityEvent<bool, float> onShakeStartLocal;

	[FormerlySerializedAs("onShakeEnd")]
	public UnityEvent<bool, float> onShakeEndLocal;

	private bool isShaking;

	private float shakeEndTime;

	private bool isLeftHand;

	public Vector3 HandVelocity { get; private set; }

	private void Awake()
	{
		HandVelocity = Vector3.zero;
		shakeEndTime = 0f;
	}

	private void UpdateShakeVelocity()
	{
		if ((bool)parentTransferrable)
		{
			if (!parentTransferrable.InHand())
			{
				HandVelocity = Vector3.zero;
			}
			else if (parentTransferrable.IsMyItem())
			{
				isLeftHand = parentTransferrable.InLeftHand();
				HandVelocity = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand).GetAverageVelocity(worldSpace: true);
				HandVelocity = Vector3.ClampMagnitude(HandVelocity, maxHandVelocity);
			}
		}
	}

	public void Update()
	{
		UpdateShakeVelocity();
		if (Time.time - shakeEndTime > cooldown && !isShaking && HandVelocity.magnitude >= shakeStartVelocityThreshold)
		{
			onShakeStartLocal?.Invoke(isLeftHand, HandVelocity.magnitude);
			isShaking = true;
		}
		if (isShaking && HandVelocity.magnitude < shakeEndVelocityThreshold)
		{
			onShakeEndLocal?.Invoke(isLeftHand, HandVelocity.magnitude);
			isShaking = false;
			shakeEndTime = Time.time;
		}
	}
}
