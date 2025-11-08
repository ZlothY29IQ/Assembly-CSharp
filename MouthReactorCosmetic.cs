using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class MouthReactorCosmetic : MonoBehaviour, ITickSystemTick
{
	private static readonly Vector3 DEFAULT_OFFSET = new Vector3(0f, 0.0208f, 0.171f);

	private const float DEFAULT_RADIUS = 0.1666667f;

	[Tooltip("The transform to check against the mouth's position. Defaults to the transform this script is attached to.")]
	public Transform reactorTransform;

	[Tooltip("Offset the relative position of the reactor transform.")]
	public Vector3 reactorOffset = Vector3.zero;

	[Tooltip("How close the reactor needs to be to the mouth to trigger the event.")]
	public float reactorRadius = 0.1666667f;

	[Tooltip("The continuous value is the distance to the mouth. When inside the mouth radius, the value will always be 0.")]
	public ContinuousPropertyArray continuousProperties;

	[Tooltip("After the event fires, it must wait this many seconds before it fires again.")]
	public float eventRefireDelay = 0.6f;

	[Tooltip("After the event fires, prevent firing again until the reactor transform is moved outside the mouth and then back in.")]
	public bool mustExitBeforeRefire = true;

	public UnityEvent onInsideMouth;

	public Vector3 mouthOffset = DEFAULT_OFFSET;

	private VRRig myRig;

	private float lastInsideTime;

	private bool wasInside;

	private bool IsRadiusChanged => reactorRadius != 0.1666667f;

	private bool IsOffsetChanged => mouthOffset != DEFAULT_OFFSET;

	public bool TickRunning { get; set; }

	private void ResetReactorTransform()
	{
		if (reactorTransform == null)
		{
			reactorTransform = base.transform;
		}
	}

	private void ResetRadius()
	{
		reactorRadius = 0.1666667f;
	}

	private void ResetOffset()
	{
		mouthOffset = DEFAULT_OFFSET;
	}

	private void OnEnable()
	{
		if ((object)myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		Vector3 vector = myRig.head.rigTarget.TransformPoint(mouthOffset);
		float sqrMagnitude = (reactorTransform.TransformPoint(reactorOffset) - vector).sqrMagnitude;
		if (sqrMagnitude < reactorRadius * reactorRadius)
		{
			if ((!mustExitBeforeRefire || !wasInside) && Time.time - lastInsideTime >= eventRefireDelay)
			{
				onInsideMouth?.Invoke();
				lastInsideTime = Time.time;
			}
			wasInside = true;
		}
		else
		{
			wasInside = false;
		}
		if (continuousProperties.Count > 0)
		{
			continuousProperties.ApplyAll(Mathf.Min(0f, Mathf.Sqrt(sqrMagnitude) - reactorRadius));
		}
	}
}
