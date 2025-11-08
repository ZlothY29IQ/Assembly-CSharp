using System.Collections.Generic;
using UnityEngine;

public class MaterialCombinerPerRendererMono : MonoBehaviour
{
	public List<MaterialCombinerPerRendererInfo> slotData = new List<MaterialCombinerPerRendererInfo>();

	protected void Awake()
	{
	}

	public void AddEntry(Renderer r, int slot, int sliceIndex, Color baseColor, Material oldMat)
	{
		slotData.Add(new MaterialCombinerPerRendererInfo
		{
			renderer = r,
			slotIndex = slot,
			sliceIndex = sliceIndex,
			baseColor = baseColor,
			oldMat = oldMat
		});
	}

	public bool TryGetData(Renderer r, int slot, out MaterialCombinerPerRendererInfo data)
	{
		foreach (MaterialCombinerPerRendererInfo slotDatum in slotData)
		{
			if (slotDatum.renderer == r && slotDatum.slotIndex == slot)
			{
				data = slotDatum;
				return true;
			}
		}
		data = default(MaterialCombinerPerRendererInfo);
		return false;
	}
}
