using System;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

public class SIUpgradeXformOffsetter : MonoBehaviour
{
	[Serializable]
	public struct SIUpgradeXformOffsetOp
	{
		public SIUpgradeType upgradeType;

		public Transform xform;

		[FormerlySerializedAs("newParent")]
		public Transform targetXform;
	}

	private const string preLog = "[SIUpgradeXformOffsetter]  ";

	private const string preErr = "[SIUpgradeXformOffsetter]  ERROR!!!  ";

	[SerializeField]
	private SIGadget m_superInfectionGadget;

	[SerializeField]
	private SIUpgradeXformOffsetOp[] m_upgradeXformOffsetOps;

	protected void Awake()
	{
		if (m_superInfectionGadget == null)
		{
			Debug.LogError("[SIUpgradeXformOffsetter]  ERROR!!!  Awake: Disabling component because `m_superInfectionGadget` is null. Path=" + base.transform.GetPathQ(), this);
			base.enabled = false;
			return;
		}
		SIUpgradeXformOffsetOp[] upgradeXformOffsetOps = m_upgradeXformOffsetOps;
		for (int i = 0; i < upgradeXformOffsetOps.Length; i++)
		{
			SIUpgradeXformOffsetOp sIUpgradeXformOffsetOp = upgradeXformOffsetOps[i];
			if (!(sIUpgradeXformOffsetOp.xform != null) && !(sIUpgradeXformOffsetOp.targetXform != null))
			{
				Debug.LogError("[SIUpgradeXformOffsetter]  ERROR!!!  Awake: Disabling component because null reference in `m_upgradeXformOffsetOps` array. Path=" + base.transform.GetPathQ(), this);
				base.enabled = false;
				break;
			}
		}
	}

	protected void OnEnable()
	{
		SIGadget superInfectionGadget = m_superInfectionGadget;
		superInfectionGadget.OnPostRefreshVisuals = (Action<SIUpgradeSet>)Delegate.Combine(superInfectionGadget.OnPostRefreshVisuals, new Action<SIUpgradeSet>(_HandleGadgetOnPostRefreshVisuals));
	}

	protected void OnDisable()
	{
		SIGadget superInfectionGadget = m_superInfectionGadget;
		superInfectionGadget.OnPostRefreshVisuals = (Action<SIUpgradeSet>)Delegate.Remove(superInfectionGadget.OnPostRefreshVisuals, new Action<SIUpgradeSet>(_HandleGadgetOnPostRefreshVisuals));
	}

	private void _HandleGadgetOnPostRefreshVisuals(SIUpgradeSet upgradeSet)
	{
		for (int i = 0; i < m_upgradeXformOffsetOps.Length; i++)
		{
			SIUpgradeXformOffsetOp sIUpgradeXformOffsetOp = m_upgradeXformOffsetOps[i];
			if (upgradeSet.Contains(sIUpgradeXformOffsetOp.upgradeType))
			{
				sIUpgradeXformOffsetOp.xform.SetLocalPositionAndRotation(sIUpgradeXformOffsetOp.targetXform.localPosition, sIUpgradeXformOffsetOp.targetXform.localRotation);
				sIUpgradeXformOffsetOp.xform.localScale = sIUpgradeXformOffsetOp.targetXform.localScale;
			}
		}
	}
}
