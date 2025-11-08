using System;
using System.Diagnostics;
using UnityEngine;

namespace GorillaTagScripts;

[CreateAssetMenu(fileName = "CrystalVisualsPreset", menuName = "ScriptableObjects/CrystalVisualsPreset", order = 0)]
public class CrystalVisualsPreset : ScriptableObject
{
	[Serializable]
	public struct VisualState
	{
		[ColorUsage(false, false)]
		public Color albedo;

		[ColorUsage(false, false)]
		public Color emission;

		public override int GetHashCode()
		{
			int item = GetColorHash(albedo);
			int item2 = GetColorHash(emission);
			return (item, item2).GetHashCode();
			static int GetColorHash(Color c)
			{
				return (c.r, c.g, c.b).GetHashCode();
			}
		}
	}

	public VisualState stateA;

	public VisualState stateB;

	public override int GetHashCode()
	{
		return (stateA, stateB).GetHashCode();
	}

	[Conditional("UNITY_EDITOR")]
	private void Save()
	{
	}
}
