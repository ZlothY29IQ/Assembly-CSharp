using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class LightningStrike : MonoBehaviour
{
	public static SRand rand = new SRand("LightningStrike");

	private ParticleSystem ps;

	private ParticleSystem.MainModule psMain;

	private ParticleSystem.ShapeModule psShape;

	private ParticleSystem.TrailModule psTrails;

	private AudioSource audioSource;

	private void Initialize()
	{
		ps = GetComponent<ParticleSystem>();
		psMain = ps.main;
		psMain.playOnAwake = true;
		psMain.stopAction = ParticleSystemStopAction.Disable;
		psShape = ps.shape;
		psTrails = ps.trails;
		audioSource = GetComponent<AudioSource>();
		audioSource.playOnAwake = true;
	}

	public void Play(Vector3 p1, Vector3 p2, float beamWidthMultiplier, float audioVolume, float duration, Gradient colorOverLifetime)
	{
		if (ps == null)
		{
			Initialize();
		}
		base.transform.position = p1;
		base.transform.rotation = Quaternion.LookRotation(p1 - p2);
		psShape.radius = Vector3.Distance(p1, p2) * 0.5f;
		psShape.position = new Vector3(0f, 0f, 0f - psShape.radius);
		psShape.randomPositionAmount = Mathf.Clamp(psShape.radius / 50f, 0f, 1f);
		psTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(beamWidthMultiplier * 0.1f, beamWidthMultiplier);
		psTrails.colorOverLifetime = colorOverLifetime;
		psMain.duration = duration;
		audioSource.volume = Mathf.Clamp(psShape.radius / 5f, 0f, 1f) * audioVolume;
		base.gameObject.SetActive(value: true);
	}
}
