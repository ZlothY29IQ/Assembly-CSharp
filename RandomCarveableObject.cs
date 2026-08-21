using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using Voxels;

[NetworkBehaviourWeaved(0)]
public class RandomCarveableObject : NetworkComponent
{
	[SerializeField]
	private Transform spawnPoint;

	[SerializeField]
	private GameObject[] prefabs;

	[SerializeField]
	private byte materialId;

	[SerializeField]
	private VoxelWorld world;

	[SerializeField]
	private GameObject spawnFX;

	[SerializeField]
	private CallLimiter spamCheck = new CallLimiter(1, 1f);

	[SerializeField]
	private RigEventVolume proximityTrigger;

	[SerializeField]
	private GorillaPressableButton button;

	private int _carveableIndex = -1;

	private int _version;

	private GameObject _carveable;

	private int3[] _voxels;

	private UnityEngine.BoundsInt _voxelBounds;

	private bool _buttonConfigured;

	private bool _canRequestNewCarveable;

	private bool HasAuthority => VoxelManager.HasAuthority;

	private new void Start()
	{
		if ((object)world == null)
		{
			world = GetComponentInParent<VoxelWorld>();
		}
		spawnFX.SetActive(value: false);
		for (int num = spawnPoint.childCount - 1; num >= 0; num--)
		{
			JamUtil.Destroy(spawnPoint.GetChild(num).gameObject);
		}
		proximityTrigger.OnCountChanged += OnPlayerCountChanged;
		OnPlayerCountChanged();
		Init();
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		proximityTrigger.OnCountChanged -= OnPlayerCountChanged;
	}

	private void Init()
	{
		if (HasAuthority)
		{
			SpawnRandomCarveable();
		}
		else if (_version != 0 && IsValidPrefab(_carveableIndex))
		{
			SpawnCarveable(_carveableIndex);
		}
	}

	private void ClearWorld()
	{
		world.SetVoxels(world.WorldBounds, 0, materialId, immediate: false);
	}

	private void FillBounds()
	{
		SetBoundsDensity(byte.MaxValue);
	}

	private void SetBoundsDensity(byte density)
	{
		world.SetVoxels(_voxels, density, materialId, immediate: false);
	}

	private void SetCarveable(int index)
	{
		_carveableIndex = index;
		for (int num = spawnPoint.childCount - 1; num >= 0; num--)
		{
			JamUtil.Destroy(spawnPoint.GetChild(num).gameObject);
		}
		_carveable = UnityEngine.Object.Instantiate(prefabs[_carveableIndex], spawnPoint.position, spawnPoint.rotation, spawnPoint);
		_carveable.transform.localScale = Vector3.one;
	}

	private void OnPlayerCountChanged()
	{
		SetCanRequestNewCarveable(proximityTrigger.RigCount == 0);
	}

	private void SetCanRequestNewCarveable(bool active)
	{
		if (!_buttonConfigured || _canRequestNewCarveable != active)
		{
			button.isOn = active;
			button.UpdateColor();
			_buttonConfigured = true;
			_canRequestNewCarveable = active;
		}
	}

	public void RequestSpawnRandomCarveable()
	{
		if (_canRequestNewCarveable)
		{
			Debug.Log("RequestSpawnRandomCarveable()");
			if (HasAuthority)
			{
				SpawnRandomCarveable();
			}
			else
			{
				base.GetView.RPC("RPC_SpawnRandomCarveable", RpcTarget.MasterClient);
			}
		}
	}

	private bool IsValidAuthorityRPC(PhotonMessageInfo info)
	{
		if (VoxelManager.HasAuthority)
		{
			return spamCheck.CheckCallTime(Time.unscaledTime);
		}
		return false;
	}

	[PunRPC]
	public void RPC_SpawnRandomCarveable(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RPC_SpawnRandomCarveable");
		if (IsValidAuthorityRPC(info))
		{
			SpawnRandomCarveable();
		}
	}

	private void SpawnRandomCarveable()
	{
		SpawnCarveable(UnityEngine.Random.Range(0, prefabs.Length));
	}

	private async void SpawnCarveable(int index)
	{
		_ = 1;
		try
		{
			if (!IsValidPrefab(index))
			{
				Debug.LogError($"Invalid index: {index}");
				return;
			}
			spawnFX.SetActive(value: false);
			await UniTask.WaitUntil(() => world.WorldGenerationComplete);
			SetCarveable(index);
			_carveable.gameObject.SetActive(value: false);
			world.ResetChunk(int3.zero);
			await UniTask.WaitUntil(() => world.BoundsChunksLoaded(world.WorldBounds), PlayerLoopTiming.PostLateUpdate);
			_carveable.gameObject.SetActive(value: true);
			spawnFX.SetActive(value: true);
			if (HasAuthority)
			{
				IncrementVersion();
			}
			else
			{
				VoxelManager.RequestWorldState(world);
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void IncrementVersion()
	{
		_version++;
	}

	private bool IsValidPrefab(int index)
	{
		if (index >= 0)
		{
			return index < prefabs.Length;
		}
		return false;
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(_carveableIndex);
		stream.SendNext(_version);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == info.photonView.Owner)
		{
			int num = (int)stream.ReceiveNext();
			int num2 = (int)stream.ReceiveNext();
			if (_carveableIndex != num || _version != num2)
			{
				OnStateChange(num, num2);
			}
		}
	}

	public void OnStateChange(int newIndex, int newVersion)
	{
		_carveableIndex = newIndex;
		_version = newVersion;
		IsValidPrefab(_carveableIndex);
		if (IsValidPrefab(_carveableIndex))
		{
			SpawnCarveable(_carveableIndex);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
