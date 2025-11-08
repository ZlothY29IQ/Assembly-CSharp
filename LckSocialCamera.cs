using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using GorillaExtensions;
using GorillaTag;
using Liv.Lck.GorillaTag;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(1)]
public class LckSocialCamera : NetworkComponent, IGorillaSliceableSimple
{
	private enum CameraState
	{
		Empty,
		Visible,
		Recording
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
	[NetworkStructWeaved(1)]
	private struct CameraData : INetworkStruct
	{
		[FieldOffset(0)]
		public CameraState currentState;

		public CameraData(CameraState currentState)
		{
			this.currentState = currentState;
		}
	}

	private struct CameraDataLocal
	{
		public CameraState currentState;
	}

	[SerializeField]
	private Transform _scaleTransform;

	[SerializeField]
	public CoconutCamera CoconutCamera;

	[SerializeField]
	private List<GameObject> _visualObjects;

	[SerializeField]
	private VRRig _vrrig;

	[SerializeField]
	private VRRigSerializer m_rigNetworkController;

	private LCKSocialCameraFollower m_coconutCamera;

	private bool m_isCorrupted = true;

	private bool m_lckDelegateRegistered;

	private CameraDataLocal _localData;

	[WeaverGenerated]
	[DefaultForProperty("_networkedData", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private CameraData __networkedData;

	[Networked]
	[NetworkedWeaved(0, 1)]
	private unsafe ref CameraData _networkedData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing LckSocialCamera._networkedData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return ref *(CameraData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
	}

	private CameraState currentState
	{
		get
		{
			return _localData.currentState;
		}
		set
		{
			_localData.currentState = value;
			if (base.IsLocallyOwned)
			{
				CoconutCamera.SetVisualsActive(active: false);
				CoconutCamera.SetRecordingState(isRecording: false);
			}
			else
			{
				CoconutCamera.SetVisualsActive(visible);
				CoconutCamera.SetRecordingState(recording);
			}
		}
	}

	public bool visible
	{
		get
		{
			return GetFlag(currentState, CameraState.Visible);
		}
		set
		{
			currentState = SetFlag(currentState, CameraState.Visible, value);
		}
	}

	public bool recording
	{
		get
		{
			return GetFlag(currentState, CameraState.Recording);
		}
		set
		{
			currentState = SetFlag(currentState, CameraState.Recording, value);
		}
	}

	private static bool GetFlag(CameraState cameraState, CameraState flag)
	{
		return (cameraState & flag) == flag;
	}

	private static CameraState SetFlag(CameraState cameraState, CameraState flag, bool value)
	{
		cameraState = ((!value) ? (cameraState & ~flag) : (cameraState | flag));
		return cameraState;
	}

	public override void WriteDataFusion()
	{
		_networkedData = new CameraData(_localData.currentState);
	}

	public override void ReadDataFusion()
	{
		if (!m_isCorrupted)
		{
			ReadDataShared(_networkedData.currentState);
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(currentState);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == info.photonView.Owner && !m_isCorrupted)
		{
			CameraState newState = (CameraState)stream.ReceiveNext();
			ReadDataShared(newState);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (m_rigNetworkController.IsNull())
		{
			m_rigNetworkController = GetComponentInParent<VRRigSerializer>();
		}
		if (!m_rigNetworkController.IsNull())
		{
			m_rigNetworkController.SuccesfullSpawnEvent.Add(new InAction<RigContainer, PhotonMessageInfoWrapped>(OnSuccesfullSpawn));
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		if (m_lckDelegateRegistered)
		{
			LckSocialCameraManager.OnManagerSpawned = (Action<LckSocialCameraManager>)Delegate.Remove(LckSocialCameraManager.OnManagerSpawned, new Action<LckSocialCameraManager>(OnManagerSpawned));
		}
	}

	protected override void Start()
	{
	}

	private void OnSuccesfullSpawn(in RigContainer rig, in PhotonMessageInfoWrapped info)
	{
		_vrrig = rig.Rig;
		LCKSocialCameraFollower lCKCoconutCamera = rig.LCKCoconutCamera;
		_scaleTransform = lCKCoconutCamera.ScaleTransform;
		CoconutCamera = lCKCoconutCamera.CoconutCamera;
		_visualObjects = lCKCoconutCamera.VisualObjects;
		m_coconutCamera = lCKCoconutCamera;
		m_isCorrupted = false;
		if (_vrrig.isOfflineVRRig)
		{
			LckSocialCameraManager instance = LckSocialCameraManager.Instance;
			if (instance != null)
			{
				instance.SetLckSocialCamera(this);
			}
			else
			{
				LckSocialCameraManager.OnManagerSpawned = (Action<LckSocialCameraManager>)Delegate.Combine(LckSocialCameraManager.OnManagerSpawned, new Action<LckSocialCameraManager>(OnManagerSpawned));
				m_lckDelegateRegistered = true;
			}
		}
		else
		{
			lCKCoconutCamera.SetNetworkController(this);
		}
		visible = visible;
	}

	private void StoreRigReference()
	{
		if (base.Owner != null && !base.Owner.IsNull && VRRigCache.Instance.TryGetVrrig(base.Owner, out var playerRig))
		{
			_vrrig = playerRig.Rig;
		}
	}

	public void SliceUpdate()
	{
		if (!_vrrig.IsNull())
		{
			CoconutCamera.transform.localScale = Vector3.one * _vrrig.scaleFactor;
		}
	}

	public new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (!m_isCorrupted)
		{
			if (m_coconutCamera.IsNotNull())
			{
				m_coconutCamera.RemoveNetworkController(this);
			}
			_scaleTransform = null;
			_visualObjects = null;
			CoconutCamera = null;
		}
	}

	private void OnManagerSpawned(LckSocialCameraManager manager)
	{
		manager.SetLckSocialCamera(this);
	}

	private void ReadDataShared(CameraState newState)
	{
		currentState = newState;
	}

	public void TurnOff()
	{
		m_isCorrupted = true;
		base.gameObject.SetActive(value: false);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		_networkedData = __networkedData;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		__networkedData = _networkedData;
	}
}
