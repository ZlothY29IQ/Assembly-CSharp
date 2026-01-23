using System;
using System.Collections;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor;

public class GRSquishVolume : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private Collider _collider;

	[SerializeField]
	private Collider[] _collidersToDisable;

	[SerializeField]
	private float _reenableDelay = 1f;

	[SerializeField]
	private float _launchStrength = 8f;

	[SerializeField]
	private float _launchDeflectionDegrees = 10f;

	private Coroutine _reenableCoroutine;

	private GREnemyBossMoon moonBoss;

	public float squishHeight;

	public Vector3 rotationOffset;

	public float facingDownDegrees = 20f;

	public bool overrideDisabled;

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this);
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
	}

	private void Start()
	{
		SetCollider(colliderEnabled: false);
		SetTentacleColliders(enabled: true);
		moonBoss = GetComponentInParent<GREnemyBossMoon>();
		if (moonBoss != null && !moonBoss.squishVolumes.Contains(this))
		{
			moonBoss.squishVolumes.Add(this);
		}
	}

	public void SliceUpdate()
	{
		SetCollider(!overrideDisabled && base.transform.position.y < squishHeight && Vector3.Angle(-base.transform.forward, Quaternion.Euler(rotationOffset) * Vector3.down) < facingDownDegrees);
	}

	public void SetCollider(bool colliderEnabled)
	{
		_collider.enabled = colliderEnabled;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.TryGetComponentInParent<GRPlayer>(out var component) && !(GRPlayer.GetLocal() != component) && _reenableCoroutine == null)
		{
			SetTentacleColliders(enabled: false);
			GTPlayer.Instance.DoLaunch(GetLaunchVector());
			moonBoss.HitPlayer(GRPlayer.GetLocal());
			_reenableCoroutine = StartCoroutine(ReenableCoroutine());
			moonBoss.SetSquishVolumeState(squishEnabled: false);
		}
	}

	private void SetTentacleColliders(bool enabled)
	{
		Collider[] collidersToDisable = _collidersToDisable;
		for (int i = 0; i < collidersToDisable.Length; i++)
		{
			collidersToDisable[i].enabled = enabled;
		}
	}

	private IEnumerator ReenableCoroutine()
	{
		yield return new WaitForSeconds(_reenableDelay);
		SetTentacleColliders(enabled: true);
		_reenableCoroutine = null;
		moonBoss.SetSquishVolumeState(squishEnabled: true);
	}

	private Vector3 GetLaunchVector()
	{
		Vector3 position = GRPlayer.GetLocal().transform.position;
		Vector3 lhs = position - base.transform.position;
		Vector3 vector = base.transform.position + base.transform.right * Vector3.Dot(lhs, base.transform.right);
		Vector3 normalized = (position - vector).normalized;
		Vector3 vector2 = Vector3.RotateTowards(new Vector3(normalized.x, 0f, normalized.y).normalized, maxRadiansDelta: UnityEngine.Random.Range(_launchDeflectionDegrees / 2f, _launchDeflectionDegrees) * (MathF.PI / 180f), target: Vector3.up, maxMagnitudeDelta: 0f);
		return _launchStrength * vector2.normalized;
	}
}
