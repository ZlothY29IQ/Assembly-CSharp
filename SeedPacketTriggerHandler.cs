using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(OnTriggerEventsCosmetic))]
public class SeedPacketTriggerHandler : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem particleToPlay;

	[SerializeField]
	private SoundBankPlayer soundBankPlayer;

	[SerializeField]
	private bool destroyOnTriggerEnter;

	[SerializeField]
	private float destroyDelay = 1f;

	[SerializeField]
	private bool toggleOnceOnly;

	[HideInInspector]
	public UnityEvent<SeedPacketTriggerHandler> onTriggerEntered;

	private bool triggerEntered;

	public void OnTriggerEntered()
	{
		if (!toggleOnceOnly || !triggerEntered)
		{
			triggerEntered = true;
			onTriggerEntered?.Invoke(this);
			ToggleEffects();
		}
	}

	public void ToggleEffects()
	{
		if ((bool)particleToPlay)
		{
			particleToPlay.Play();
		}
		if ((bool)soundBankPlayer)
		{
			soundBankPlayer.Play();
		}
		if (destroyOnTriggerEnter)
		{
			if (destroyDelay > 0f)
			{
				Invoke("Destroy", destroyDelay);
			}
			else
			{
				Destroy();
			}
		}
	}

	private void Destroy()
	{
		triggerEntered = false;
		if (ObjectPools.instance.DoesPoolExist(base.gameObject))
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
