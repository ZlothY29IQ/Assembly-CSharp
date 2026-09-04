using UnityEngine;

public class SnowballKnockbackEnabler : MonoBehaviour
{
	private void OnEnable()
	{
		GrowingSnowballThrowable.NotifyEnableKnockbackIntent(this);
	}

	private void OnDisable()
	{
		GrowingSnowballThrowable.NotifyDisableKnockbackIntent(this);
	}
}
