using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderProjectileLauncher : MonoBehaviour, IBuilderPieceFunctional, IBuilderPieceComponent
{
	private enum FunctionalState
	{
		Idle,
		Fire
	}

	private List<BuilderProjectile> launchedProjectiles = new List<BuilderProjectile>();

	[SerializeField]
	protected BuilderPiece myPiece;

	[SerializeField]
	protected float fireCooldown = 2f;

	[Tooltip("launch in Y direction")]
	[SerializeField]
	private Transform launchPosition;

	[SerializeField]
	private float launchVelocity;

	[SerializeField]
	private AudioSource launchSound;

	[SerializeField]
	protected GameObject projectilePrefab;

	protected float projectileScale = 0.06f;

	[SerializeField]
	protected float gravityMultiplier = 1f;

	public SlingshotProjectile.AOEKnockbackConfig knockbackConfig;

	private float lastFireTime;

	private FunctionalState currentState;

	private Dictionary<int, BuilderProjectile> allProjectiles = new Dictionary<int, BuilderProjectile>();

	private void LaunchProjectile(int timeStamp)
	{
		if (!(Time.time > lastFireTime + fireCooldown))
		{
			return;
		}
		lastFireTime = Time.time;
		int hash = PoolUtils.GameObjHashCode(projectilePrefab);
		try
		{
			GameObject obj = ObjectPools.instance.Instantiate(hash);
			projectileScale = myPiece.GetScale();
			obj.transform.localScale = Vector3.one * projectileScale;
			BuilderProjectile component = obj.GetComponent<BuilderProjectile>();
			int num = HashCode.Combine(myPiece.pieceId, timeStamp);
			if (allProjectiles.ContainsKey(num))
			{
				allProjectiles.Remove(num);
			}
			allProjectiles.Add(num, component);
			SlingshotProjectile.AOEKnockbackConfig value = new SlingshotProjectile.AOEKnockbackConfig
			{
				aeoOuterRadius = knockbackConfig.aeoOuterRadius * projectileScale,
				aeoInnerRadius = knockbackConfig.aeoInnerRadius * projectileScale,
				applyAOEKnockback = knockbackConfig.applyAOEKnockback,
				impactVelocityThreshold = knockbackConfig.impactVelocityThreshold * projectileScale,
				knockbackVelocity = knockbackConfig.knockbackVelocity * projectileScale,
				playerProximityEffect = knockbackConfig.playerProximityEffect
			};
			component.aoeKnockbackConfig = value;
			component.gravityMultiplier = gravityMultiplier;
			component.Launch(launchPosition.position, launchVelocity * projectileScale * launchPosition.up, this, num, projectileScale, timeStamp);
			if (launchSound != null && launchSound.clip != null)
			{
				launchSound.Play();
			}
		}
		catch (Exception value2)
		{
			Console.WriteLine(value2);
			throw;
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (IsStateValid(newState) && (FunctionalState)newState != currentState)
		{
			currentState = (FunctionalState)newState;
			if (newState == 1)
			{
				LaunchProjectile(timeStamp);
				myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			}
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
	}

	public bool IsStateValid(byte state)
	{
		return state <= 1;
	}

	public void FunctionalPieceUpdate()
	{
		for (int num = launchedProjectiles.Count - 1; num >= 0; num--)
		{
			launchedProjectiles[num].UpdateProjectile();
		}
		if (PhotonNetwork.IsMasterClient && lastFireTime + fireCooldown < Time.time)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, 1, PhotonNetwork.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		myPiece.GetTable().RegisterFunctionalPiece(this);
	}

	public void OnPieceDeactivate()
	{
		myPiece.GetTable().UnregisterFunctionalPiece(this);
		for (int num = launchedProjectiles.Count - 1; num >= 0; num--)
		{
			launchedProjectiles[num].Deactivate();
		}
	}

	public void RegisterProjectile(BuilderProjectile projectile)
	{
		launchedProjectiles.Add(projectile);
	}

	public void UnRegisterProjectile(BuilderProjectile projectile)
	{
		launchedProjectiles.Remove(projectile);
		allProjectiles.Remove(projectile.projectileId);
	}
}
