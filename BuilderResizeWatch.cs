using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaTagScripts;
using UnityEngine;

public class BuilderResizeWatch : MonoBehaviour
{
	[Serializable]
	public struct BuilderSizeChangeSettings
	{
		public bool affectLayerA;

		public bool affectLayerB;

		public bool affectLayerC;

		public bool affectLayerD;
	}

	[SerializeField]
	private HeldButton enlargeButton;

	[SerializeField]
	private HeldButton shrinkButton;

	[SerializeField]
	private GameObject fxForLayerChange;

	private VRRig ownerRig;

	private SizeManager sizeManager;

	[HideInInspector]
	public Collider[] tempDisableColliders = new Collider[128];

	[HideInInspector]
	public List<BuilderPiece> collisionDisabledPieces = new List<BuilderPiece>();

	private float enableDist = 1f;

	private float enableDistSq = 1f;

	private bool updateCollision;

	private float growDelay = 1f;

	private double timeToCheckCollision;

	public BuilderSizeChangeSettings growSettings;

	public BuilderSizeChangeSettings shrinkSettings;

	public int SizeLayerMaskGrow
	{
		get
		{
			int num = 0;
			if (growSettings.affectLayerA)
			{
				num |= 1;
			}
			if (growSettings.affectLayerB)
			{
				num |= 2;
			}
			if (growSettings.affectLayerC)
			{
				num |= 4;
			}
			if (growSettings.affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}

	public int SizeLayerMaskShrink
	{
		get
		{
			int num = 0;
			if (shrinkSettings.affectLayerA)
			{
				num |= 1;
			}
			if (shrinkSettings.affectLayerB)
			{
				num |= 2;
			}
			if (shrinkSettings.affectLayerC)
			{
				num |= 4;
			}
			if (shrinkSettings.affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}

	private void Start()
	{
		if (enlargeButton != null)
		{
			enlargeButton.onPressButton.AddListener(OnEnlargeButtonPressed);
		}
		if (shrinkButton != null)
		{
			shrinkButton.onPressButton.AddListener(OnShrinkButtonPressed);
		}
		ownerRig = GetComponentInParent<VRRig>();
		enableDist = GTPlayer.Instance.bodyCollider.height;
		enableDistSq = enableDist * enableDist;
	}

	private void OnDestroy()
	{
		if (enlargeButton != null)
		{
			enlargeButton.onPressButton.RemoveListener(OnEnlargeButtonPressed);
		}
		if (shrinkButton != null)
		{
			shrinkButton.onPressButton.RemoveListener(OnShrinkButtonPressed);
		}
	}

	private void OnEnlargeButtonPressed()
	{
		if (sizeManager == null)
		{
			if (ownerRig == null)
			{
				Debug.LogWarning("Builder resize watch has no owner rig");
				return;
			}
			sizeManager = ownerRig.sizeManager;
		}
		if (sizeManager != null && sizeManager.currentSizeLayerMaskValue != SizeLayerMaskGrow && !updateCollision)
		{
			DisableCollisionWithPieces();
			sizeManager.currentSizeLayerMaskValue = SizeLayerMaskGrow;
			if (fxForLayerChange != null)
			{
				ObjectPools.instance.Instantiate(fxForLayerChange, ownerRig.transform.position);
			}
			timeToCheckCollision = Time.time + growDelay;
			updateCollision = true;
		}
	}

	private void DisableCollisionWithPieces()
	{
		if (!BuilderTable.TryGetBuilderTableForZone(ownerRig.zoneEntity.currentZone, out var table))
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(GTPlayer.Instance.headCollider.transform.position, 1f, tempDisableColliders, table.allPiecesMask);
		for (int i = 0; i < num; i++)
		{
			BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(tempDisableColliders[i]);
			if (!(builderPieceFromCollider != null) || builderPieceFromCollider.state != BuilderPiece.State.AttachedAndPlaced || builderPieceFromCollider.isBuiltIntoTable || collisionDisabledPieces.Contains(builderPieceFromCollider))
			{
				continue;
			}
			foreach (Collider collider in builderPieceFromCollider.colliders)
			{
				collider.enabled = false;
			}
			foreach (Collider placedOnlyCollider in builderPieceFromCollider.placedOnlyColliders)
			{
				placedOnlyCollider.enabled = false;
			}
			collisionDisabledPieces.Add(builderPieceFromCollider);
		}
	}

	private void EnableCollisionWithPieces()
	{
		for (int num = collisionDisabledPieces.Count - 1; num >= 0; num--)
		{
			BuilderPiece builderPiece = collisionDisabledPieces[num];
			if (builderPiece == null)
			{
				collisionDisabledPieces.RemoveAt(num);
			}
			else if (Vector3.SqrMagnitude(GTPlayer.Instance.bodyCollider.transform.position - builderPiece.transform.position) >= enableDistSq)
			{
				EnableCollisionWithPiece(builderPiece);
				collisionDisabledPieces.RemoveAt(num);
			}
		}
	}

	private void EnableCollisionWithPiece(BuilderPiece piece)
	{
		foreach (Collider collider in piece.colliders)
		{
			collider.enabled = piece.state != BuilderPiece.State.None && piece.state != BuilderPiece.State.Displayed;
		}
		foreach (Collider placedOnlyCollider in piece.placedOnlyColliders)
		{
			placedOnlyCollider.enabled = piece.state == BuilderPiece.State.AttachedAndPlaced;
		}
	}

	private void Update()
	{
		if (updateCollision && (double)Time.time >= timeToCheckCollision)
		{
			EnableCollisionWithPieces();
			if (collisionDisabledPieces.Count <= 0)
			{
				updateCollision = false;
			}
		}
	}

	private void OnShrinkButtonPressed()
	{
		if (sizeManager == null)
		{
			if (ownerRig == null)
			{
				Debug.LogWarning("Builder resize watch has no owner rig");
			}
			sizeManager = ownerRig.sizeManager;
		}
		if (sizeManager != null && sizeManager.currentSizeLayerMaskValue != SizeLayerMaskShrink)
		{
			sizeManager.currentSizeLayerMaskValue = SizeLayerMaskShrink;
		}
	}
}
