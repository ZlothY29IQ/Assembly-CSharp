using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

[CreateAssetMenu(fileName = "TechTreeGadgetGraph", menuName = "SuperInfection/TechTree Gadget Graph")]
public class TechTreeGadgetGraph : NodeGraph
{
	public string nickName;

	public SITechTreePageId pageId;

	public Sprite icon;

	public float costMultiplier = 1f;

	public ESuperGameModes excludedGameModes;

	public EAssetReleaseTier releaseTier;

	private const float XLayoutStep = 300f;

	private const float YLayoutStep = 250f;

	public GadgetNode[] GadgetNodes => nodes.Select((Node n) => n as GadgetNode).ToArray();

	public bool IsValid
	{
		get
		{
			EAssetReleaseTier eAssetReleaseTier = releaseTier;
			if (eAssetReleaseTier != EAssetReleaseTier.Disabled && eAssetReleaseTier <= EAssetReleaseTier.PublicRC)
			{
				List<Node> list = nodes;
				if (list == null)
				{
					return false;
				}
				return list.Count > 0;
			}
			return false;
		}
	}
}
