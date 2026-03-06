using System;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;

public class BuilderPaintBrush : HoldableObject
{
	public enum PaintBrushState
	{
		Inactive,
		HeldRemote,
		Held,
		Hover,
		JustPainted
	}

	[SerializeField]
	private Transform brushSurface;

	[SerializeField]
	private Vector3 paintVolumeHalfExtents;

	[SerializeField]
	private BuilderMaterialOptions paintBrushMaterialOptions;

	[SerializeField]
	private MeshRenderer brushRenderer;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip paintSound;

	[SerializeField]
	private AudioClip brushStrokeSound;

	private GameObject holdingHand;

	private bool inLeftHand;

	private GorillaVelocityTracker handVelocity;

	private BuilderPiece hoveredPiece;

	private Collider hoveredPieceCollider;

	private Collider[] hitColliders = new Collider[16];

	private LayerMask pieceLayers = 0;

	private Vector3 lastPosition = Vector3.zero;

	private float positionDelta;

	private float wiggleDistanceRequirement = 0.08f;

	private float minimumWiggleFrameDistance = 0.005f;

	private float maximumWiggleFrameDistance = 0.04f;

	private float maxPaintVelocitySqrMag = 0.5f;

	private float paintDelay = 0.2f;

	private float paintTimeElapsed = -1f;

	private float paintDistance;

	private int materialType = -1;

	private PaintBrushState brushState;

	private Rigidbody rb;

	private void Awake()
	{
		pieceLayers = (int)pieceLayers | (1 << LayerMask.NameToLayer("Gorilla Object"));
		pieceLayers = (int)pieceLayers | (1 << LayerMask.NameToLayer("BuilderProp"));
		pieceLayers = (int)pieceLayers | (1 << LayerMask.NameToLayer("Prop"));
		paintDistance = Vector3.SqrMagnitude(paintVolumeHalfExtents);
		rb = GetComponent<Rigidbody>();
	}

	public override void DropItemCleanup()
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		holdingHand = grabbingHand;
		handVelocity = grabbingHand.GetComponent<GorillaVelocityTracker>();
		if (handVelocity == null)
		{
			Debug.Log("No Velocity Estimator");
		}
		inLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
		BodyDockPositions myBodyDockPositions = GorillaTagger.Instance.offlineVRRig.myBodyDockPositions;
		rb.isKinematic = true;
		rb.useGravity = false;
		if (inLeftHand)
		{
			base.transform.SetParent(myBodyDockPositions.leftHandTransform, worldPositionStays: true);
		}
		else
		{
			base.transform.SetParent(myBodyDockPositions.rightHandTransform, worldPositionStays: true);
		}
		base.transform.localScale = Vector3.one;
		EquipmentInteractor.instance.UpdateHandEquipment(this, inLeftHand);
		GorillaTagger.Instance.StartVibration(inLeftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		brushState = PaintBrushState.Held;
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (base.OnRelease(zoneReleased, releasingHand))
		{
			holdingHand = null;
			EquipmentInteractor.instance.UpdateHandEquipment(null, inLeftHand);
			inLeftHand = false;
			handVelocity = null;
			ClearHoveredPiece();
			base.transform.parent = null;
			base.transform.localScale = Vector3.one;
			rb.isKinematic = false;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.useGravity = true;
			return true;
		}
		return false;
	}

	private void LateUpdate()
	{
		if (brushState != PaintBrushState.Inactive)
		{
			if (holdingHand == null || materialType == -1)
			{
				brushState = PaintBrushState.Inactive;
			}
			else
			{
				FindPieceToPaint();
			}
		}
	}

	private void FindPieceToPaint()
	{
		switch (brushState)
		{
		case PaintBrushState.Held:
		{
			if (materialType == -1)
			{
				break;
			}
			Array.Clear(hitColliders, 0, hitColliders.Length);
			int num2 = Physics.OverlapBoxNonAlloc(brushSurface.transform.position - brushSurface.up * paintVolumeHalfExtents.y, paintVolumeHalfExtents, hitColliders, brushSurface.transform.rotation, pieceLayers, QueryTriggerInteraction.Ignore);
			BuilderPieceCollider builderPieceCollider = null;
			Collider collider = null;
			float num3 = float.MaxValue;
			for (int i = 0; i < num2; i++)
			{
				BuilderPieceCollider component = hitColliders[i].GetComponent<BuilderPieceCollider>();
				if (component != null && component.piece.materialType != materialType && component.piece.materialType != -1)
				{
					float sqrMagnitude3 = (brushSurface.transform.position - component.transform.position).sqrMagnitude;
					if (sqrMagnitude3 < num3 && component.piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, component.piece.transform.position))
					{
						num3 = sqrMagnitude3;
						builderPieceCollider = component;
						collider = hitColliders[i];
					}
				}
			}
			if (builderPieceCollider != null)
			{
				ClearHoveredPiece();
				hoveredPiece = builderPieceCollider.piece;
				hoveredPieceCollider = collider;
				hoveredPiece.PaintingTint(enable: true);
				GorillaTagger.Instance.StartVibration(inLeftHand, GorillaTagger.Instance.tapHapticStrength / 4f, GorillaTagger.Instance.tapHapticDuration);
				positionDelta = 0f;
				lastPosition = brushSurface.transform.position;
				brushState = PaintBrushState.Hover;
			}
			break;
		}
		case PaintBrushState.Hover:
		{
			if (hoveredPiece == null || hoveredPieceCollider == null)
			{
				ClearHoveredPiece();
				break;
			}
			float sqrMagnitude = handVelocity.GetLatestVelocity().sqrMagnitude;
			float sqrMagnitude2 = handVelocity.GetAverageVelocity().sqrMagnitude;
			if (handVelocity != null && (sqrMagnitude > maxPaintVelocitySqrMag || sqrMagnitude2 > maxPaintVelocitySqrMag))
			{
				ClearHoveredPiece();
				break;
			}
			Vector3 vector = brushSurface.position - brushSurface.up * paintVolumeHalfExtents.y;
			Vector3 vector2 = hoveredPieceCollider.ClosestPointOnBounds(vector);
			if (Vector3.SqrMagnitude(vector - vector2) > paintDistance)
			{
				ClearHoveredPiece();
				break;
			}
			GorillaTagger.Instance.StartVibration(inLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, Time.deltaTime);
			float num = Vector3.Distance(lastPosition, brushSurface.position);
			if (num < minimumWiggleFrameDistance)
			{
				lastPosition = brushSurface.position;
				break;
			}
			positionDelta += Math.Min(num, maximumWiggleFrameDistance);
			lastPosition = brushSurface.position;
			if (positionDelta >= wiggleDistanceRequirement)
			{
				positionDelta = 0f;
				audioSource.clip = paintSound;
				audioSource.GTPlay();
				PaintPiece();
				brushState = PaintBrushState.JustPainted;
			}
			break;
		}
		case PaintBrushState.JustPainted:
			if (paintTimeElapsed > paintDelay)
			{
				paintTimeElapsed = 0f;
				brushState = PaintBrushState.Held;
			}
			else
			{
				paintTimeElapsed += Time.deltaTime;
			}
			break;
		}
	}

	private void PaintPiece()
	{
		hoveredPiece.GetTable().RequestPaintPiece(hoveredPiece.pieceId, materialType);
		hoveredPiece.PaintingTint(enable: false);
		hoveredPiece = null;
		hoveredPieceCollider = null;
		paintTimeElapsed = 0f;
		GorillaTagger.Instance.StartVibration(inLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
	}

	private void ClearHoveredPiece()
	{
		if (hoveredPiece != null)
		{
			hoveredPiece.PaintingTint(enable: false);
		}
		hoveredPiece = null;
		hoveredPieceCollider = null;
		positionDelta = 0f;
		brushState = ((!(holdingHand == null) && materialType != -1) ? PaintBrushState.Held : PaintBrushState.Inactive);
	}

	public void SetBrushMaterial(int inMaterialType)
	{
		materialType = inMaterialType;
		audioSource.clip = paintSound;
		audioSource.GTPlay();
		if (holdingHand != null)
		{
			GorillaTagger.Instance.StartVibration(inLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
		}
		if (materialType == -1)
		{
			ClearHoveredPiece();
		}
		else if (brushState == PaintBrushState.Inactive && holdingHand != null)
		{
			brushState = PaintBrushState.Held;
		}
		if (paintBrushMaterialOptions != null && brushRenderer != null)
		{
			paintBrushMaterialOptions.GetMaterialFromType(materialType, out var material, out var _);
			if (material != null)
			{
				brushRenderer.material = material;
			}
		}
	}
}
