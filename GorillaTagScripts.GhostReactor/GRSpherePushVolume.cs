using System;
using System.Collections;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor;

[RequireComponent(typeof(SphereCollider))]
public class GRSpherePushVolume : MonoBehaviour
{
	public enum PushKind
	{
		Radial,
		UpAndOut
	}

	[SerializeField]
	private PushKind _pushKind;

	[SerializeField]
	private float _pushDelay;

	[SerializeField]
	private float _pushCooldown = 1f;

	[SerializeField]
	private AnimationCurve _pushScaling = AnimationCurve.Constant(0f, 1f, 1f);

	[SerializeField]
	private float _pushForce = 1f;

	[SerializeField]
	private float _disableAfter = 3f;

	private SphereCollider _collider;

	private bool _localFlung;

	private Coroutine _coroutine;

	private void Awake()
	{
		_collider = GetComponent<SphereCollider>();
		_collider.enabled = false;
	}

	public void Trigger()
	{
		_collider.enabled = true;
		StartCoroutine(DisableCoroutine());
	}

	private void OnTriggerStay(Collider other)
	{
		if (!_localFlung && _coroutine == null && other.gameObject.TryGetComponentInParent<GRPlayer>(out var component) && !(GRPlayer.GetLocal() != component))
		{
			_coroutine = StartCoroutine(ActionCoroutine(other));
			_collider.enabled = false;
		}
	}

	private IEnumerator ActionCoroutine(Collider other)
	{
		yield return new WaitForSeconds(_pushDelay);
		Vector3 velocity = CalculatePushVector(other);
		GTPlayer.Instance.DoLaunch(velocity);
		_localFlung = true;
		yield return new WaitForSeconds(_pushCooldown);
		_localFlung = false;
		_coroutine = null;
	}

	private IEnumerator DisableCoroutine()
	{
		yield return new WaitForSeconds(_disableAfter);
		_collider.enabled = false;
	}

	private Vector3 CalculatePushVector(Collider other)
	{
		return _pushKind switch
		{
			PushKind.Radial => CalculateRadialPushVector(other), 
			PushKind.UpAndOut => CalculateUpAndOutPushVector(other), 
			_ => throw new NotImplementedException(), 
		};
	}

	private Vector3 CalculateRadialPushVector(Collider other)
	{
		Vector3 vector = other.gameObject.transform.position - base.transform.position;
		float time = vector.magnitude / _collider.radius;
		return _pushScaling.Evaluate(time) * _pushForce * vector.normalized;
	}

	private Vector3 CalculateUpAndOutPushVector(Collider other)
	{
		Vector3 vector = new Vector3(other.gameObject.transform.position.x - base.transform.position.x, 0f, other.gameObject.transform.position.z - base.transform.position.z);
		float time = vector.magnitude / _collider.radius;
		vector.Normalize();
		Vector3.RotateTowards(vector, Vector3.up, MathF.PI / 4f, 0f);
		return vector * (_pushForce * _pushScaling.Evaluate(time));
	}
}
