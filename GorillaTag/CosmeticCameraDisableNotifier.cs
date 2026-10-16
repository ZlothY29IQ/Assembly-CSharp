using System;
using UnityEngine;

namespace GorillaTag;

[RequireComponent(typeof(VRRigCollection))]
public class CosmeticCameraDisableNotifier : MonoBehaviour
{
	private VRRigCollection _vrrigCollection;

	[SerializeField]
	private Camera _cosmeticCamera;

	[Tooltip("Surface showing the camera's render texture. When set, the camera only renders while this is visible.")]
	[SerializeField]
	private Renderer _displayRenderer;

	private bool _localPlayerInside;

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

	private void LateUpdate()
	{
		bool flag = !_localPlayerInside && (_displayRenderer == null || _displayRenderer.isVisible);
		if (_cosmeticCamera.enabled != flag)
		{
			_cosmeticCamera.enabled = flag;
		}
	}

	private void PlayerEnteredTryOnSpace(RigContainer playerRig)
	{
		if (playerRig.Rig.isLocal)
		{
			_localPlayerInside = true;
		}
	}

	private void PlayerLeftTryOnSpace(RigContainer playerRig)
	{
		if (playerRig.Rig.isLocal)
		{
			_localPlayerInside = false;
		}
	}
}
