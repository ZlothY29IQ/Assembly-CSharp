using System;
using KID.Model;

[Serializable]
public class GetRequirementsResponse : GetAgeGateRequirementsResponse
{
	public int PlatformMinimumAge { get; set; }
}
