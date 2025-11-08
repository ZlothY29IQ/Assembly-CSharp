using UnityEngine;

public class LightningGenerator : MonoBehaviour
{
	[SerializeField]
	private uint maxConcurrentStrikes = 10u;

	[SerializeField]
	private LightningStrike prototype;

	private LightningStrike[] strikes;

	private int index;

	private void Awake()
	{
		strikes = new LightningStrike[maxConcurrentStrikes];
		for (int i = 0; i < strikes.Length; i++)
		{
			if (i == 0)
			{
				strikes[i] = prototype;
			}
			else
			{
				strikes[i] = Object.Instantiate(prototype, base.transform);
			}
			strikes[i].gameObject.SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		LightningDispatcher.RequestLightningStrike += LightningDispatcher_RequestLightningStrike;
	}

	private void OnDisable()
	{
		LightningDispatcher.RequestLightningStrike -= LightningDispatcher_RequestLightningStrike;
	}

	private LightningStrike LightningDispatcher_RequestLightningStrike(Vector3 t1, Vector3 t2)
	{
		index = (index + 1) % strikes.Length;
		return strikes[index];
	}
}
