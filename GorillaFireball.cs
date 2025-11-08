using Photon.Pun;
using UnityEngine;

public class GorillaFireball : GorillaThrowable, IPunInstantiateMagicCallback
{
	public float maxExplosionScale;

	public float totalExplosionTime;

	public float gravityStrength;

	private bool canExplode;

	private float explosionStartTime;

	public override void Start()
	{
		base.Start();
		canExplode = false;
		explosionStartTime = 0f;
	}

	private void Update()
	{
		if (explosionStartTime != 0f)
		{
			float num = (Time.time - explosionStartTime) / totalExplosionTime * (maxExplosionScale - 0.25f) + 0.25f;
			base.gameObject.transform.localScale = new Vector3(num, num, num);
			if (base.photonView.IsMine && Time.time > explosionStartTime + totalExplosionTime)
			{
				PhotonNetwork.Destroy(PhotonView.Get(this));
			}
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (rigidbody.useGravity)
		{
			rigidbody.AddForce(Physics.gravity * (0f - gravityStrength) * rigidbody.mass);
		}
	}

	public override void ThrowThisThingo()
	{
		base.ThrowThisThingo();
		canExplode = true;
	}

	private new void OnCollisionEnter(Collision collision)
	{
		if (base.photonView.IsMine && canExplode)
		{
			base.photonView.RPC("Explode", RpcTarget.All, null);
		}
	}

	public void LocalExplode()
	{
		rigidbody.isKinematic = true;
		canExplode = false;
		explosionStartTime = Time.time;
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		if (base.photonView.IsMine)
		{
			if ((bool)base.photonView.InstantiationData[0])
			{
				base.transform.parent = GorillaPlaySpace.Instance.myVRRig.leftHandTransform;
			}
			else
			{
				base.transform.parent = GorillaPlaySpace.Instance.myVRRig.rightHandTransform;
			}
		}
	}

	[PunRPC]
	public void Explode()
	{
		LocalExplode();
	}
}
