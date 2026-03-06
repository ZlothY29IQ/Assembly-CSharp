using System;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaTag;

[Serializable]
public struct BoneOffset
{
	public GTHardCodedBones.SturdyEBone bone;

	public XformOffset offset;

	public static readonly BoneOffset Identity;

	public Vector3 pos => offset.pos;

	public Quaternion rot => offset.rot;

	public Vector3 scale => offset.scale;

	public BoneOffset(GTHardCodedBones.EBone bone)
	{
		this.bone = bone;
		offset = XformOffset.Identity;
	}

	public BoneOffset(GTHardCodedBones.EBone bone, XformOffset offset)
	{
		this.bone = bone;
		this.offset = offset;
	}

	public BoneOffset(GTHardCodedBones.EBone bone, Vector3 pos, Quaternion rot)
	{
		this.bone = bone;
		offset = new XformOffset(pos, rot);
	}

	public BoneOffset(GTHardCodedBones.EBone bone, Vector3 pos, Vector3 rotAngles)
	{
		this.bone = bone;
		offset = new XformOffset(pos, rotAngles);
	}

	public BoneOffset(GTHardCodedBones.EBone bone, Vector3 pos, Quaternion rot, Vector3 scale)
	{
		this.bone = bone;
		offset = new XformOffset(pos, rot, scale);
	}

	public BoneOffset(GTHardCodedBones.EBone bone, Vector3 pos, Vector3 rotAngles, Vector3 scale)
	{
		this.bone = bone;
		offset = new XformOffset(pos, rotAngles, scale);
	}

	static BoneOffset()
	{
		Identity = new BoneOffset
		{
			bone = GTHardCodedBones.EBone.None,
			offset = XformOffset.Identity
		};
	}
}
