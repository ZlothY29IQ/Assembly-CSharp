using System.Linq;
using UnityEngine;
using XNode;

[CreateAssetMenu(fileName = "TechTreePage", menuName = "SuperInfection/TechTree Page")]
public class TechTreeGadgetGraph : NodeGraph
{
	public string nickName;

	public SITechTreePageId pageId;

	public float costMultiplier = 1f;

	public ESuperGameModes excludedGameModes;

	public EAssetReleaseTier releaseTier;

	private const float XLayoutStep = 300f;

	private const float YLayoutStep = 250f;

	public GadgetNode[] GadgetNodes => nodes.Select((Node n) => n as GadgetNode).ToArray();
}
