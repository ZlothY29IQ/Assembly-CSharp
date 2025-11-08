using System.Collections.Generic;
using GorillaLocomotion.Swimming;
using UnityEngine;
using UnityEngine.Events;

public class WaterInteractionEvents : MonoBehaviour
{
	public UnityEvent onEnterWater = new UnityEvent();

	public UnityEvent onExitWater = new UnityEvent();

	[SerializeField]
	private SphereCollider waterContactSphere;

	private List<WaterVolume> overlappingWaterVolumes = new List<WaterVolume>();

	private bool inWater;

	private void Update()
	{
		if (overlappingWaterVolumes.Count < 1)
		{
			if (inWater)
			{
				onExitWater.Invoke();
			}
			inWater = false;
			base.enabled = false;
			return;
		}
		bool flag = false;
		for (int i = 0; i < overlappingWaterVolumes.Count; i++)
		{
			if (overlappingWaterVolumes[i].GetSurfaceQueryForPoint(waterContactSphere.transform.position, out var result))
			{
				float num = Vector3.Dot(result.surfacePoint - waterContactSphere.transform.position, result.surfaceNormal);
				float num2 = Vector3.Dot(result.surfacePoint - Vector3.up * result.maxDepth - base.transform.position, result.surfaceNormal);
				if (num > 0f - waterContactSphere.radius && num2 < waterContactSphere.radius)
				{
					flag = true;
				}
			}
		}
		bool flag2 = inWater;
		inWater = flag;
		if (!flag2 && inWater)
		{
			onEnterWater.Invoke();
		}
		else if (flag2 && !inWater)
		{
			onExitWater.Invoke();
		}
	}

	protected void OnTriggerEnter(Collider other)
	{
		WaterVolume component = other.GetComponent<WaterVolume>();
		if (component != null && !overlappingWaterVolumes.Contains(component))
		{
			overlappingWaterVolumes.Add(component);
			base.enabled = true;
		}
	}

	protected void OnTriggerExit(Collider other)
	{
		WaterVolume component = other.GetComponent<WaterVolume>();
		if (component != null && overlappingWaterVolumes.Contains(component))
		{
			overlappingWaterVolumes.Remove(component);
		}
	}
}
