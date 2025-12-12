using System;
using UnityEngine;

namespace GorillaTag;

[RequireComponent(typeof(VRRigCollection))]
public class CosmeticTryOnNotifier : MonoBehaviour
{
	private enum Mode
	{
		TRY_ON,
		ENABLE_LIST,
		ENABLE_LIST_TITLEDATA
	}

	private VRRigCollection m_vrrigCollection;

	[SerializeField]
	private Mode mode;

	[SerializeField]
	private StringList unlockList;

	private void Awake()
	{
		if (!TryGetComponent<VRRigCollection>(out m_vrrigCollection))
		{
			m_vrrigCollection = this.AddComponent<VRRigCollection>();
		}
		VRRigCollection vrrigCollection = m_vrrigCollection;
		vrrigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Combine(vrrigCollection.playerEnteredCollection, new Action<RigContainer>(PlayerEnteredTryOnSpace));
		VRRigCollection vrrigCollection2 = m_vrrigCollection;
		vrrigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Combine(vrrigCollection2.playerLeftCollection, new Action<RigContainer>(PlayerLeftTryOnSpace));
	}

	private void PlayerEnteredTryOnSpace(RigContainer playerRig)
	{
		switch (mode)
		{
		case Mode.TRY_ON:
			PlayerCosmeticsSystem.SetRigTryOn(inTryon: true, playerRig);
			break;
		case Mode.ENABLE_LIST:
			PlayerCosmeticsSystem.SetRigTemporarySpace(enteringSpace: true, playerRig, unlockList.Strings);
			break;
		}
	}

	private void PlayerLeftTryOnSpace(RigContainer playerRig)
	{
		switch (mode)
		{
		case Mode.TRY_ON:
			PlayerCosmeticsSystem.SetRigTryOn(inTryon: false, playerRig);
			break;
		case Mode.ENABLE_LIST:
			PlayerCosmeticsSystem.SetRigTemporarySpace(enteringSpace: false, playerRig, unlockList.Strings);
			break;
		}
	}
}
