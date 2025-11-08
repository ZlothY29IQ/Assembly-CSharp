using System;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

public class MenorahCandle : MonoBehaviourPun
{
	public int day;

	public int month;

	public int year;

	public GameObject flame;

	public GameObject candle;

	private DateTime litDate;

	private bool activeTimeEventDay;

	private DateTime currentDate;

	private void Awake()
	{
	}

	private void Start()
	{
		EnableCandle(enable: false);
		EnableFlame(enable: false);
		litDate = new DateTime(year, month, day);
		currentDate = DateTime.Now;
		EnableCandle(CandleShouldBeVisible());
		EnableFlame(enable: false);
		GorillaComputer instance = GorillaComputer.instance;
		instance.OnServerTimeUpdated = (Action)Delegate.Combine(instance.OnServerTimeUpdated, new Action(OnTimeChanged));
	}

	private void UpdateMenorah()
	{
		EnableCandle(CandleShouldBeVisible());
		if (ShouldLightCandle())
		{
			EnableFlame(enable: true);
		}
		else if (ShouldSnuffCandle())
		{
			EnableFlame(enable: false);
		}
	}

	private void OnTimeChanged()
	{
		currentDate = GorillaComputer.instance.GetServerTime();
		UpdateMenorah();
	}

	public void OnTimeEventStart()
	{
		activeTimeEventDay = true;
		UpdateMenorah();
	}

	public void OnTimeEventEnd()
	{
		activeTimeEventDay = false;
		UpdateMenorah();
	}

	private void EnableCandle(bool enable)
	{
		if ((bool)candle)
		{
			candle.SetActive(enable);
		}
	}

	private bool CandleShouldBeVisible()
	{
		return currentDate >= litDate;
	}

	private void EnableFlame(bool enable)
	{
		if ((bool)flame)
		{
			flame.SetActive(enable);
		}
	}

	private bool ShouldLightCandle()
	{
		if (!activeTimeEventDay && CandleShouldBeVisible())
		{
			return !flame.activeSelf;
		}
		return false;
	}

	private bool ShouldSnuffCandle()
	{
		if (activeTimeEventDay)
		{
			return flame.activeSelf;
		}
		return false;
	}

	private void OnDestroy()
	{
		if ((bool)GorillaComputer.instance)
		{
			GorillaComputer instance = GorillaComputer.instance;
			instance.OnServerTimeUpdated = (Action)Delegate.Remove(instance.OnServerTimeUpdated, new Action(OnTimeChanged));
		}
	}
}
