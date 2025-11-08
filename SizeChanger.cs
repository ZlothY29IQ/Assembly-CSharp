using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SizeChanger : GorillaTriggerBox
{
	public enum ChangerType
	{
		Static,
		Continuous,
		Radius
	}

	[SerializeField]
	private ChangerType myType;

	[SerializeField]
	private float staticEasing;

	[SerializeField]
	private float maxScale;

	[SerializeField]
	private float minScale;

	private Collider myCollider;

	[SerializeField]
	private Transform startPos;

	[SerializeField]
	private Transform endPos;

	[SerializeField]
	private SizeChangerTrigger enterTrigger;

	[SerializeField]
	private SizeChangerTrigger exitTrigger;

	[SerializeField]
	private Transform scaleAwayFromPoint;

	[SerializeField]
	private SizeChangerTrigger exitOnEnterTrigger;

	public bool alwaysControlWhenEntered;

	public int priority;

	public bool aprilFoolsEnabled;

	public float startRadius;

	public float endRadius;

	public bool affectLayerA = true;

	public bool affectLayerB = true;

	public bool affectLayerC = true;

	public bool affectLayerD = true;

	public UnityAction OnExit;

	public UnityAction OnEnter;

	private HashSet<VRRig> unregisteredPresentRigs;

	public int SizeLayerMask
	{
		get
		{
			int num = 0;
			if (affectLayerA)
			{
				num |= 1;
			}
			if (affectLayerB)
			{
				num |= 2;
			}
			if (affectLayerC)
			{
				num |= 4;
			}
			if (affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}

	public ChangerType MyType => myType;

	public float MaxScale => maxScale;

	public float MinScale => minScale;

	public Transform StartPos => startPos;

	public Transform EndPos => endPos;

	public float StaticEasing => staticEasing;

	private void Awake()
	{
		minScale = Mathf.Max(minScale, 0.01f);
		myCollider = GetComponent<Collider>();
	}

	public void OnEnable()
	{
		if ((bool)enterTrigger)
		{
			enterTrigger.OnEnter += OnTriggerEnter;
		}
		if ((bool)exitTrigger)
		{
			exitTrigger.OnExit += OnTriggerExit;
		}
		if ((bool)exitOnEnterTrigger)
		{
			exitOnEnterTrigger.OnEnter += OnTriggerExit;
		}
	}

	public void OnDisable()
	{
		if ((bool)enterTrigger)
		{
			enterTrigger.OnEnter -= OnTriggerEnter;
		}
		if ((bool)exitTrigger)
		{
			exitTrigger.OnExit -= OnTriggerExit;
		}
		if ((bool)exitOnEnterTrigger)
		{
			exitOnEnterTrigger.OnEnter -= OnTriggerExit;
		}
	}

	public void AddEnterTrigger(SizeChangerTrigger trigger)
	{
		if ((bool)trigger)
		{
			trigger.OnEnter += OnTriggerEnter;
		}
	}

	public void RemoveEnterTrigger(SizeChangerTrigger trigger)
	{
		if ((bool)trigger)
		{
			trigger.OnEnter -= OnTriggerEnter;
		}
	}

	public void AddExitOnEnterTrigger(SizeChangerTrigger trigger)
	{
		if ((bool)trigger)
		{
			trigger.OnEnter += OnTriggerExit;
		}
	}

	public void RemoveExitOnEnterTrigger(SizeChangerTrigger trigger)
	{
		if ((bool)trigger)
		{
			trigger.OnEnter -= OnTriggerExit;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null))
			{
				acceptRig(component);
			}
		}
	}

	public void acceptRig(VRRig rig)
	{
		if (!rig.sizeManager.touchingChangers.Contains(this))
		{
			rig.sizeManager.touchingChangers.Add(this);
		}
		OnEnter?.Invoke();
	}

	public void OnTriggerExit(Collider other)
	{
		if ((bool)other.GetComponent<SphereCollider>())
		{
			VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
			if (!(component == null))
			{
				unacceptRig(component);
			}
		}
	}

	public void unacceptRig(VRRig rig)
	{
		rig.sizeManager.touchingChangers.Remove(this);
		OnExit?.Invoke();
	}

	public Vector3 ClosestPoint(Vector3 position)
	{
		if ((bool)enterTrigger && (bool)exitTrigger)
		{
			Vector3 vector = enterTrigger.ClosestPoint(position);
			Vector3 vector2 = exitTrigger.ClosestPoint(position);
			if (!(Vector3.Distance(position, vector) < Vector3.Distance(position, vector2)))
			{
				return vector2;
			}
			return vector;
		}
		if ((bool)myCollider)
		{
			return myCollider.ClosestPoint(position);
		}
		return position;
	}

	public void SetScaleCenterPoint(Transform centerPoint)
	{
		scaleAwayFromPoint = centerPoint;
	}

	public bool TryGetScaleCenterPoint(out Vector3 centerPoint)
	{
		if (scaleAwayFromPoint != null)
		{
			centerPoint = scaleAwayFromPoint.position;
			return true;
		}
		centerPoint = Vector3.zero;
		return false;
	}
}
