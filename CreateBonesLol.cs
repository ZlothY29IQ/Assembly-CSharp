using UnityEngine;

public class CreateBonesLol : MonoBehaviour
{
	public GameObject cube;

	public OVRSkeleton skeleton;

	private void Update()
	{
		if (skeleton.Bones.Count <= 0)
		{
			return;
		}
		foreach (OVRBone bone in skeleton.Bones)
		{
			GameObject obj = Object.Instantiate(cube);
			obj.transform.parent = bone.Transform;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;
		}
		base.enabled = false;
	}
}
