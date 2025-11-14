using System;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class GorillaStatusToThermalTemperatureMono : MonoBehaviour, ISpawnable
{
	[Serializable]
	private struct _MaterialIndexToTemperature
	{
		public int[] matIndexes;

		public float temperature;
	}

	private const string preLog = "[GorillaStatusToThermalTemperatureMono]  ";

	private const string preErr = "[GorillaStatusToThermalTemperatureMono]  ERROR!!!  ";

	[Tooltip("Should either be assigned here or via another script.")]
	[SerializeField]
	private VRRig m_rig;

	[SerializeField]
	private ThermalSourceVolume m_thermalSourceVolume;

	[SerializeField]
	private _MaterialIndexToTemperature[] m_materialIndexesToTemperatures;

	[DebugReadout]
	private float[] _runtimeMatIndexes_to_temperatures;

	private const float _k_invalidTemperature = -32768f;

	public bool hasRig { get; private set; }

	public VRRig rig => m_rig;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void SetRig(VRRig newRig)
	{
		if (!(newRig == m_rig))
		{
			if (hasRig)
			{
				VRRig vRRig = m_rig;
				vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(_OnMatChanged));
			}
			m_rig = newRig;
			hasRig = newRig != null;
			if (hasRig && base.isActiveAndEnabled)
			{
				VRRig vRRig2 = m_rig;
				vRRig2.OnMaterialIndexChanged = (Action<int, int>)Delegate.Combine(vRRig2.OnMaterialIndexChanged, new Action<int, int>(_OnMatChanged));
				_InitRuntimeArray();
				_OnMatChanged(-1, m_rig.setMatIndex);
			}
		}
	}

	protected void Awake()
	{
		hasRig = m_rig != null;
		_InitRuntimeArray();
	}

	private void _InitRuntimeArray()
	{
		if (!hasRig || _runtimeMatIndexes_to_temperatures != null)
		{
			return;
		}
		int num = VRRig.LocalRig.materialsToChangeTo.Length;
		_runtimeMatIndexes_to_temperatures = new float[num];
		for (int i = 0; i < _runtimeMatIndexes_to_temperatures.Length; i++)
		{
			_runtimeMatIndexes_to_temperatures[i] = -32768f;
		}
		_MaterialIndexToTemperature[] materialIndexesToTemperatures = m_materialIndexesToTemperatures;
		for (int j = 0; j < materialIndexesToTemperatures.Length; j++)
		{
			_MaterialIndexToTemperature materialIndexToTemperature = materialIndexesToTemperatures[j];
			int[] matIndexes = materialIndexToTemperature.matIndexes;
			foreach (int num2 in matIndexes)
			{
				if (num2 >= 0 && num2 < num)
				{
					_runtimeMatIndexes_to_temperatures[num2] = materialIndexToTemperature.temperature;
				}
			}
		}
		if (!Application.isEditor)
		{
			m_materialIndexesToTemperatures = null;
		}
	}

	protected void OnEnable()
	{
		if (hasRig && !ApplicationQuittingState.IsQuitting)
		{
			if (m_thermalSourceVolume == null)
			{
				GTDev.LogError("[GorillaStatusToThermalTemperatureMono]  ERROR!!!  Disabling because thermal source is not assigned. Path=" + base.transform.GetPathQ(), this);
				base.enabled = false;
			}
			else
			{
				VRRig vRRig = m_rig;
				vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Combine(vRRig.OnMaterialIndexChanged, new Action<int, int>(_OnMatChanged));
				_OnMatChanged(-1, m_rig.setMatIndex);
			}
		}
	}

	protected void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting && hasRig)
		{
			VRRig vRRig = m_rig;
			vRRig.OnMaterialIndexChanged = (Action<int, int>)Delegate.Remove(vRRig.OnMaterialIndexChanged, new Action<int, int>(_OnMatChanged));
		}
	}

	private void _OnMatChanged(int oldIndex, int newIndex)
	{
		float num = _runtimeMatIndexes_to_temperatures[newIndex];
		m_thermalSourceVolume.celsius = num;
		m_thermalSourceVolume.enabled = num > -32767.99f;
	}

	public void OnSpawn(VRRig newRig)
	{
		SetRig(newRig);
	}

	public void OnDespawn()
	{
		SetRig(null);
	}
}
