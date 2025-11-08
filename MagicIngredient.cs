using System;
using UnityEngine;
using UnityEngine.Serialization;

[Obsolete("replaced with ThrowableSetDressing.cs")]
public class MagicIngredient : TransferrableObject
{
	[FormerlySerializedAs("IngredientType")]
	public MagicIngredientType IngredientTypeSO;

	public Transform rootParent;

	private WorldShareableItem item;

	private Transform grabPtInitParent;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		item = worldShareableInstance;
		grabPtInitParent = anchor.transform.parent;
	}

	private void ReParent()
	{
		Transform transform = anchor.transform;
		base.gameObject.transform.parent = transform;
		transform.parent = grabPtInitParent;
	}

	public void Disable()
	{
		DropItem();
		base.OnDisable();
		if ((bool)item)
		{
			item.OnDisable();
		}
		base.gameObject.SetActive(value: false);
	}
}
