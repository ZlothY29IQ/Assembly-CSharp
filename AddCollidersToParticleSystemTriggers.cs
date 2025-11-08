using UnityEngine;

public class AddCollidersToParticleSystemTriggers : MonoBehaviour
{
	public Collider[] collidersToAdd;

	public ParticleSystem particleSystemToUpdate;

	private int count;

	private int index;

	private void Update()
	{
		for (count = 0; count < 6; count++)
		{
			index++;
			if (index >= collidersToAdd.Length)
			{
				if (BetterDayNightManager.instance.collidersToAddToWeatherSystems.Count >= index - collidersToAdd.Length)
				{
					index = 0;
				}
				else
				{
					particleSystemToUpdate.trigger.SetCollider(count, BetterDayNightManager.instance.collidersToAddToWeatherSystems[index - collidersToAdd.Length]);
				}
			}
			if (index < collidersToAdd.Length)
			{
				particleSystemToUpdate.trigger.SetCollider(count, collidersToAdd[index]);
			}
		}
	}
}
