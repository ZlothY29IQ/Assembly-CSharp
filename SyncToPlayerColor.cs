using System;
using UnityEngine;

public class SyncToPlayerColor : MonoBehaviour
{
	public VRRig rig;

	public Material target;

	public ShaderHashId[] colorPropertiesToSync = new ShaderHashId[1] { "_BaseColor" };

	private Action<Color> _colorFunc;

	protected virtual void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		_colorFunc = UpdateColor;
	}

	protected virtual void Start()
	{
		UpdateColor(rig.playerColor);
		rig.OnColorInitialized(_colorFunc);
	}

	protected virtual void OnEnable()
	{
		rig.OnColorChanged += _colorFunc;
	}

	protected virtual void OnDisable()
	{
		rig.OnColorChanged -= _colorFunc;
	}

	public virtual void UpdateColor(Color color)
	{
		if ((bool)target && colorPropertiesToSync != null)
		{
			for (int i = 0; i < colorPropertiesToSync.Length; i++)
			{
				ShaderHashId shaderHashId = colorPropertiesToSync[i];
				target.SetColor(shaderHashId, color);
			}
		}
	}
}
