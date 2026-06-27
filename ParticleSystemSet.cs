using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public class ParticleSystemSet : MonoBehaviour
{
	[SerializeField]
	private GameObject[] ActiveDuringEmission;

	private Vector3 localScale = Vector3.one;

	private ParticleSystem[] ps;

	private ParticleSystem.MainModule[] psMains;

	public GameObject[] skipForSimulationSpeed;

	private HashSet<ParticleSystem.MainModule> skipSet;

	private ParticleSystem.EmissionModule[] psEmits;

	private bool loop;

	private float fadeRate = 1f;

	private void Awake()
	{
		localScale = base.transform.localScale;
		ps = GetComponentsInChildren<ParticleSystem>();
		List<ParticleSystem.MainModule> list = new List<ParticleSystem.MainModule>();
		List<ParticleSystem.EmissionModule> list2 = new List<ParticleSystem.EmissionModule>();
		skipSet = new HashSet<ParticleSystem.MainModule>();
		for (int i = 0; i < ps.Length; i++)
		{
			list.Add(ps[i].main);
			list2.Add(ps[i].emission);
		}
		for (int j = 0; j < skipForSimulationSpeed.Length; j++)
		{
			skipSet.Add(skipForSimulationSpeed[j].GetComponent<ParticleSystem>().main);
		}
		psMains = list.ToArray();
		psEmits = list2.ToArray();
		SetPlayBackSpeed(0f);
	}

	public void SetFadeRate(float rate)
	{
		if (rate > 0f)
		{
			fadeRate = rate;
		}
	}

	public void SetPlayBackSpeed(float target)
	{
		for (int i = 0; i < psMains.Length; i++)
		{
			if (!skipSet.Contains(psMains[i]))
			{
				psMains[i].simulationSpeed = target;
			}
		}
	}

	public async void FadePlayBackSpeed(float target)
	{
		loop = fadeRate > 0f;
		while (loop)
		{
			for (int i = 0; i < psMains.Length; i++)
			{
				if (!skipSet.Contains(psMains[i]))
				{
					psMains[i].simulationSpeed = Mathf.MoveTowards(psMains[i].simulationSpeed, target, Time.deltaTime * fadeRate);
					loop = psMains[i].simulationSpeed != target;
				}
			}
			await Task.Yield();
		}
	}

	public void SetColor(string RRGGBB)
	{
		Color color = new Color((float)int.Parse(RRGGBB.Substring(0, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBB.Substring(2, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBB.Substring(4, 2), NumberStyles.HexNumber) / 255f);
		Color.RGBToHSV(color, out var H, out var S, out var V);
		Color max = Color.HSVToRGB(H, S, V / 4f);
		for (int i = 0; i < psMains.Length; i++)
		{
			psMains[i].startColor = new ParticleSystem.MinMaxGradient(color, max);
		}
	}

	public void SetColors(string RRGGBBRRGGBB)
	{
		Color min = new Color((float)int.Parse(RRGGBBRRGGBB.Substring(0, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBBRRGGBB.Substring(2, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBBRRGGBB.Substring(4, 2), NumberStyles.HexNumber) / 255f);
		Color max = new Color((float)int.Parse(RRGGBBRRGGBB.Substring(6, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBBRRGGBB.Substring(8, 2), NumberStyles.HexNumber) / 255f, (float)int.Parse(RRGGBBRRGGBB.Substring(10, 2), NumberStyles.HexNumber) / 255f);
		for (int i = 0; i < psMains.Length; i++)
		{
			psMains[i].startColor = new ParticleSystem.MinMaxGradient(min, max);
		}
	}

	public void Pause()
	{
		for (int i = 0; i < ps.Length; i++)
		{
			ps[i].Pause();
		}
	}

	public void StartEmission()
	{
		for (int i = 0; i < ps.Length; i++)
		{
			psMains[i].prewarm = false;
			ps[i].Play();
		}
		for (int j = 0; j < ActiveDuringEmission.Length; j++)
		{
			ActiveDuringEmission[j].SetActive(value: true);
		}
	}

	public void StopEmission()
	{
		for (int i = 0; i < ps.Length; i++)
		{
			ps[i].Stop();
		}
		for (int j = 0; j < ActiveDuringEmission.Length; j++)
		{
			ActiveDuringEmission[j].SetActive(value: false);
		}
	}

	public void Clear()
	{
		for (int i = 0; i < ps.Length; i++)
		{
			ps[i].Clear();
		}
	}

	public void SetScaleXZ(float scaler)
	{
		base.transform.localScale = new Vector3(localScale.x * scaler, localScale.y, localScale.z * scaler);
	}

	public async void FadeScaleXZ(float scaler)
	{
		Vector3 targetScale = new Vector3(localScale.x * scaler, localScale.y, localScale.z * scaler);
		loop = fadeRate > 0f;
		while (loop)
		{
			for (int i = 0; i < psMains.Length; i++)
			{
				base.transform.localScale = Vector3.MoveTowards(base.transform.localScale, targetScale, Time.deltaTime * fadeRate);
			}
			loop = base.transform.localScale != targetScale;
			await Task.Yield();
		}
	}
}
