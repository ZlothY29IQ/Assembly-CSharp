using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

public class PropPlacementRB : MonoBehaviour, IDelayedExecListener
{
	[FormerlySerializedAs("rb")]
	[SerializeField]
	private Rigidbody m_rb;

	[FormerlySerializedAs("simDurationBeforeFreeze")]
	[SerializeField]
	private float m_simDurationBeforeFreeze;

	private PropHuntPropZone _parentZone;

	[SerializeField]
	internal GameObject _placingProp;

	[SerializeField]
	private MeshCollider[] _colliders;

	private bool _isInstantiatingAsync;

	protected void OnDestroy()
	{
		if (_placingProp != null)
		{
			Object.Destroy(_placingProp);
		}
	}

	public void PlaceProp_NoPool(PropHuntPropZone parentZone, GTAssetRef<GameObject> propRef, Vector3 pos, Quaternion rot, CosmeticSO debugCosmeticSO)
	{
		if (_isInstantiatingAsync)
		{
			Debug.LogError("ERROR!!!  PropPlacementRB: Tried to place (spawn) prop while one was already being placed.");
			return;
		}
		_parentZone = parentZone;
		MeshCollider[] colliders = _colliders;
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].gameObject.SetActive(value: false);
		}
		base.transform.position = pos;
		base.transform.rotation = rot;
		base.gameObject.SetActive(value: false);
		_isInstantiatingAsync = true;
		AsyncOperationHandle<GameObject> asyncOperationHandle = propRef.InstantiateAsync();
		asyncOperationHandle.Completed += OnPropLoaded_NoPool;
	}

	public void OnPropLoaded_NoPool(AsyncOperationHandle<GameObject> handle)
	{
		_isInstantiatingAsync = false;
		_placingProp = handle.Result;
		_placingProp.transform.position = base.transform.position;
		_placingProp.transform.rotation = base.transform.rotation;
		m_rb.linearVelocity = Vector3.zero;
		m_rb.angularVelocity = Vector3.zero;
		CosmeticSO debugCosmeticSO = null;
		if (!TryPrepPropTemplate(this, _placingProp, debugCosmeticSO))
		{
			DestroyProp_NoPool();
			return;
		}
		_placingProp.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		GTDelayedExec.Add(this, 2f, 0);
	}

	public static bool TryPrepPropTemplate(PropPlacementRB rb, GameObject rendererGobj, CosmeticSO _debugCosmeticSO)
	{
		rb._isInstantiatingAsync = false;
		rb._placingProp = rendererGobj;
		rb._placingProp.transform.position = rb.transform.position;
		rb._placingProp.transform.rotation = rb.transform.rotation;
		rb.m_rb.linearVelocity = Vector3.zero;
		rb.m_rb.angularVelocity = Vector3.zero;
		bool flag = false;
		MeshFilter[] componentsInChildren = rendererGobj.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		List<MeshCollider> value;
		using (ListPool<MeshCollider>.Get(out value))
		{
			value.Capacity = math.max(value.Capacity, 8);
			foreach (MeshFilter meshFilter in componentsInChildren)
			{
				Mesh sharedMesh = meshFilter.sharedMesh;
				if (!(sharedMesh == null) && sharedMesh.isReadable)
				{
					flag = true;
					GameObject obj = new GameObject(meshFilter.name + "__PropHuntDecoy_Collider");
					obj.transform.parent = rb.transform;
					obj.layer = 30;
					MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
					meshCollider.convex = true;
					meshCollider.transform.position = meshFilter.transform.position;
					meshCollider.transform.rotation = meshFilter.transform.rotation;
					meshCollider.sharedMesh = meshFilter.sharedMesh;
					value.Add(meshCollider);
				}
			}
			rb._colliders = value.ToArray();
			if (!flag)
			{
				return false;
			}
			Transform[] componentsInChildren2 = rendererGobj.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].gameObject.isStatic = true;
			}
			return true;
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		OnPropFell();
	}

	private void OnPropFell()
	{
		if ((object)_placingProp != null)
		{
			_placingProp.transform.position = base.transform.position;
			_placingProp.transform.rotation = base.transform.rotation;
			_placingProp.SetActive(value: true);
			base.gameObject.SetActive(value: false);
		}
	}

	public void DestroyProp_NoPool()
	{
		if ((object)_placingProp != null)
		{
			Object.Destroy(_placingProp);
			_placingProp = null;
		}
	}
}
