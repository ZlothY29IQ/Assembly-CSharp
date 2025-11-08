using UnityEngine;

public class SecondLookSkeletonEnabler : Tappable
{
	public bool isTapped;

	public AudioSource playOnDisappear;

	public ParticleSystem particles;

	public GameObject spookyText;

	private SecondLookSkeleton skele;

	private void Awake()
	{
		isTapped = false;
		skele = Object.FindFirstObjectByType<SecondLookSkeleton>();
		skele.spookyText = spookyText;
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		if (!isTapped)
		{
			base.OnTapLocal(tapStrength, tapTime, info);
			if (skele != null)
			{
				skele.tapped = true;
			}
			base.gameObject.SetActive(value: false);
			isTapped = true;
			playOnDisappear.GTPlay();
			particles.Play();
		}
	}
}
