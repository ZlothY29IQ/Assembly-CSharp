using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class GlowBugsInJar : MonoBehaviour
{
	[SerializeField]
	private TransferrableObject transferrableObject;

	[Space]
	[Tooltip("Time interval - every X seconds update the glow value")]
	[SerializeField]
	private float glowUpdateInterval = 2f;

	[Tooltip("step increment - increase the glow value one step for N amount")]
	[SerializeField]
	private float glowIncreaseStepAmount = 0.1f;

	[Tooltip("step decrement - decrease the glow value one step for N amount")]
	[SerializeField]
	private float glowDecreaseStepAmount = 0.2f;

	[Space]
	[SerializeField]
	private string shaderProperty = "_EmissionColor";

	[SerializeField]
	private Renderer[] renderers;

	private bool shakeStarted = true;

	private static int EmissionColor;

	private float currentGlowAmount;

	private float shakeTimer;

	private RubberDuckEvents _events;

	private CallLimiter callLimiter = new CallLimiter(10, 2f);

	private void OnEnable()
	{
		shakeStarted = false;
		UpdateGlow(0f);
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShakeEvent);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnShakeEvent);
			_events.Dispose();
			_events = null;
		}
	}

	private void OnShakeEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != target)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "OnShakeEvent");
		if (!callLimiter.CheckCallTime(Time.time) || args == null || args.Length != 1)
		{
			return;
		}
		object obj = args[0];
		if (obj is bool)
		{
			if ((bool)obj)
			{
				ShakeStartLocal();
			}
			else
			{
				ShakeEndLocal();
			}
		}
	}

	public void HandleOnShakeStart()
	{
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(true);
		}
		ShakeStartLocal();
	}

	private void ShakeStartLocal()
	{
		currentGlowAmount = 0f;
		shakeStarted = true;
		shakeTimer = 0f;
	}

	public void HandleOnShakeEnd()
	{
		if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
		{
			_events.Activate.RaiseOthers(false);
		}
		ShakeEndLocal();
	}

	private void ShakeEndLocal()
	{
		shakeStarted = false;
		shakeTimer = 0f;
	}

	public void Update()
	{
		if (shakeStarted)
		{
			shakeTimer += 1f;
			if (shakeTimer >= glowUpdateInterval && currentGlowAmount < 1f)
			{
				currentGlowAmount += glowIncreaseStepAmount;
				UpdateGlow(currentGlowAmount);
				shakeTimer = 0f;
			}
		}
		else
		{
			shakeTimer += 1f;
			if (shakeTimer >= glowUpdateInterval && currentGlowAmount > 0f)
			{
				currentGlowAmount -= glowDecreaseStepAmount;
				UpdateGlow(currentGlowAmount);
				shakeTimer = 0f;
			}
		}
	}

	private void UpdateGlow(float value)
	{
		if (renderers.Length != 0)
		{
			for (int i = 0; i < renderers.Length; i++)
			{
				Material material = renderers[i].material;
				Color color = material.GetColor(shaderProperty);
				color.a = value;
				material.SetColor(shaderProperty, color);
				material.EnableKeyword("_EMISSION");
			}
		}
	}
}
