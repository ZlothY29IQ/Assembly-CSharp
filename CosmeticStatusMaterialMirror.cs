using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class CosmeticStatusMaterialMirror : MonoBehaviour, ISpawnable
{
	[Serializable]
	private struct Target
	{
		[Tooltip("Renderer inside this cosmetic that should take on the status material")]
		public Renderer renderer;

		[Tooltip("[Optional] Indices into the renderer's Materials list (one per sub-mesh, e.g. 0 = Body, 1 = Chest, 2 = Tail). Leave empty to replace every slot. List specific indices to replace only those and keep the rest")]
		public int[] slots;
	}

	[Serializable]
	private struct StatusOverride
	{
		public int[] matIndexes;

		public Material material;
	}

	[Tooltip("Renderers that take on the status material")]
	[SerializeField]
	private Target[] targets = Array.Empty<Target>();

	[Tooltip("[Optional] Per-status material replacements, e.g. a lava material authored for this mesh's UVs. Statuses not listed here (or with no material assigned) use the VRRig's status material")]
	[SerializeField]
	private StatusOverride[] overrides = Array.Empty<StatusOverride>();

	[Tooltip("[Optional] Statuses this cosmetic should not react to")]
	[SerializeField]
	private int[] ignoredMatIndexes = Array.Empty<int>();

	private VRRig rig;

	private Material[][] cachedMaterials;

	private bool isStatusApplied;

	private bool isSubscribed;

	private bool hasStarted;

	private static readonly List<Material> matsCache = new List<Material>(8);

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig newRig)
	{
		SetRig(newRig);
	}

	public void OnDespawn()
	{
		SetRig(null);
	}

	protected void OnEnable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if (rig == null)
			{
				rig = GetComponentInParent<VRRig>(includeInactive: true);
			}
			if (hasStarted)
			{
				Subscribe();
			}
		}
	}

	protected void Start()
	{
		hasStarted = true;
		ValidateSlots();
		Subscribe();
	}

	protected void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			Unsubscribe();
			Restore();
		}
	}

	public void SetRig(VRRig newRig)
	{
		if (!(newRig == rig))
		{
			Unsubscribe();
			Restore();
			rig = newRig;
			if (base.isActiveAndEnabled && hasStarted)
			{
				Subscribe();
			}
		}
	}

	private void Subscribe()
	{
		if (!isSubscribed && !(rig == null))
		{
			VRRig vRRig = rig;
			vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Combine(vRRig.OnMaterialIndexChanged, new Action<int, int>(OnMatChanged));
			isSubscribed = true;
			OnMatChanged(-1, rig.setMatIndex);
		}
	}

	private void Unsubscribe()
	{
		if (isSubscribed)
		{
			if (rig != null)
			{
				VRRig vRRig = rig;
				vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(OnMatChanged));
			}
			isSubscribed = false;
		}
	}

	private void OnMatChanged(int oldIndex, int newIndex)
	{
		Material statusMaterial = GetStatusMaterial(newIndex);
		if (statusMaterial == null)
		{
			Restore();
		}
		else
		{
			Apply(statusMaterial);
		}
	}

	private Material GetStatusMaterial(int matIndex)
	{
		if (matIndex <= 0 || rig == null)
		{
			return null;
		}
		if (Array.IndexOf(ignoredMatIndexes, matIndex) >= 0)
		{
			return null;
		}
		StatusOverride[] array = overrides;
		for (int i = 0; i < array.Length; i++)
		{
			StatusOverride statusOverride = array[i];
			if (statusOverride.material != null && statusOverride.matIndexes != null && Array.IndexOf(statusOverride.matIndexes, matIndex) >= 0)
			{
				return statusOverride.material;
			}
		}
		Material[] materialsToChangeTo = rig.materialsToChangeTo;
		if (matIndex >= materialsToChangeTo.Length)
		{
			return null;
		}
		return materialsToChangeTo[matIndex];
	}

	private void Apply(Material statusMat)
	{
		if (!isStatusApplied)
		{
			if (cachedMaterials == null)
			{
				cachedMaterials = new Material[targets.Length][];
			}
			for (int i = 0; i < targets.Length; i++)
			{
				Renderer renderer = targets[i].renderer;
				cachedMaterials[i] = ((renderer != null) ? renderer.sharedMaterials : null);
			}
			isStatusApplied = true;
		}
		for (int j = 0; j < targets.Length; j++)
		{
			Target target = targets[j];
			if (target.renderer == null)
			{
				continue;
			}
			target.renderer.GetSharedMaterials(matsCache);
			if (target.slots == null || target.slots.Length == 0)
			{
				for (int k = 0; k < matsCache.Count; k++)
				{
					matsCache[k] = statusMat;
				}
			}
			else
			{
				int[] slots = target.slots;
				foreach (int num in slots)
				{
					if (num >= 0 && num < matsCache.Count)
					{
						matsCache[num] = statusMat;
					}
				}
			}
			target.renderer.SetSharedMaterials(matsCache);
		}
		matsCache.Clear();
	}

	private void ValidateSlots()
	{
		Target[] array = targets;
		for (int i = 0; i < array.Length; i++)
		{
			Target target = array[i];
			if (target.renderer == null || target.slots == null)
			{
				continue;
			}
			int num = target.renderer.sharedMaterials.Length;
			int[] slots = target.slots;
			foreach (int num2 in slots)
			{
				if (num2 < 0 || num2 >= num)
				{
					GTDev.LogError($"[CosmeticStatusMaterialMirror] ERROR!!!  Slot {num2} out of range on \"{target.renderer.name}\". " + "Path=" + base.transform.GetPathQ(), this);
				}
			}
		}
	}

	private void Restore()
	{
		if (!isStatusApplied)
		{
			return;
		}
		isStatusApplied = false;
		for (int i = 0; i < targets.Length; i++)
		{
			Renderer renderer = targets[i].renderer;
			if (renderer != null && cachedMaterials[i] != null)
			{
				renderer.sharedMaterials = cachedMaterials[i];
			}
			cachedMaterials[i] = null;
		}
	}
}
