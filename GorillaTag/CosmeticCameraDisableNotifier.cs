using System;
using UnityEngine;

namespace GorillaTag;

[RequireComponent(typeof(VRRigCollection))]
public class CosmeticCameraDisableNotifier : MonoBehaviour
{
	private VRRigCollection _vrrigCollection;

	[SerializeField]
	private Camera _cosmeticCamera;

	private void Awake()
	{
		if (!TryGetComponent<VRRigCollection>(out _vrrigCollection))
		{
			_vrrigCollection = this.AddComponent<VRRigCollection>();
		}
		VRRigCollection vrrigCollection = _vrrigCollection;
		vrrigCollection.playerEnteredCollection = (Action<RigContainer>)Delegate.Combine(vrrigCollection.playerEnteredCollection, new Action<RigContainer>(PlayerEnteredTryOnSpace));
		VRRigCollection vrrigCollection2 = _vrrigCollection;
		vrrigCollection2.playerLeftCollection = (Action<RigContainer>)Delegate.Combine(vrrigCollection2.playerLeftCollection, new Action<RigContainer>(PlayerLeftTryOnSpace));
	}

	private void PlayerEnteredTryOnSpace(RigContainer playerRig)
	{
		if (playerRig.Rig.isLocal)
		{
			_cosmeticCamera.enabled = false;
		}
	}

	private void PlayerLeftTryOnSpace(RigContainer playerRig)
	{
		if (playerRig.Rig.isLocal)
		{
			_cosmeticCamera.enabled = true;
		}
	}
}
