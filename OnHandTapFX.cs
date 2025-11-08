using GorillaLocomotion;
using UnityEngine;

internal struct OnHandTapFX : IFXEffectContext<HandEffectContext>
{
	public VRRig rig;

	public Vector3 tapDir;

	public bool isDownTap;

	public bool isLeftHand;

	public StiltID stiltID;

	public int surfaceIndex;

	public float volume;

	public float speed;

	public HandEffectContext effectContext
	{
		get
		{
			HandEffectContext handEffect = rig.GetHandEffect(isLeftHand, stiltID);
			rig.SetHandEffectData(handEffect, surfaceIndex, isDownTap, isLeftHand, stiltID, volume, speed, tapDir);
			return handEffect;
		}
	}

	public FXSystemSettings settings => rig.fxSettings;
}
