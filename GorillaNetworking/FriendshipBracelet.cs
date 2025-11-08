using System.Collections.Generic;
using UnityEngine;

namespace GorillaNetworking;

public class FriendshipBracelet : MonoBehaviour
{
	[SerializeField]
	private SkinnedMeshRenderer[] braceletStrings;

	[SerializeField]
	private MeshRenderer[] braceletBeads;

	[SerializeField]
	private MeshRenderer[] braceletBananas;

	[SerializeField]
	private bool isLeftHand;

	[SerializeField]
	private AudioClip braceletFormedSound;

	[SerializeField]
	private AudioClip braceletBrokenSound;

	[SerializeField]
	private ParticleSystem braceletFormedParticle;

	[SerializeField]
	private ParticleSystem braceletBrokenParticle;

	private VRRig ownerRig;

	protected void Awake()
	{
		ownerRig = GetComponentInParent<VRRig>();
	}

	private AudioSource GetAudioSource()
	{
		if (!isLeftHand)
		{
			return ownerRig.rightHandPlayer;
		}
		return ownerRig.leftHandPlayer;
	}

	private void OnEnable()
	{
		PlayAppearEffects();
	}

	public void PlayAppearEffects()
	{
		GetAudioSource().GTPlayOneShot(braceletFormedSound);
		if ((bool)braceletFormedParticle)
		{
			braceletFormedParticle.Play();
		}
	}

	private void OnDisable()
	{
		if (ownerRig.gameObject.activeInHierarchy)
		{
			GetAudioSource().GTPlayOneShot(braceletBrokenSound);
			if ((bool)braceletBrokenParticle)
			{
				braceletBrokenParticle.Play();
			}
		}
	}

	public void UpdateBeads(List<Color> colors, int selfIndex)
	{
		int num = colors.Count - 1;
		int num2 = (braceletBeads.Length - num) / 2;
		for (int i = 0; i < braceletBeads.Length; i++)
		{
			int num3 = i - num2;
			if (num3 >= 0 && num3 < num)
			{
				braceletBeads[i].enabled = true;
				braceletBeads[i].material.color = colors[num3];
				braceletBananas[i].gameObject.SetActive(num3 == selfIndex);
			}
			else
			{
				braceletBeads[i].enabled = false;
				braceletBananas[i].gameObject.SetActive(value: false);
			}
		}
		SkinnedMeshRenderer[] array = braceletStrings;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].material.color = colors[colors.Count - 1];
		}
	}
}
