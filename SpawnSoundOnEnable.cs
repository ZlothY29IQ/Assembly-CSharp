using UnityEngine;

public class SpawnSoundOnEnable : MonoBehaviour
{
	public int soundSubIndex = 3;

	public bool triggerOnFirstEnable;

	private bool firstEnabledOccured;

	private void OnEnable()
	{
		if (CrittersManager.instance == null || !CrittersManager.instance.LocalAuthority() || !CrittersManager.instance.LocalInZone)
		{
			return;
		}
		if (!triggerOnFirstEnable && !firstEnabledOccured)
		{
			firstEnabledOccured = true;
			return;
		}
		CrittersLoudNoise crittersLoudNoise = (CrittersLoudNoise)CrittersManager.instance.SpawnActor(CrittersActor.CrittersActorType.LoudNoise, soundSubIndex);
		if (!(crittersLoudNoise == null))
		{
			crittersLoudNoise.MoveActor(base.transform.position, base.transform.rotation);
			crittersLoudNoise.SetImpulseVelocity(Vector3.zero, Vector3.zero);
		}
	}
}
