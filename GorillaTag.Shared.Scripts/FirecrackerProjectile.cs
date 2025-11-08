using System.Collections;
using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Shared.Scripts;

public class FirecrackerProjectile : MonoBehaviour, ITickSystemTick, IProjectile
{
	[SerializeField]
	private GameObject explosionEffect;

	[SerializeField]
	private float forceBackToPoolAfterSec = 20f;

	[SerializeField]
	private float explosionTime = 5f;

	[SerializeField]
	private GameObject disableWhenHit;

	[SerializeField]
	private float sizzleDuration;

	[SerializeField]
	private AudioClip sizzleAudioClip;

	[Space]
	public UnityEvent OnEnableObject;

	public UnityEvent<FirecrackerProjectile, Vector3> OnCollisionEntered;

	public UnityEvent<FirecrackerProjectile, Vector3> OnDetonationStart;

	public UnityEvent<FirecrackerProjectile> OnDetonationComplete;

	private Rigidbody rb;

	private float timeCreated = float.PositiveInfinity;

	private float timeExploded = float.PositiveInfinity;

	private AudioSource audioSource;

	private TickSystemTimer m_timer = new TickSystemTimer(40f);

	private bool collisionEntered;

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

	public bool TickRunning { get; set; }

	public void Tick()
	{
		if (Time.time - timeCreated > forceBackToPoolAfterSec || Time.time - timeExploded > explosionTime)
		{
			OnDetonationComplete?.Invoke(this);
		}
	}

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
		m_timer.Start();
		timeExploded = float.PositiveInfinity;
		timeCreated = float.PositiveInfinity;
		collisionEntered = false;
		if ((bool)disableWhenHit)
		{
			disableWhenHit.SetActive(value: true);
		}
		OnEnableObject?.Invoke();
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
		m_timer.Stop();
		if (useTransferrableObjectState)
		{
			OnResetProjectileState?.Invoke();
		}
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		audioSource = GetComponent<AudioSource>();
		m_timer.callback = Detonate;
	}

	private void Detonate()
	{
		m_timer.Stop();
		timeExploded = Time.time;
		if ((bool)disableWhenHit)
		{
			disableWhenHit.SetActive(value: false);
		}
		collisionEntered = false;
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

	public void Launch(Vector3 startPosition, Quaternion startRotation, Vector3 velocity, float chargeFrac, VRRig ownerRig, int progress)
	{
		base.transform.position = startPosition;
		base.transform.rotation = startRotation;
		base.transform.localScale = Vector3.one * ownerRig.scaleFactor;
		rb.linearVelocity = velocity;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!collisionEntered)
		{
			Vector3 point = other.contacts[0].point;
			Vector3 normal = other.contacts[0].normal;
			OnCollisionEntered?.Invoke(this, normal);
			if (sizzleDuration > 0f)
			{
				StartCoroutine(Sizzle(point, normal));
			}
			else
			{
				OnDetonationStart?.Invoke(this, point);
				Detonate(point, normal);
			}
			collisionEntered = true;
		}
	}

	private IEnumerator Sizzle(Vector3 contactPoint, Vector3 normal)
	{
		if ((bool)audioSource && sizzleAudioClip != null)
		{
			audioSource.GTPlayOneShot(sizzleAudioClip);
		}
		yield return new WaitForSeconds(sizzleDuration);
		OnDetonationStart?.Invoke(this, contactPoint);
		Detonate(contactPoint, normal);
	}

	private void Detonate(Vector3 contactPoint, Vector3 normal)
	{
		timeExploded = Time.time;
		GameObject obj = ObjectPools.instance.Instantiate(explosionEffect, contactPoint);
		obj.transform.up = normal;
		obj.transform.position = base.transform.position;
		if (obj.TryGetComponent<SoundBankPlayer>(out var component) && (bool)component.soundBank)
		{
			component.Play();
		}
		if ((bool)disableWhenHit)
		{
			disableWhenHit.SetActive(value: false);
		}
		collisionEntered = false;
	}
}
