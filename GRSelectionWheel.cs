using System;
using System.Collections.Generic;
using GorillaExtensions;
using TMPro;
using UnityEngine;

public class GRSelectionWheel : MonoBehaviour, ITickSystemTick
{
	private List<TMP_Text> shelfNames = new List<TMP_Text>();

	public TMP_Text templateText;

	public float deltaAngle;

	public float pointerOffsetAngle;

	public float wheelTextRadius;

	public float textHorizOffset = -0.0375f;

	public float rotSpeed = 60f;

	public bool isBeingDrivenRemotely;

	public AudioSource audioSource;

	public int lastPlayedAudioTickPage = -1;

	public float wheelTextPairOffset = 0.0025f;

	public Transform rotationWheel;

	public float lastAngle = -1000f;

	[NonSerialized]
	public int targetPage;

	[NonSerialized]
	public float currentAngle;

	private float rotSpeedMult;

	public bool TickRunning { get; set; }

	public void Start()
	{
		targetPage = 0;
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void ShowText(bool showText)
	{
		foreach (TMP_Text shelfName in shelfNames)
		{
			shelfName.enabled = showText;
		}
	}

	public void InitFromNameList(List<string> shelves)
	{
		shelfNames.Clear();
		for (int i = 0; i < shelves.Count; i++)
		{
			TMP_Text tMP_Text = UnityEngine.Object.Instantiate(templateText);
			tMP_Text.text = shelves[i];
			shelfNames.Add(tMP_Text);
			tMP_Text.transform.SetParent(base.transform, worldPositionStays: false);
		}
		UpdateVisuals();
	}

	public void Tick()
	{
		if (!isBeingDrivenRemotely)
		{
			float num = deltaAngle * (float)shelfNames.Count;
			float num2 = currentAngle / deltaAngle;
			int num3 = (int)(num2 + 0.5f);
			if (rotSpeedMult == 0f)
			{
				float num4 = ((float)num3 - num2) * deltaAngle;
				currentAngle += num4 * (1f - Mathf.Exp(-20f * Time.deltaTime));
				targetPage = num3;
			}
			else
			{
				currentAngle += rotSpeedMult * Time.deltaTime * rotSpeed;
				currentAngle = Mathf.Clamp(currentAngle, (0f - deltaAngle) * 0.4f, num - deltaAngle + deltaAngle * 0.4f);
			}
		}
		int num5 = (int)(currentAngle / deltaAngle + 0.5f);
		if (lastPlayedAudioTickPage != num5)
		{
			lastPlayedAudioTickPage = num5;
			audioSource.GTPlay();
		}
		float num6 = 0.005f;
		if (Math.Abs(lastAngle - currentAngle) > num6)
		{
			UpdateVisuals();
		}
		lastAngle = currentAngle;
	}

	public void SetRotationSpeed(float speed)
	{
		rotSpeedMult = Mathf.Sign(speed) * Mathf.Pow(Mathf.Abs(speed), 2f);
	}

	public void SetTargetShelf(int shelf)
	{
		currentAngle += (float)(shelf - targetPage) * deltaAngle;
		targetPage = shelf;
	}

	public void SetTargetAngle(float angle)
	{
		currentAngle = angle;
	}

	public void UpdateVisuals()
	{
		rotationWheel.localRotation = Quaternion.Euler(0f - currentAngle + 7.5f, 0f, 0f);
		_ = deltaAngle;
		_ = shelfNames.Count;
		float num = currentAngle / deltaAngle;
		for (int i = 0; i < shelfNames.Count; i++)
		{
			float num2 = ((float)i - num) * deltaAngle + pointerOffsetAngle;
			float f = num2 * MathF.PI / 180f;
			float num3 = Mathf.Cos(f);
			float num4 = Mathf.Sin(f);
			Quaternion localRotation = Quaternion.Euler(90f - num2, 180f, 0f);
			Vector3 position = new Vector3(textHorizOffset, num3 * wheelTextRadius, num4 * wheelTextRadius);
			shelfNames[i].transform.rotation = base.transform.TransformRotation(localRotation);
			shelfNames[i].transform.position = base.transform.TransformPoint(position);
			shelfNames[i].color = ((Math.Abs(num - (float)i) < 0.5f) ? Color.green : Color.white);
		}
	}
}
