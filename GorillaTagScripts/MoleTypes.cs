using UnityEngine;

namespace GorillaTagScripts;

public class MoleTypes : MonoBehaviour
{
	public bool isHazard;

	public int scorePoint = 1;

	public MeshRenderer MeshRenderer;

	public Material monkeMoleDefaultMaterial;

	public Material monkeMoleHitMaterial;

	public bool IsLeftSideMoleType { get; set; }

	public Mole MoleContainerParent { get; set; }

	private void Start()
	{
		MoleContainerParent = GetComponentInParent<Mole>();
		if ((bool)MoleContainerParent)
		{
			IsLeftSideMoleType = MoleContainerParent.IsLeftSideMole;
		}
	}
}
