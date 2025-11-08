using System;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Serialization;

public class BuilderResourceMeter : MonoBehaviour
{
	public BuilderResourceColors resourceColors;

	public MeshRenderer fillCube;

	public MeshRenderer emptyCube;

	private Color fillColor = Color.white;

	public Color emptyColor = Color.black;

	[FormerlySerializedAs("MeterHeight")]
	public float meterHeight = 2f;

	public float meshHeight = 1f;

	public BuilderResourceType _resourceType;

	private float fillAmount;

	[Range(0f, 1f)]
	[SerializeField]
	private float fillTarget;

	public float lerpSpeed = 0.5f;

	private bool animatingMeter;

	private int resourceMax = -1;

	private int usedResource = -1;

	private bool inBuilderZone;

	internal BuilderTable table;

	private void Awake()
	{
		fillColor = resourceColors.GetColorForType(_resourceType);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		fillCube.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor(ShaderProps._BaseColor, fillColor);
		fillCube.SetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor(ShaderProps._BaseColor, emptyColor);
		emptyCube.SetPropertyBlock(materialPropertyBlock);
		fillAmount = fillTarget;
	}

	private void Start()
	{
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
		if (flag != inBuilderZone)
		{
			inBuilderZone = flag;
			if (!flag)
			{
				fillCube.enabled = false;
				emptyCube.enabled = false;
			}
			else
			{
				fillCube.enabled = true;
				emptyCube.enabled = true;
				OnAvailableResourcesChange();
			}
		}
	}

	public void OnAvailableResourcesChange()
	{
		if (!(table == null) && table.maxResources != null)
		{
			resourceMax = table.maxResources[(int)_resourceType];
			int num = table.usedResources[(int)_resourceType];
			if (num != usedResource)
			{
				usedResource = num;
				SetNormalizedFillTarget((float)(resourceMax - usedResource) / (float)resourceMax);
			}
		}
	}

	public void UpdateMeterFill()
	{
		if (animatingMeter)
		{
			float newFill = Mathf.MoveTowards(fillAmount, fillTarget, lerpSpeed * Time.deltaTime);
			UpdateFill(newFill);
		}
	}

	private void UpdateFill(float newFill)
	{
		fillAmount = newFill;
		if (Mathf.Approximately(fillAmount, fillTarget))
		{
			fillAmount = fillTarget;
			animatingMeter = false;
		}
		if (inBuilderZone)
		{
			if (fillAmount <= float.Epsilon)
			{
				fillCube.enabled = false;
				float y = meterHeight / meshHeight;
				Vector3 localScale = new Vector3(emptyCube.transform.localScale.x, y, emptyCube.transform.localScale.z);
				Vector3 localPosition = new Vector3(0f, meterHeight / 2f, 0f);
				emptyCube.transform.localScale = localScale;
				emptyCube.transform.localPosition = localPosition;
				emptyCube.enabled = true;
			}
			else if (fillAmount >= 1f)
			{
				float y2 = meterHeight / meshHeight;
				Vector3 localScale2 = new Vector3(fillCube.transform.localScale.x, y2, fillCube.transform.localScale.z);
				Vector3 localPosition2 = new Vector3(0f, meterHeight / 2f, 0f);
				fillCube.transform.localScale = localScale2;
				fillCube.transform.localPosition = localPosition2;
				fillCube.enabled = true;
				emptyCube.enabled = false;
			}
			else
			{
				float num = meterHeight / meshHeight * fillAmount;
				Vector3 localScale3 = new Vector3(fillCube.transform.localScale.x, num, fillCube.transform.localScale.z);
				Vector3 localPosition3 = new Vector3(0f, num * meshHeight / 2f, 0f);
				fillCube.transform.localScale = localScale3;
				fillCube.transform.localPosition = localPosition3;
				fillCube.enabled = true;
				float num2 = meterHeight / meshHeight * (1f - fillAmount);
				Vector3 localScale4 = new Vector3(emptyCube.transform.localScale.x, num2, emptyCube.transform.localScale.z);
				Vector3 localPosition4 = new Vector3(0f, meterHeight - num2 * meshHeight / 2f, 0f);
				emptyCube.transform.localScale = localScale4;
				emptyCube.transform.localPosition = localPosition4;
				emptyCube.enabled = true;
			}
		}
	}

	public void SetNormalizedFillTarget(float fill)
	{
		fillTarget = Mathf.Clamp(fill, 0f, 1f);
		animatingMeter = true;
	}
}
