using GorillaLocomotion.Gameplay;
using Photon.Pun;
using UnityEngine;

public class SizeLayerChangerGrabable : MonoBehaviour, IGorillaGrabable
{
	[SerializeField]
	private bool grabChangesSizeLayer = true;

	[SerializeField]
	private bool releaseChangesSizeLayer = true;

	[SerializeField]
	private SizeLayerMask grabbedSizeLayerMask;

	[SerializeField]
	private SizeLayerMask releasedSizeLayerMask;

	[SerializeField]
	private bool momentaryGrabOnly = true;

	public bool MomentaryGrabOnly()
	{
		return momentaryGrabOnly;
	}

	bool IGorillaGrabable.CanBeGrabbed(GorillaGrabber grabber)
	{
		return true;
	}

	void IGorillaGrabable.OnGrabbed(GorillaGrabber g, out Transform grabbedObject, out Vector3 grabbedLocalPosiiton)
	{
		if (grabChangesSizeLayer)
		{
			VRRigCache.Instance.TryGetVrrig(PhotonNetwork.LocalPlayer, out var playerRig);
			playerRig.Rig.sizeManager.currentSizeLayerMaskValue = grabbedSizeLayerMask.Mask;
		}
		grabbedObject = base.transform;
		grabbedLocalPosiiton = base.transform.InverseTransformPoint(g.transform.position);
	}

	void IGorillaGrabable.OnGrabReleased(GorillaGrabber g)
	{
		if (releaseChangesSizeLayer)
		{
			VRRigCache.Instance.TryGetVrrig(PhotonNetwork.LocalPlayer, out var playerRig);
			playerRig.Rig.sizeManager.currentSizeLayerMaskValue = releasedSizeLayerMask.Mask;
		}
	}
}
