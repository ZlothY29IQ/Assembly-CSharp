using UnityEngine;

public class DelayedDestroyObject : MonoBehaviour
{
	public float lifetime = 10f;

	private float _timeToDie;

	private void Start()
	{
		_timeToDie = Time.time + lifetime;
	}

	private void LateUpdate()
	{
		if (Time.time >= _timeToDie)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
