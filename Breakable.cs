using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class Breakable : MonoBehaviour
{
	[SerializeField]
	private Collider _collider;

	[SerializeField]
	private Rigidbody _rigidbody;

	[SerializeField]
	private GameObject rendererRoot;

	[SerializeField]
	private Renderer[] _renderers = new Renderer[0];

	[Space]
	[SerializeField]
	private ParticleSystem _breakEffect;

	[SerializeField]
	private UnityLayerMask _physicsMask = UnityLayerMask.GorillaHand;

	public UnityEvent<Breakable> onSpawn;

	public UnityEvent<Breakable> onBreak;

	public UnityEvent<Breakable> onReset;

	public float canBreakDelay = 1f;

	[SerializeField]
	private PhotonSignal<int> _breakSignal = "_breakSignal";

	[SerializeField]
	private CallLimiter m_spamChecker = new CallLimiter(2, 1f);

	[NonSerialized]
	[Space]
	private bool _broken;

	private bool m_useGravity = true;

	private float startTime;

	private float endTime;

	private void Awake()
	{
		_breakSignal.OnSignal += BreakRPC;
		if (_rigidbody.IsNotNull())
		{
			m_useGravity = _rigidbody.useGravity;
		}
	}

	private void BreakRPC(int owner, PhotonSignalInfo info)
	{
		VRRig vRRig = GetComponent<OwnerRig>();
		if (!(vRRig == null) && vRRig.OwningNetPlayer.ActorNumber == owner && m_spamChecker.CheckCallTime(Time.unscaledTime))
		{
			OnBreak(callback: true, signal: false);
		}
	}

	private void Setup()
	{
		if (_collider == null)
		{
			this.GetOrAddComponent<SphereCollider>(out var result);
			_collider = result;
		}
		_collider.enabled = true;
		if (_rigidbody == null)
		{
			this.GetOrAddComponent<Rigidbody>(out _rigidbody);
		}
		_rigidbody.isKinematic = false;
		_rigidbody.useGravity = false;
		_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
		UpdatePhysMasks();
		if (rendererRoot == null)
		{
			_renderers = GetComponentsInChildren<Renderer>();
		}
		else
		{
			_renderers = rendererRoot.GetComponentsInChildren<Renderer>();
		}
	}

	private void OnCollisionEnter(Collision col)
	{
		OnBreak();
	}

	private void OnCollisionStay(Collision col)
	{
		OnBreak();
	}

	private void OnTriggerEnter(Collider col)
	{
		OnBreak();
	}

	private void OnTriggerStay(Collider col)
	{
		OnBreak();
	}

	private void OnEnable()
	{
		_breakSignal.Enable();
		_broken = false;
		OnSpawn();
	}

	private void OnDisable()
	{
		_breakSignal.Disable();
		_broken = false;
		OnReset(callback: false);
		ShowRenderers(visible: false);
	}

	public void Break()
	{
		OnBreak();
	}

	public void Reset()
	{
		OnReset();
	}

	protected virtual void ShowRenderers(bool visible)
	{
		if (_renderers.IsNullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < _renderers.Length; i++)
		{
			Renderer renderer = _renderers[i];
			if ((bool)renderer)
			{
				renderer.forceRenderingOff = !visible;
			}
		}
	}

	protected virtual void OnReset(bool callback = true)
	{
		if ((bool)_breakEffect && _breakEffect.isPlaying)
		{
			_breakEffect.Stop();
		}
		ShowRenderers(visible: true);
		_broken = false;
		if (callback)
		{
			onReset?.Invoke(this);
		}
	}

	protected virtual void OnSpawn(bool callback = true)
	{
		startTime = Time.time;
		endTime = startTime + canBreakDelay;
		ShowRenderers(visible: true);
		if (_rigidbody.IsNotNull())
		{
			_rigidbody.detectCollisions = true;
			_rigidbody.useGravity = m_useGravity;
		}
		if (callback)
		{
			onSpawn?.Invoke(this);
		}
	}

	protected virtual void OnBreak(bool callback = true, bool signal = true)
	{
		if (_broken || Time.time < endTime)
		{
			return;
		}
		if ((bool)_breakEffect)
		{
			if (_breakEffect.isPlaying)
			{
				_breakEffect.Stop();
			}
			_breakEffect.Play();
		}
		if (signal && PhotonNetwork.InRoom)
		{
			VRRig vRRig = GetComponent<OwnerRig>();
			if (vRRig != null)
			{
				_breakSignal.Raise(vRRig.OwningNetPlayer.ActorNumber);
			}
		}
		ShowRenderers(visible: false);
		if (_rigidbody.IsNotNull())
		{
			_rigidbody.detectCollisions = false;
			_rigidbody.useGravity = false;
		}
		_broken = true;
		if (callback)
		{
			onBreak?.Invoke(this);
		}
	}

	private void UpdatePhysMasks()
	{
		int physicsMask = (int)_physicsMask;
		if ((bool)_collider)
		{
			_collider.includeLayers = physicsMask;
			_collider.excludeLayers = ~physicsMask;
		}
		if ((bool)_rigidbody)
		{
			_rigidbody.includeLayers = physicsMask;
			_rigidbody.excludeLayers = ~physicsMask;
		}
	}
}
