using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderRecycler : MonoBehaviour
{
	public float recycleEffectDuration = 0.25f;

	private double timeToStopBlades = double.MinValue;

	private bool playingBladeEffect;

	private bool playingPipeEffect;

	private double timeToCheckPipes = double.MinValue;

	public List<MonoBehaviour> effectBehaviors;

	public GameObject recycleParticles;

	public SoundBankPlayer bladeSoundPlayer;

	public List<MeshRenderer> outputPipes;

	public BuilderResourceColors builderResourceColors;

	private bool hasFans;

	private bool hasPipes;

	private MaterialPropertyBlock props;

	private int[] totalRecycledCost;

	private int[] currentChainCost;

	private int numPipes;

	internal int recyclerID = -1;

	internal BuilderTable table;

	private List<Renderer> zoneRenderers = new List<Renderer>(10);

	private bool inBuilderZone;

	private void Awake()
	{
		hasFans = effectBehaviors.Count > 0 && bladeSoundPlayer != null && recycleParticles != null;
		hasPipes = outputPipes.Count > 0;
	}

	private void Start()
	{
		if (hasPipes)
		{
			numPipes = Mathf.Min(outputPipes.Count, 3);
			props = new MaterialPropertyBlock();
			ResetOutputPipes();
			totalRecycledCost = new int[3];
			currentChainCost = new int[3];
			for (int i = 0; i < totalRecycledCost.Length; i++)
			{
				totalRecycledCost[i] = 0;
				currentChainCost[i] = 0;
			}
		}
		zoneRenderers.Clear();
		if (hasPipes)
		{
			zoneRenderers.AddRange(outputPipes);
		}
		if (hasFans)
		{
			foreach (MonoBehaviour effectBehavior in effectBehaviors)
			{
				Renderer component = effectBehavior.GetComponent<Renderer>();
				if (component != null)
				{
					zoneRenderers.Add(component);
				}
			}
		}
		inBuilderZone = true;
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		OnZoneChanged();
	}

	private void OnDestroy()
	{
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocks);
		if (flag && !inBuilderZone)
		{
			foreach (Renderer zoneRenderer in zoneRenderers)
			{
				zoneRenderer.enabled = true;
			}
		}
		else if (!flag && inBuilderZone)
		{
			foreach (Renderer zoneRenderer2 in zoneRenderers)
			{
				zoneRenderer2.enabled = false;
			}
		}
		inBuilderZone = flag;
	}

	private void OnTriggerEnter(Collider other)
	{
		BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(other);
		if (!(builderPieceFromCollider == null) && !builderPieceFromCollider.isBuiltIntoTable && !builderPieceFromCollider.isArmShelf)
		{
			table.RequestRecyclePiece(builderPieceFromCollider, playFX: true, recyclerID);
		}
	}

	public void OnRecycleRequestedAtRecycler(BuilderPiece piece)
	{
		if (hasPipes)
		{
			AddPieceCost(piece.cost);
		}
		if (!hasFans)
		{
			return;
		}
		foreach (MonoBehaviour effectBehavior in effectBehaviors)
		{
			effectBehavior.enabled = true;
		}
		recycleParticles.SetActive(value: true);
		bladeSoundPlayer.Play();
		timeToStopBlades = Time.time + recycleEffectDuration;
		playingBladeEffect = true;
	}

	private void AddPieceCost(BuilderResources cost)
	{
		foreach (BuilderResourceQuantity quantity in cost.quantities)
		{
			if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
			{
				totalRecycledCost[(int)quantity.type] += quantity.count;
			}
		}
		if (!playingPipeEffect)
		{
			UpdatePipeLoop();
		}
	}

	private Vector2 GetUVShiftOffset()
	{
		float y = Shader.GetGlobalVector(ShaderProps._Time).y;
		Vector4 vector = new Vector4(500f, 0f, 0f, 0f);
		return new Vector2(-1f * (Mathf.Floor(y * (vector / recycleEffectDuration).x) * 1f / vector.x % 1f) * vector.x - vector.x + 165f, 0f);
	}

	private void UpdatePipeLoop()
	{
		bool flag = false;
		for (int i = 0; i < numPipes; i++)
		{
			if (totalRecycledCost[i] > 0)
			{
				flag = true;
				outputPipes[i].GetPropertyBlock(props, 1);
				Vector4 value = new Vector4(500f, 0f, 0f, 0f) / recycleEffectDuration;
				Vector2 uVShiftOffset = GetUVShiftOffset();
				props.SetColor(ShaderProps._BaseColor, builderResourceColors.colors[i].color);
				props.SetVector(ShaderProps._UvShiftRate, value);
				props.SetVector(ShaderProps._UvShiftOffset, uVShiftOffset);
				outputPipes[i].SetPropertyBlock(props, 1);
				totalRecycledCost[i] = Mathf.Max(totalRecycledCost[i] - 1, 0);
			}
			else
			{
				outputPipes[i].GetPropertyBlock(props, 1);
				props.SetColor(ShaderProps._BaseColor, Color.black);
				outputPipes[i].SetPropertyBlock(props, 1);
			}
		}
		if (flag)
		{
			playingPipeEffect = true;
			timeToCheckPipes = Time.time + recycleEffectDuration;
		}
		else
		{
			playingPipeEffect = false;
		}
	}

	private void ResetOutputPipes()
	{
		foreach (MeshRenderer outputPipe in outputPipes)
		{
			outputPipe.GetPropertyBlock(props, 1);
			props.SetColor(ShaderProps._BaseColor, Color.black);
			outputPipe.SetPropertyBlock(props, 1);
		}
	}

	public void UpdateRecycler()
	{
		if (playingBladeEffect && (double)Time.time > timeToStopBlades)
		{
			if (hasFans)
			{
				foreach (MonoBehaviour effectBehavior in effectBehaviors)
				{
					effectBehavior.enabled = false;
				}
				recycleParticles.SetActive(value: false);
			}
			playingBladeEffect = false;
		}
		if (playingPipeEffect && (double)Time.time > timeToCheckPipes)
		{
			UpdatePipeLoop();
		}
	}
}
