using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialCycler;

public class MaterialCycler : MonoBehaviour
{
	[Serializable]
	private class MaterialPack
	{
		[SerializeField]
		private Material[] materials;

		public Material[] Materials => materials;
	}

	[SerializeField]
	private string _cyclerKey;

	[SerializeField]
	private MaterialPack[] materials;

	[SerializeField]
	private Renderer[] renderers;

	[SerializeField]
	private string setColorTarget = "_BaseColor";

	[SerializeField]
	private UnityEvent<Vector3> reset;

	[SerializeField]
	private GrabbingColorPicker _colorPicker;

	private Coroutine crDirty;

	private float synchTime;

	private int? _keyHash;

	public int index { get; private set; }

	public GrabbingColorPicker ColorPicker => _colorPicker;

	public int NumMaterials => materials.Length;

	public int KeyHash
	{
		get
		{
			int valueOrDefault = _keyHash.GetValueOrDefault();
			if (!_keyHash.HasValue)
			{
				valueOrDefault = _cyclerKey.GetStaticHash();
				_keyHash = valueOrDefault;
				return valueOrDefault;
			}
			return valueOrDefault;
		}
	}

	private void Awake()
	{
		SetMaterials();
		if (string.IsNullOrEmpty(_cyclerKey))
		{
			throw new Exception("Must have a defined Cycler Key on all MaterialCyclers.");
		}
	}

	private void OnEnable()
	{
		if (string.IsNullOrEmpty(_cyclerKey))
		{
			throw new Exception("Must have a defined Cycler Key on all MaterialCyclers.");
		}
		MaterialCyclerManager.Instance.RegisterCycler(KeyHash, this);
	}

	private void OnDisable()
	{
		MaterialCyclerManager.Instance.UnregisterCycler(this);
	}

	internal void MaterialCyclerNetworked_OnSynchronize(int idx, Color rgb)
	{
		if (idx >= 0 && idx < materials.Length)
		{
			index = idx;
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material = materials[index].Materials[i];
				renderers[i].material.SetColor(setColorTarget, rgb);
			}
			reset.Invoke(new Vector3(renderers[0].material.color.r, renderers[0].material.color.g, renderers[0].material.color.b));
		}
	}

	public void SynchronizeLocal(float r, float g, float b)
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material = materials[index].Materials[i];
			renderers[i].material.SetColor(setColorTarget, new Color(r, g, b));
		}
		reset.Invoke(new Vector3(renderers[0].material.color.r, renderers[0].material.color.g, renderers[0].material.color.b));
	}

	private void SetMaterials()
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			if (materials[index].Materials.Length > i)
			{
				renderers[i].material = materials[index].Materials[i];
			}
			else
			{
				renderers[i].material = null;
			}
		}
		reset.Invoke(new Vector3(renderers[0].material.color.r, renderers[0].material.color.g, renderers[0].material.color.b));
	}

	public void NextMaterial()
	{
		CycleMaterial((index + 1) % materials.Length);
	}

	internal void CycleMaterial(int newIndex)
	{
		index = newIndex;
		SetMaterials();
		SetDirty();
	}

	private void SetDirty()
	{
		synchTime = Time.time + MaterialCyclerManager.Instance.SyncTimeOut;
		if (crDirty == null)
		{
			crDirty = StartCoroutine(timeOutDirty());
		}
	}

	private IEnumerator timeOutDirty()
	{
		while (synchTime > Time.time)
		{
			yield return null;
		}
		synchronize();
		crDirty = null;
	}

	private void synchronize()
	{
		MaterialCyclerManager.Instance.Synchronize(KeyHash, index, renderers[0].material.color);
	}

	public void SetColor(Vector3 rgb)
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].material.SetColor(setColorTarget, new Color(rgb.x, rgb.y, rgb.z));
		}
		SetDirty();
	}
}
