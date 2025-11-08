using UnityEngine;

namespace GorillaTag.MonkeFX;

[CreateAssetMenu(fileName = "MeshGenerator", menuName = "ScriptableObjects/MeshGenerator", order = 1)]
public class MonkeFXSettingsSO : ScriptableObject
{
	public GTDirectAssetRef<Mesh>[] sourceMeshes;

	[HideInInspector]
	public Mesh combinedMesh;

	protected void Awake()
	{
		MonkeFX.Register(this);
	}
}
