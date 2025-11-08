using System;
using UnityEngine;

public class MetroBlimp : MonoBehaviour
{
	public MetroSpotlight spotLightLeft;

	public MetroSpotlight spotLightRight;

	[Space]
	public BoxCollider topCollider;

	public Material blimpMaterial;

	public Renderer blimpRenderer;

	[Space]
	public float ascendSpeed = 1f;

	public float descendSpeed = 0.5f;

	public float descendOffset = -24.1f;

	public float descendReactionTime = 3f;

	[NonSerialized]
	[Space]
	private float _startLocalHeight;

	[NonSerialized]
	private float _topStayTime;

	[NonSerialized]
	private float _numHandsOnBlimp;

	[NonSerialized]
	private bool _lowering;

	private const string _INNER_GLOW = "_INNER_GLOW";

	private void Awake()
	{
		_startLocalHeight = base.transform.localPosition.y;
	}

	public void Tick()
	{
		bool flag = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f < 0.0001f;
		int num = Mathf.CeilToInt(_numHandsOnBlimp / 2f);
		if (_numHandsOnBlimp == 0f)
		{
			_topStayTime = 0f;
			if (flag)
			{
				blimpRenderer.material.DisableKeyword("_INNER_GLOW");
			}
		}
		else
		{
			_topStayTime += Time.deltaTime;
			if (flag)
			{
				blimpRenderer.material.EnableKeyword("_INNER_GLOW");
			}
		}
		Vector3 localPosition = base.transform.localPosition;
		Vector3 b = localPosition;
		float y = b.y;
		float num2 = _startLocalHeight + descendOffset;
		float deltaTime = Time.deltaTime;
		if (num > 0)
		{
			if (y > num2)
			{
				b += Vector3.down * (descendSpeed * (float)num * deltaTime);
			}
		}
		else if (y < _startLocalHeight)
		{
			b += Vector3.up * (ascendSpeed * deltaTime);
		}
		base.transform.localPosition = Vector3.Slerp(localPosition, b, 0.5f);
	}

	private static bool IsPlayerHand(Collider c)
	{
		return c.gameObject.IsOnLayer(UnityLayer.GorillaHand);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsPlayerHand(other))
		{
			_numHandsOnBlimp += 1f;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsPlayerHand(other))
		{
			_numHandsOnBlimp -= 1f;
		}
	}
}
