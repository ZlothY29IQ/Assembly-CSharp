using Photon.Pun;

public class GorillaThrowingRock : GorillaThrowable, IPunInstantiateMagicCallback
{
	public float bonkSpeedMin = 1f;

	public float bonkSpeedMax = 5f;

	public VRRig hitRig;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
	}
}
