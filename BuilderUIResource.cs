using GorillaTagScripts;
using TMPro;
using UnityEngine;

public class BuilderUIResource : MonoBehaviour
{
	public TextMeshPro resourceNameLabel;

	public TextMeshPro costLabel;

	public TextMeshPro availableLabel;

	public void SetResourceCost(BuilderResourceQuantity resourceCost, BuilderTable table)
	{
		BuilderResourceType type = resourceCost.type;
		int count = resourceCost.count;
		int availableResources = table.GetAvailableResources(type);
		if (resourceNameLabel != null)
		{
			resourceNameLabel.text = GetResourceName(type);
		}
		if (costLabel != null)
		{
			costLabel.text = count.ToString();
		}
		if (availableLabel != null)
		{
			availableLabel.text = availableResources.ToString();
		}
	}

	private string GetResourceName(BuilderResourceType type)
	{
		return type switch
		{
			BuilderResourceType.Basic => "Basic", 
			BuilderResourceType.Decorative => "Decorative", 
			BuilderResourceType.Functional => "Functional", 
			_ => "Resource Needs Name", 
		};
	}
}
