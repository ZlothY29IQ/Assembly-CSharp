using System;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class LeafBlowerEffects : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private GameObject gunBarrel;

	[SerializeField]
	private float projectionRange;

	[SerializeField]
	private float projectionWidth;

	[SerializeField]
	private float headToleranceAngle;

	[SerializeField]
	private LayerMask raycastLayers;

	[SerializeField]
	private ParticleSystem angledHitParticleSystem;

	[SerializeField]
	private ParticleSystem squareHitParticleSystem;

	[SerializeField]
	private float squareHitAngle;

	[SerializeField]
	private CosmeticRefID fanRef;

	private float headToleranceAngleCos;

	private float squareHitAngleCos;

	private CosmeticFan fan;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnDespawn()
	{
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		headToleranceAngleCos = Mathf.Cos(MathF.PI / 180f * headToleranceAngle);
		squareHitAngleCos = Mathf.Cos(MathF.PI / 180f * squareHitAngle);
		fan = rig.cosmeticReferences.Get(fanRef).GetComponent<CosmeticFan>();
	}

	public void StartFan()
	{
		fan.Run();
	}

	public void StopFan()
	{
		fan.Stop();
	}

	public void UpdateEffects()
	{
		ProjectParticles();
		BlowFaces();
	}

	public void ProjectParticles()
	{
		if (Physics.Raycast(gunBarrel.transform.position, gunBarrel.transform.forward, out var hitInfo, projectionRange, raycastLayers))
		{
			SpawnOnEnter component = hitInfo.collider.GetComponent<SpawnOnEnter>();
			if (component != null)
			{
				component.OnTriggerEnter(hitInfo.collider);
			}
			if (Vector3.Dot(hitInfo.normal, gunBarrel.transform.forward) < 0f - squareHitAngleCos)
			{
				squareHitParticleSystem.transform.position = hitInfo.point;
				squareHitParticleSystem.transform.rotation = Quaternion.LookRotation(hitInfo.normal, gunBarrel.transform.forward);
				if (angledHitParticleSystem != squareHitParticleSystem && angledHitParticleSystem.isPlaying)
				{
					angledHitParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				}
				if (!squareHitParticleSystem.isPlaying)
				{
					squareHitParticleSystem.Play(withChildren: true);
				}
			}
			else
			{
				angledHitParticleSystem.transform.position = hitInfo.point;
				angledHitParticleSystem.transform.rotation = Quaternion.LookRotation(hitInfo.normal, gunBarrel.transform.forward);
				if (angledHitParticleSystem != squareHitParticleSystem && squareHitParticleSystem.isPlaying)
				{
					squareHitParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				}
				if (!angledHitParticleSystem.isPlaying)
				{
					angledHitParticleSystem.Play(withChildren: true);
				}
			}
		}
		else
		{
			StopEffects();
		}
	}

	public void StopEffects()
	{
		angledHitParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		squareHitParticleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}

	public void BlowFaces()
	{
		Vector3 position = gunBarrel.transform.position;
		Vector3 forward = gunBarrel.transform.forward;
		if (NetworkSystem.Instance.InRoom)
		{
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				TryBlowFace(activeRigContainer.Rig, position, forward);
			}
			return;
		}
		TryBlowFace(VRRig.LocalRig, position, forward);
	}

	private void TryBlowFace(VRRig rig, Vector3 origin, Vector3 directionNormalized)
	{
		Transform rigTarget = rig.head.rigTarget;
		Vector3 vector = rigTarget.position - origin;
		float num = Vector3.Dot(vector, directionNormalized);
		if (!(num < 0f) && !(num > projectionRange) && !(vector - num * directionNormalized).IsLongerThan(projectionWidth) && !(Vector3.Dot(-rigTarget.forward, vector.normalized) < headToleranceAngleCos))
		{
			rig.GetComponent<GorillaMouthFlap>().EnableLeafBlower();
		}
	}
}
