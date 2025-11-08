using UnityEngine;

public class DisableGameObjectDelayed : MonoBehaviour
{
	public float delayTime = 1f;

	public float enabledTime;

	private void OnEnable()
	{
		enabledTime = Time.time;
	}

	private void Update()
	{
		if (Time.time > enabledTime + delayTime)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void EnableAndResetTimer()
	{
		base.gameObject.SetActive(value: true);
		OnEnable();
	}
}
