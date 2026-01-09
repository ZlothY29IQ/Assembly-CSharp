using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct GTRendererMatSlot
{
	public Renderer renderer;

	public int slot;

	public bool isValid { get; private set; }

	public bool TryInitialize()
	{
		isValid = renderer != null;
		if (!isValid)
		{
			return false;
		}
		List<Material> value;
		using (ListPool<Material>.Get(out value))
		{
			renderer.GetSharedMaterials(value);
			isValid = slot >= 0 && slot < value.Count && value[slot] != null;
			return isValid;
		}
	}
}
