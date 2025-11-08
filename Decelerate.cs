using UnityEngine;
using UnityEngine.Events;

public class Decelerate : MonoBehaviour
{
	[SerializeField]
	private Rigidbody _rigidbody;

	[SerializeField]
	private float _friction = 0.875f;

	[SerializeField]
	private bool _resetOrientationOnRelease;

	public UnityEvent onStop;

	public void Restart()
	{
		base.enabled = true;
	}

	private void Update()
	{
		if ((bool)_rigidbody)
		{
			Vector3 linearVelocity = _rigidbody.linearVelocity;
			linearVelocity *= _friction;
			if (linearVelocity.Approx0(0.001f))
			{
				_rigidbody.linearVelocity = Vector3.zero;
				onStop?.Invoke();
				base.enabled = false;
			}
			else
			{
				_rigidbody.linearVelocity = linearVelocity;
			}
			if (_resetOrientationOnRelease && !_rigidbody.rotation.Approx(Quaternion.identity))
			{
				_rigidbody.rotation = Quaternion.identity;
			}
		}
	}
}
