using UnityEngine;

public class TrickTreatItem : RandomComponent<MeshRenderer>
{
	protected override void OnNextItem(MeshRenderer item)
	{
		for (int i = 0; i < items.Length; i++)
		{
			MeshRenderer obj = items[i];
			obj.enabled = obj == item;
		}
	}

	public void Randomize()
	{
		NextItem();
	}
}
