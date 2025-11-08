using System.Collections.Generic;
using UnityEngine;

public class RotationSoundPlayer : MonoBehaviour
{
	[Tooltip("Transforms that will make a noise when they rotate.")]
	[SerializeField]
	private Transform[] transforms;

	[SerializeField]
	private SoundBankPlayer soundBankPlayer;

	[Tooltip("How much the transform must rotate from it's initial rotation before a sound is played.")]
	private float rotationAmountThreshold = 30f;

	[Tooltip("How fast the transform must rotate before a sound is played.")]
	private float rotationSpeedThreshold = 45f;

	private float cooldown = 0.6f;

	private float cooldownTimer;

	private Vector3[] initialUpAxis;

	private Vector3[] lastUpAxis;

	private float[] lastRotationSpeeds;

	private void Awake()
	{
		List<Transform> list = new List<Transform>(transforms);
		list.RemoveAll((Transform xform) => xform == null);
		transforms = list.ToArray();
		initialUpAxis = new Vector3[transforms.Length];
		lastUpAxis = new Vector3[transforms.Length];
		lastRotationSpeeds = new float[transforms.Length];
		for (int i = 0; i < transforms.Length; i++)
		{
			initialUpAxis[i] = transforms[i].localRotation * Vector3.up;
			lastUpAxis[i] = initialUpAxis[i];
			lastRotationSpeeds[i] = 0f;
		}
	}

	private void Update()
	{
		cooldownTimer -= Time.deltaTime;
		for (int i = 0; i < transforms.Length; i++)
		{
			Vector3 vector = transforms[i].localRotation * Vector3.up;
			float num = Vector3.Angle(vector, initialUpAxis[i]);
			float num2 = Vector3.Angle(vector, lastUpAxis[i]);
			float deltaTime = Time.deltaTime;
			float num3 = num2 / deltaTime;
			if (cooldownTimer <= 0f && num > rotationAmountThreshold && num3 > rotationSpeedThreshold && !soundBankPlayer.isPlaying)
			{
				cooldownTimer = cooldown;
				soundBankPlayer.Play();
			}
			lastUpAxis[i] = vector;
			lastRotationSpeeds[i] = num3;
		}
	}
}
