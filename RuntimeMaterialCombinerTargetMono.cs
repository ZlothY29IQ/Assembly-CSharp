using System;
using UnityEngine;

public class RuntimeMaterialCombinerTargetMono : MonoBehaviour
{
	[HideInInspector]
	public GTSerializableDict<string, string>[] m_matSlot_to_texProp_to_texGuid;

	protected void Awake()
	{
		throw new NotImplementedException("// TODO: get the material combiner manager to fingerprint and combine these materials.");
	}
}
