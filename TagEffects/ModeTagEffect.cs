using System;
using System.Collections.Generic;
using GorillaGameModes;
using UnityEngine;

namespace TagEffects;

[Serializable]
public class ModeTagEffect
{
	[SerializeField]
	private GameModeType[] modes;

	private HashSet<GameModeType> modesHash;

	public TagEffectPack tagEffect;

	public bool blockTagOverride;

	public bool blockFistBumpOverride;

	public bool blockHiveFiveOverride;

	public HashSet<GameModeType> Modes
	{
		get
		{
			if (modesHash == null)
			{
				modesHash = new HashSet<GameModeType>(modes);
			}
			return modesHash;
		}
	}
}
