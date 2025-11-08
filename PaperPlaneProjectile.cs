using System;
using GorillaTag.Reactions;
using UnityEngine;
using UnityEngine.Events;

public class PaperPlaneProjectile : MonoBehaviour
{
	public delegate void PaperPlaneHit(Vector3 endPoint);

	private const float speedScaleRatio = 0.7f;

	[NonSerialized]
	[Space]
	private float _timeElapsed;

	[NonSerialized]
	private float _speed;

	[NonSerialized]
	private Vector3 _direction;

	[NonSerialized]
	private bool _stopped;

	private Transform _tCached;

	private SpawnWorldEffects spawnWorldEffects;

	private Vector3 nextPos;

	private RaycastHit[] results = new RaycastHit[1];

	[Tooltip("Maximum lifetime in seconds for the projectile")]
	[SerializeField]
	private float maxFlightTime = 7.5f;

	[Tooltip("Collisions are ignored for minFlightTime seconds after launch")]
	[SerializeField]
	private float minFlightTime = 0.5f;

	[Tooltip("Hand speed to projectile launch Speed")]
	[SerializeField]
	private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(6.324555f, 20f, 6.324555f, 6.324555f));

	[Tooltip("maximum speed of launched projectile (clamped after applying speed curve)")]
	[SerializeField]
	private float maxSpeed = 10f;

	[Tooltip("minimum speed of launched projectile (clamped after applying speed curve)")]
	[SerializeField]
	private float minSpeed = 1f;

	[SerializeField]
	private bool enableRotation;

	[Tooltip("Objects enabled when launched and disabled on Hit")]
	[SerializeField]
	private GameObject flyingObject;

	[Tooltip("Objects disabled when launched and enabled on Hit")]
	[SerializeField]
	private GameObject crashingObject;

	[Tooltip("Layers the projectile collides with")]
	[SerializeField]
	private LayerMask layerMask;

	[SerializeField]
	private bool useTransferrableObjectState;

	[SerializeField]
	protected UnityEvent OnResetProjectileState;

	[SerializeField]
	protected string boolADebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolATrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolAFalse;

	[SerializeField]
	protected string boolBDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolBTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolBFalse;

	[SerializeField]
	protected string boolCDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolCTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolCFalse;

	[SerializeField]
	protected string boolDDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolDTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolDFalse;

	[SerializeField]
	protected UnityEvent<int> OnItemStateIntChanged;

	private VRRig myRig;

	private float scaleFactor;

	public new Transform transform => _tCached;

	public VRRig MyRig => myRig;

	public event PaperPlaneHit OnHit;

	private void Awake()
	{
		_tCached = base.transform;
		spawnWorldEffects = GetComponent<SpawnWorldEffects>();
	}

	private void Start()
	{
		ResetProjectile();
	}

	public void ResetProjectile()
	{
		_timeElapsed = 0f;
		flyingObject.SetActive(value: true);
		crashingObject.SetActive(value: false);
	}

	internal void SetTransferrableState(TransferrableObject.SyncOptions syncType, int state)
	{
		if (!useTransferrableObjectState)
		{
			return;
		}
		switch (syncType)
		{
		case TransferrableObject.SyncOptions.Bool:
		{
			bool num = (state & 1) != 0;
			bool flag = (state & 2) != 0;
			bool flag2 = (state & 4) != 0;
			bool flag3 = (state & 8) != 0;
			if (num)
			{
				OnItemStateBoolATrue?.Invoke();
			}
			else
			{
				OnItemStateBoolAFalse?.Invoke();
			}
			if (flag)
			{
				OnItemStateBoolBTrue?.Invoke();
			}
			else
			{
				OnItemStateBoolBFalse?.Invoke();
			}
			if (flag2)
			{
				OnItemStateBoolCTrue?.Invoke();
			}
			else
			{
				OnItemStateBoolCFalse?.Invoke();
			}
			if (flag3)
			{
				OnItemStateBoolDTrue?.Invoke();
			}
			else
			{
				OnItemStateBoolDFalse?.Invoke();
			}
			break;
		}
		case TransferrableObject.SyncOptions.Int:
			OnItemStateIntChanged?.Invoke(state);
			break;
		}
	}

	public void Launch(Vector3 startPos, Quaternion startRot, Vector3 vel)
	{
		base.gameObject.SetActive(value: true);
		ResetProjectile();
		transform.position = startPos;
		if (enableRotation)
		{
			transform.rotation = startRot;
		}
		else
		{
			transform.LookAt(transform.position + vel.normalized);
		}
		_direction = vel.normalized;
		_speed = Mathf.Clamp(speedCurve.Evaluate(vel.magnitude), minSpeed, maxSpeed);
		_stopped = false;
		scaleFactor = 0.7f * (transform.lossyScale.x - 1f + 1.4285715f);
	}

	private void Update()
	{
		if (_stopped)
		{
			if (!crashingObject.gameObject.activeSelf)
			{
				if ((bool)ObjectPools.instance)
				{
					ObjectPools.instance.Destroy(base.gameObject);
				}
				else
				{
					base.gameObject.SetActive(value: false);
				}
			}
			return;
		}
		_timeElapsed += Time.deltaTime;
		nextPos = transform.position + _direction * _speed * Time.deltaTime * scaleFactor;
		if (_timeElapsed < maxFlightTime && (_timeElapsed < minFlightTime || Physics.RaycastNonAlloc(transform.position, nextPos - transform.position, results, Vector3.Distance(transform.position, nextPos), layerMask.value) == 0))
		{
			transform.position = nextPos;
			transform.Rotate(Mathf.Sin(_timeElapsed) * 10f * Time.deltaTime, 0f, 0f);
			return;
		}
		if (_timeElapsed < maxFlightTime)
		{
			if (results[0].collider.TryGetComponent<SlingshotProjectileHitNotifier>(out var component))
			{
				component.InvokeHit(this, results[0].collider);
			}
			if (spawnWorldEffects != null)
			{
				spawnWorldEffects.RequestSpawn(nextPos);
			}
		}
		_stopped = true;
		_timeElapsed = 0f;
		this.OnHit?.Invoke(nextPos);
		this.OnHit = null;
		flyingObject.SetActive(value: false);
		crashingObject.SetActive(value: true);
	}

	internal void SetVRRig(VRRig rig)
	{
		myRig = rig;
	}

	private void OnDisable()
	{
		if (useTransferrableObjectState)
		{
			OnResetProjectileState?.Invoke();
		}
	}
}
