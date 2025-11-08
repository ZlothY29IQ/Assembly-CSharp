using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderProjectileTarget : MonoBehaviour, IBuilderPieceFunctional
{
	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private SlingshotProjectileHitNotifier hitNotifier;

	[SerializeField]
	protected float hitCooldown = 2f;

	[Tooltip("Optional Sounds to play on hit")]
	[SerializeField]
	protected SoundBankPlayer hitSoundbank;

	[Tooltip("Optional Sounds to play on hit")]
	[SerializeField]
	protected Animation hitAnimation;

	[SerializeField]
	protected List<Collider> colliders;

	[SerializeField]
	private TMP_Text scoreText;

	private double lastHitTime;

	private int hitCount;

	private const byte MAX_SCORE = 10;

	private const byte HIT = 11;

	private void Awake()
	{
		hitNotifier.OnProjectileHit += OnProjectileHit;
		foreach (Collider collider in colliders)
		{
			collider.contactOffset = 0.0001f;
		}
	}

	private void OnDestroy()
	{
		hitNotifier.OnProjectileHit -= OnProjectileHit;
	}

	private void OnDisable()
	{
		hitCount = 0;
		if (scoreText != null)
		{
			scoreText.text = hitCount.ToString("D2");
		}
	}

	private void OnProjectileHit(SlingshotProjectile projectile, Collision collision)
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced && projectile.projectileOwner != null && projectile.projectileOwner == NetworkSystem.Instance.LocalPlayer && lastHitTime + (double)hitCooldown < (double)Time.time)
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 11);
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (instigator != null && IsStateValid(newState) && newState != 11)
		{
			lastHitTime = Time.time;
			hitCount = Mathf.Clamp(newState, 0, 10);
			PlayHitEffects();
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && newState == 11)
		{
			hitCount++;
			hitCount %= 11;
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, (byte)hitCount, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public bool IsStateValid(byte state)
	{
		return state <= 11;
	}

	private void PlayHitEffects()
	{
		if (hitSoundbank != null)
		{
			hitSoundbank.Play();
		}
		if (hitAnimation != null && hitAnimation.clip != null)
		{
			hitAnimation.Play();
		}
		if (scoreText != null)
		{
			scoreText.text = hitCount.ToString("D2");
		}
	}

	public void FunctionalPieceUpdate()
	{
	}

	public float GetInteractionDistace()
	{
		return 20f;
	}
}
