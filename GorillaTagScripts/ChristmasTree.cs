using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

[NetworkBehaviourWeaved(1)]
public class ChristmasTree : NetworkComponent
{
	public GameObject hangers;

	public GameObject lights;

	public GameObject topOrnament;

	public float spinSpeed = 60f;

	private readonly List<AttachPoint> attachPointsList = new List<AttachPoint>();

	private MeshRenderer[] lightRenderers;

	private bool wasActive;

	private bool isActive;

	private bool spinTheTop;

	[SerializeField]
	private Material lightsOffMaterial;

	[SerializeField]
	private Material[] lightsOnMaterials;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private NetworkBool _Data;

	[Networked]
	[NetworkedWeaved(0, 1)]
	private unsafe NetworkBool Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ChristmasTree.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(NetworkBool*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ChristmasTree.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(NetworkBool*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		AttachPoint[] componentsInChildren = hangers.GetComponentsInChildren<AttachPoint>();
		foreach (AttachPoint attachPoint in componentsInChildren)
		{
			attachPointsList.Add(attachPoint);
			attachPoint.onHookedChanged = (UnityAction)Delegate.Combine(attachPoint.onHookedChanged, new UnityAction(UpdateHangers));
		}
		lightRenderers = lights.GetComponentsInChildren<MeshRenderer>();
		MeshRenderer[] array = lightRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].material = lightsOffMaterial;
		}
		wasActive = false;
		isActive = false;
	}

	private void Update()
	{
		if (spinTheTop && (bool)topOrnament)
		{
			topOrnament.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		foreach (AttachPoint attachPoints in attachPointsList)
		{
			attachPoints.onHookedChanged = (UnityAction)Delegate.Remove(attachPoints.onHookedChanged, new UnityAction(UpdateHangers));
		}
		attachPointsList.Clear();
	}

	private void UpdateHangers()
	{
		if (attachPointsList.Count == 0)
		{
			return;
		}
		foreach (AttachPoint attachPoints in attachPointsList)
		{
			if (attachPoints.IsHooked())
			{
				if (base.IsMine)
				{
					updateLight(enable: true);
				}
				return;
			}
		}
		if (base.IsMine)
		{
			updateLight(enable: false);
		}
	}

	private void updateLight(bool enable)
	{
		isActive = enable;
		for (int i = 0; i < lightRenderers.Length; i++)
		{
			lightRenderers[i].material = (enable ? lightsOnMaterials[i % lightsOnMaterials.Length] : lightsOffMaterial);
		}
		spinTheTop = enable;
	}

	public override void WriteDataFusion()
	{
		Data = isActive;
	}

	public override void ReadDataFusion()
	{
		wasActive = isActive;
		isActive = Data;
		if (wasActive != isActive)
		{
			updateLight(isActive);
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			stream.SendNext(isActive);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			wasActive = isActive;
			isActive = (bool)stream.ReceiveNext();
			if (wasActive != isActive)
			{
				updateLight(isActive);
			}
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
