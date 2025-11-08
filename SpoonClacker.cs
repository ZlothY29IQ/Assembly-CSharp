using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SpoonClacker : MonoBehaviour
{
	public TransferrableObject transferObject;

	public SkinnedMeshRenderer skinnedMesh;

	public HingeJoint hingeJoint;

	public int targetBlendShape;

	public float hingeMin;

	public float hingeMax;

	public bool invertOut;

	public float minThreshold = 0.01f;

	public float maxThreshold = 0.01f;

	public float hysterisisFactor = 4f;

	public UnityEvent OnHitMin;

	public UnityEvent OnHitMax;

	private bool _lockMin;

	private bool _lockMax;

	public SoundBankPlayer soundsSingle;

	public SoundBankPlayer soundsMulti;

	private TimeSince _sincelastHit;

	[FormerlySerializedAs("multiHitInterval")]
	public float multiHitCutoff = 0.1f;

	private void Awake()
	{
		Setup();
	}

	private void Setup()
	{
		JointLimits limits = hingeJoint.limits;
		hingeMin = limits.min;
		hingeMax = limits.max;
	}

	private void Update()
	{
		if (!transferObject)
		{
			return;
		}
		TransferrableObject.PositionState currentState = transferObject.currentState;
		if (currentState != TransferrableObject.PositionState.InLeftHand && currentState != TransferrableObject.PositionState.InRightHand)
		{
			return;
		}
		float num = MathUtils.Linear(hingeJoint.angle, hingeMin, hingeMax, 0f, 1f);
		float value = (invertOut ? (1f - num) : num) * 100f;
		skinnedMesh.SetBlendShapeWeight(targetBlendShape, value);
		if (!_lockMin && num <= minThreshold)
		{
			OnHitMin.Invoke();
			_lockMin = true;
		}
		else if (!_lockMax && num >= 1f - maxThreshold)
		{
			OnHitMax.Invoke();
			_lockMax = true;
			if (_sincelastHit.HasElapsed(multiHitCutoff, resetOnElapsed: true))
			{
				soundsSingle.Play();
			}
			else
			{
				soundsMulti.Play();
			}
		}
		if (_lockMin && num > minThreshold * hysterisisFactor)
		{
			_lockMin = false;
		}
		if (_lockMax && num < 1f - maxThreshold * hysterisisFactor)
		{
			_lockMax = false;
		}
	}
}
