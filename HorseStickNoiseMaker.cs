using GorillaExtensions;
using UnityEngine;

public class HorseStickNoiseMaker : MonoBehaviour
{
	[Tooltip("Meters the object should traverse between playing a provided audio clip.")]
	public float metersPerClip = 4f;

	[Tooltip("Number of seconds that must elapse before playing another audio clip.")]
	public float minSecBetweenClips = 1.5f;

	public SoundBankPlayer soundBankPlayer;

	[Tooltip("Transform assigned in Gorilla Player Networked Prefab to the Gorilla Player Networked parent to keep track of distance traveled.")]
	public Transform gorillaPlayerXform;

	[Delayed]
	public string gorillaPlayerXform_path;

	[Tooltip("Optional particle FX to spawn when sound plays")]
	public ParticleSystem particleFX;

	private Vector3 oldPos;

	private float timeSincePlay;

	private float distElapsed;

	protected void OnEnable()
	{
		if (!gorillaPlayerXform && !base.transform.TryFindByPath(gorillaPlayerXform_path, out gorillaPlayerXform))
		{
			Debug.LogError("HorseStickNoiseMaker: DEACTIVATING! Could not find gorillaPlayerXform using path: \"" + gorillaPlayerXform_path + "\"\nThis component's transform path: \"" + base.transform.GetPath() + "\"");
			base.gameObject.SetActive(value: false);
		}
		else
		{
			oldPos = gorillaPlayerXform.position;
			distElapsed = 0f;
			timeSincePlay = 0f;
		}
	}

	protected void LateUpdate()
	{
		Vector3 position = gorillaPlayerXform.position;
		distElapsed += (position - oldPos).magnitude;
		timeSincePlay += Time.deltaTime;
		oldPos = position;
		if (distElapsed >= metersPerClip && timeSincePlay >= minSecBetweenClips)
		{
			soundBankPlayer.Play();
			distElapsed = 0f;
			timeSincePlay = 0f;
			if (particleFX != null)
			{
				particleFX.Play();
			}
		}
	}
}
