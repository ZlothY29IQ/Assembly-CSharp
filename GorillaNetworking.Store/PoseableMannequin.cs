using System.Collections;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaNetworking.Store;

public class PoseableMannequin : MonoBehaviour
{
	public SkinnedMeshRenderer skinnedMeshRenderer;

	[FormerlySerializedAs("meshCollider")]
	public MeshCollider skinnedMeshCollider;

	public GTPosRotConstraints[] cosmeticConstraints;

	public Mesh BakedColliderMesh;

	[SerializeField]
	[FormerlySerializedAs("liveAssetPath")]
	protected string prefabAssetPath;

	[SerializeField]
	protected string prefabFolderPath;

	[SerializeField]
	protected string prefabAssetName;

	public MeshFilter staticGorillaMesh;

	public MeshCollider staticGorillaMeshCollider;

	public MeshRenderer staticGorillaMeshRenderer;

	public void Start()
	{
		if ((bool)skinnedMeshRenderer)
		{
			skinnedMeshRenderer.gameObject.SetActive(value: false);
		}
		if ((bool)staticGorillaMesh)
		{
			staticGorillaMesh.gameObject.SetActive(value: true);
		}
	}

	private string GetPrefabPathFromCurrentPrefabStage()
	{
		return "";
	}

	private string GetMeshPathFromPrefabPath(string prefabPath)
	{
		return "";
	}

	public void BakeSkinnedMesh()
	{
		BakeAndSaveMeshInPath(GetMeshPathFromPrefabPath(GetPrefabPathFromCurrentPrefabStage()));
	}

	public void BakeAndSaveMeshInPath(string meshPath)
	{
	}

	private void UpdateStaticMeshMannequin()
	{
		staticGorillaMesh.sharedMesh = BakedColliderMesh;
		staticGorillaMeshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;
		staticGorillaMeshCollider.sharedMesh = BakedColliderMesh;
	}

	private void UpdateSkinnedMeshCollider()
	{
		skinnedMeshCollider.sharedMesh = BakedColliderMesh;
	}

	public void UpdateGTPosRotConstraints()
	{
		GTPosRotConstraints[] array = cosmeticConstraints;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].constraints.ForEach(delegate(GorillaPosRotConstraint c)
			{
				c.follower.rotation = c.source.rotation;
				c.follower.position = c.source.position;
			});
		}
	}

	private void HookupCosmeticConstraints()
	{
		cosmeticConstraints = GetComponentsInChildren<GTPosRotConstraints>();
		GTPosRotConstraints[] array = cosmeticConstraints;
		foreach (GTPosRotConstraints gTPosRotConstraints in array)
		{
			for (int j = 0; j < gTPosRotConstraints.constraints.Length; j++)
			{
				gTPosRotConstraints.constraints[j].source = FindBone(gTPosRotConstraints.constraints[j].follower.name);
			}
		}
	}

	private Transform FindBone(string boneName)
	{
		Transform[] bones = skinnedMeshRenderer.bones;
		foreach (Transform transform in bones)
		{
			if (transform.name == boneName)
			{
				return transform;
			}
		}
		return null;
	}

	public void CreasteTestClip()
	{
	}

	public void SerializeVRRig()
	{
		StartCoroutine(SaveLocalPlayerPose());
	}

	public IEnumerator SaveLocalPlayerPose()
	{
		yield return null;
	}

	public void SerializeOutBonesFromSkinnedMesh(SkinnedMeshRenderer paramSkinnedMeshRenderer)
	{
	}

	public void SetCurvesForBone(SkinnedMeshRenderer paramSkinnedMeshRenderer, AnimationClip clip, Transform bone)
	{
		Keyframe[] keys = new Keyframe[1]
		{
			new Keyframe(0f, bone.parent.localRotation.x)
		};
		Keyframe[] keys2 = new Keyframe[1]
		{
			new Keyframe(0f, bone.parent.localRotation.y)
		};
		Keyframe[] keys3 = new Keyframe[1]
		{
			new Keyframe(0f, bone.parent.localRotation.z)
		};
		Keyframe[] keys4 = new Keyframe[1]
		{
			new Keyframe(0f, bone.parent.localRotation.w)
		};
		AnimationCurve curve = new AnimationCurve(keys);
		AnimationCurve curve2 = new AnimationCurve(keys2);
		AnimationCurve curve3 = new AnimationCurve(keys3);
		AnimationCurve curve4 = new AnimationCurve(keys4);
		string relativePath = "";
		string text = bone.name.Replace("_new", "");
		Transform[] bones = skinnedMeshRenderer.bones;
		foreach (Transform transform in bones)
		{
			if (transform.name == text)
			{
				relativePath = transform.GetPath(skinnedMeshRenderer.transform.parent).TrimStart('/');
				break;
			}
		}
		clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", curve);
		clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", curve2);
		clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", curve3);
		clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", curve4);
	}

	public void UpdatePrefabWithAnimationClip(string AnimationFileName)
	{
	}

	public void LoadPoseOntoMannequin(AnimationClip clip, float frameTime = 0f)
	{
	}

	public void OnValidate()
	{
	}
}
