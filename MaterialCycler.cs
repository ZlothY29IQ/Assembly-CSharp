using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

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
	private MaterialPack[] materials;

	[SerializeField]
	private Renderer[] renderers;

	private int index;

	[SerializeField]
	private string setColorTarget = "_BaseColor";

	[SerializeField]
	private UnityEvent<Vector3> reset;

	private Coroutine crDirty;

	private float synchTime;

	private MaterialCyclerNetworked materialCyclerNetworked;

	private void Awake()
	{
		materialCyclerNetworked = GetComponent<MaterialCyclerNetworked>();
		SetMaterials();
	}

	private void OnEnable()
	{
		if (materialCyclerNetworked != null)
		{
			materialCyclerNetworked.OnSynchronize += MaterialCyclerNetworked_OnSynchronize;
		}
	}

	private void OnDisable()
	{
		if (materialCyclerNetworked != null)
		{
			materialCyclerNetworked.OnSynchronize -= MaterialCyclerNetworked_OnSynchronize;
		}
	}

	private void MaterialCyclerNetworked_OnSynchronize(int idx, int3 rgb)
	{
		if (idx >= 0 && idx < materials.Length)
		{
			index = idx;
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material = materials[index].Materials[i];
				renderers[i].material.SetColor(setColorTarget, new Color((float)rgb.x / 9f, (float)rgb.y / 9f, (float)rgb.z / 9f));
			}
			reset.Invoke(new Vector3(renderers[0].material.color.r, renderers[0].material.color.g, renderers[0].material.color.b));
		}
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
		index = (index + 1) % materials.Length;
		SetMaterials();
		SetDirty();
	}

	private void SetDirty()
	{
		if (!(materialCyclerNetworked == null))
		{
			synchTime = Time.time + materialCyclerNetworked.SyncTimeOut;
			if (crDirty == null)
			{
				crDirty = StartCoroutine(timeOutDirty());
			}
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
		materialCyclerNetworked.Synchronize(index, renderers[0].material.color);
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
