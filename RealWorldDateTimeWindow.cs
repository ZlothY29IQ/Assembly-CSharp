using System;
using UniLabs.Time;
using UnityEngine;

public class RealWorldDateTimeWindow : ScriptableObject
{
	[SerializeField]
	private UDateTime startTime;

	[SerializeField]
	private UDateTime endTime;

	public bool MatchesDate(DateTime utcDate)
	{
		if (startTime <= utcDate)
		{
			return endTime >= utcDate;
		}
		return false;
	}
}
