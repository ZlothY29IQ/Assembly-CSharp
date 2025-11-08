using System.Collections.Generic;
using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PropHuntHandFollower : MonoBehaviour, ICallBack
{
	private const bool _k__GT_PROP_HUNT__USE_POOLING__ = true;

	private const bool _k_isBetaOrEditor = false;

	private const float HandFollowDistance = 0.1f;

	private bool _hasProp;

	private bool _isLocal;

	private GameObject _prop;

	private bool _isLeftHand;

	private Vector3 _propOffset;

	private readonly List<MeshCollider> _colliders = new List<MeshCollider>(4);

	private readonly List<InteractionPoint> _interactionPoints = new List<InteractionPoint>(4);

	private Vector3 _lastRelativePos;

	private Quaternion _lastRelativeAngle;

	private Vector3 _networkLastRelativePos;

	private Quaternion _networkLastRelativeAngle;

	public LayerMask collisionLayers;

	private Vector3 targetPoint;

	private RaycastHit[] raycastHits;

	private PropHuntGrabbableProp _grabbableProp;

	private PropHuntTaggableProp _taggableProp;

	public bool hasProp
	{
		get
		{
			return _hasProp;
		}
		private set
		{
			_hasProp = value;
		}
	}

	public bool IsInstantiatingAsync { get; private set; }

	public VRRig attachedToRig { get; private set; }

	public bool IsLeftHand => _isLeftHand;

	public void Awake()
	{
		attachedToRig = GetComponent<VRRig>();
		attachedToRig.propHuntHandFollower = this;
		_isLocal = attachedToRig.isOfflineVRRig;
		raycastHits = new RaycastHit[20];
	}

	public void Start()
	{
		attachedToRig.AddLateUpdateCallback(this);
	}

	private void OnEnable()
	{
		GorillaPropHuntGameManager.RegisterPropHandFollower(this);
	}

	private void OnDisable()
	{
		if (!GTAppState.isQuitting)
		{
			DestroyProp();
			GorillaPropHuntGameManager.UnregisterPropHandFollower(this);
		}
	}

	public void DestroyProp()
	{
		if (hasProp && !(_prop == null))
		{
			PropHuntTaggableProp component2;
			if (_prop.TryGetComponent<PropHuntGrabbableProp>(out var component))
			{
				PropHuntPools.ReturnGrabbableProp(component);
			}
			else if (_prop.TryGetComponent<PropHuntTaggableProp>(out component2))
			{
				PropHuntPools.ReturnTaggableProp(component2);
			}
			_prop = null;
			hasProp = false;
		}
	}

	public static void DestroyProp_NoPool(List<MeshCollider> _colliders, ref bool hasProp, ref GameObject _prop)
	{
		foreach (MeshCollider _collider in _colliders)
		{
			if (!(_collider == null))
			{
				_collider.gameObject.transform.parent = null;
				_collider.gameObject.SetActive(value: false);
			}
		}
		if (hasProp)
		{
			Object.Destroy(_prop);
		}
		_prop = null;
		hasProp = false;
	}

	public void OnRoundStart()
	{
	}

	public void CreateProp()
	{
		if (hasProp)
		{
			DestroyProp();
		}
		_isLeftHand = false;
		int num = GorillaPropHuntGameManager.instance.GetSeed();
		if (NetworkSystem.Instance.InRoom)
		{
			num += attachedToRig.OwningNetPlayer.ActorNumber;
		}
		SRand sRand = new SRand(num);
		string cosmeticId = GorillaPropHuntGameManager.instance.GetCosmeticId(sRand.NextUInt());
		PropHuntTaggableProp out_prop2;
		if (_isLocal)
		{
			if (PropHuntPools.TryGetGrabbableProp(cosmeticId, out var out_prop))
			{
				_grabbableProp = out_prop;
				_taggableProp = null;
				_prop = out_prop.gameObject;
				_propOffset = _grabbableProp.offset;
				out_prop.handFollower = this;
				hasProp = true;
				for (int i = 0; i < out_prop.interactionPoints.Count; i++)
				{
					out_prop.interactionPoints[i].OnSpawn(attachedToRig);
				}
			}
		}
		else if (PropHuntPools.TryGetTaggableProp(cosmeticId, out out_prop2))
		{
			_taggableProp = out_prop2;
			_grabbableProp = null;
			_prop = out_prop2.gameObject;
			_propOffset = out_prop2.offset;
			out_prop2.ownerRig = attachedToRig;
			hasProp = true;
		}
	}

	public void OnPropLoaded(AsyncOperationHandle<GameObject> handle)
	{
		IsInstantiatingAsync = false;
		CosmeticSO debugCosmeticSO = null;
		if (!TryPrepPropTemplate(handle.Result, _isLocal, debugCosmeticSO, _colliders, _interactionPoints, out _grabbableProp, out _taggableProp))
		{
			return;
		}
		_prop = handle.Result;
		hasProp = _prop != null;
		_prop.SetActive(value: true);
		if (_isLocal)
		{
			_propOffset = _grabbableProp.offset;
			_grabbableProp.handFollower = this;
			for (int i = 0; i < _interactionPoints.Count; i++)
			{
				_interactionPoints[i].OnSpawn(attachedToRig);
			}
		}
		else
		{
			_propOffset = _taggableProp.offset;
			_taggableProp.ownerRig = attachedToRig;
		}
	}

	public static bool TryPrepPropTemplate(GameObject _prop, bool _isLocal, CosmeticSO debugCosmeticSO, List<MeshCollider> _colliders, List<InteractionPoint> ref_interactionPoints, out PropHuntGrabbableProp grabbableProp, out PropHuntTaggableProp taggableProp)
	{
		if (_isLocal)
		{
			grabbableProp = _prop.AddComponent<PropHuntGrabbableProp>();
			taggableProp = null;
			grabbableProp.interactionPoints = ref_interactionPoints;
		}
		else
		{
			taggableProp = _prop.AddComponent<PropHuntTaggableProp>();
			grabbableProp = null;
		}
		bool flag = false;
		bool flag2 = true;
		Bounds bounds = default(Bounds);
		int num = 0;
		MeshRenderer[] componentsInChildren = _prop.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
			if (component == null)
			{
				continue;
			}
			Mesh sharedMesh = component.sharedMesh;
			if (sharedMesh == null || !sharedMesh.isReadable)
			{
				continue;
			}
			flag = true;
			if (flag2)
			{
				bounds = meshRenderer.bounds;
			}
			else
			{
				bounds.Encapsulate(meshRenderer.bounds);
			}
			MeshCollider meshCollider;
			if (num >= _colliders.Count)
			{
				GameObject gameObject = new GameObject("PropHuntTaggable");
				gameObject.layer = 14;
				meshCollider = gameObject.AddComponent<MeshCollider>();
				meshCollider.convex = true;
				meshCollider.isTrigger = true;
				if (_isLocal)
				{
					ref_interactionPoints.Add(gameObject.AddComponent<InteractionPoint>());
				}
				_colliders.Add(meshCollider);
			}
			else
			{
				meshCollider = _colliders[num];
				meshCollider.gameObject.SetActive(value: true);
			}
			meshCollider.transform.parent = _prop.transform;
			meshCollider.transform.position = meshRenderer.transform.position;
			meshCollider.transform.rotation = meshRenderer.transform.rotation;
			meshCollider.sharedMesh = sharedMesh;
			num++;
			flag2 = false;
		}
		if (!flag)
		{
			bool flag3 = true;
			DestroyProp_NoPool(_colliders, ref flag3, ref _prop);
			return false;
		}
		Vector3 offset = _prop.transform.InverseTransformPoint(bounds.center);
		if (_isLocal)
		{
			grabbableProp.interactionPoints = ref_interactionPoints;
			grabbableProp.offset = offset;
		}
		else
		{
			taggableProp.offset = offset;
		}
		return true;
	}

	void ICallBack.CallBack()
	{
		if (!hasProp || _prop.IsNull())
		{
			return;
		}
		Transform transform = (_isLeftHand ? attachedToRig.leftHand.rigTarget : attachedToRig.rightHand.rigTarget);
		Vector3 sourcePos = transform.position;
		if (attachedToRig.isLocal)
		{
			sourcePos = (_isLeftHand ? attachedToRig.leftHand.overrideTarget.position : attachedToRig.rightHand.overrideTarget.position);
		}
		if ((_isLeftHand ? Mathf.Max(attachedToRig.leftIndex.calcT, attachedToRig.leftMiddle.calcT) : Mathf.Max(attachedToRig.rightIndex.calcT, attachedToRig.rightMiddle.calcT)) > 0.5f)
		{
			_prop.transform.rotation = transform.TransformRotation(_lastRelativeAngle);
			_prop.transform.position = GeoCollisionPoint(sourcePos, transform.TransformPoint(_lastRelativePos) + _prop.transform.TransformVector(_propOffset)) - _prop.transform.TransformVector(_propOffset);
			_networkLastRelativePos = transform.InverseTransformPoint(_prop.transform.position);
			_networkLastRelativeAngle = transform.InverseTransformRotation(_prop.transform.rotation);
			return;
		}
		Vector3 v = transform.transform.position - _prop.transform.TransformPoint(_propOffset);
		if (v.IsLongerThan(GorillaPropHuntGameManager.instance.HandFollowDistance))
		{
			float num = v.magnitude - GorillaPropHuntGameManager.instance.HandFollowDistance;
			_prop.transform.position = GeoCollisionPoint(sourcePos, _prop.transform.position + _prop.transform.TransformVector(_propOffset) + v.normalized * num) - _prop.transform.TransformVector(_propOffset);
		}
		_lastRelativePos = transform.InverseTransformPoint(_prop.transform.position);
		_lastRelativeAngle = transform.InverseTransformRotation(_prop.transform.rotation);
		_networkLastRelativePos = _lastRelativePos;
		_networkLastRelativeAngle = _lastRelativeAngle;
	}

	public Vector3 GeoCollisionPoint(Vector3 sourcePos, Vector3 targetPos)
	{
		Vector3 vector = targetPos - sourcePos;
		int num = Physics.RaycastNonAlloc(sourcePos, vector.normalized, raycastHits, vector.magnitude, collisionLayers, QueryTriggerInteraction.Ignore);
		if (num > 0)
		{
			float sqrMagnitude = vector.sqrMagnitude;
			Vector3 result = targetPos;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector2 = raycastHits[i].point - sourcePos;
				if (vector2.sqrMagnitude < sqrMagnitude)
				{
					result = raycastHits[i].point;
					sqrMagnitude = vector2.sqrMagnitude;
				}
			}
			return result;
		}
		return targetPos;
	}

	public void SwitchHand(bool newIsLeftHand)
	{
		if (_isLeftHand != newIsLeftHand)
		{
			_isLeftHand = newIsLeftHand;
			Transform transform = (_isLeftHand ? attachedToRig.leftHand.rigTarget : attachedToRig.rightHand.rigTarget);
			_lastRelativePos = transform.InverseTransformPoint(_prop.transform.position);
			_lastRelativeAngle = transform.InverseTransformRotation(_prop.transform.rotation);
		}
	}

	public void SetProp(bool isLeftHand, Vector3 propPos, Quaternion propRot)
	{
		_isLeftHand = isLeftHand;
		_lastRelativePos = propPos;
		_lastRelativeAngle = propRot;
	}

	public long GetRelativePosRotLong()
	{
		if (_prop.IsNull())
		{
			return BitPackUtils.PackHandPosRotForNetwork(Vector3.zero, Quaternion.identity);
		}
		return BitPackUtils.PackHandPosRotForNetwork(_lastRelativePos, _lastRelativeAngle);
	}
}
