public static class KIDFeaturesExtensions
{
	public static string ToStandardisedString(this EKIDFeatures feature)
	{
		return feature switch
		{
			EKIDFeatures.Custom_Nametags => "custom-username", 
			EKIDFeatures.Voice_Chat => "voice-chat", 
			EKIDFeatures.Multiplayer => "multiplayer", 
			EKIDFeatures.Mods => "mods", 
			EKIDFeatures.Groups => "join-groups", 
			_ => feature.ToString(), 
		};
	}

	public static EKIDFeatures? FromString(string name)
	{
		return name.ToLower() switch
		{
			"voice-chat" => EKIDFeatures.Voice_Chat, 
			"custom-username" => EKIDFeatures.Custom_Nametags, 
			"multiplayer" => EKIDFeatures.Multiplayer, 
			"mods" => EKIDFeatures.Mods, 
			"join-groups" => EKIDFeatures.Groups, 
			_ => null, 
		};
	}

	public static bool TryGetFromString(string name, out EKIDFeatures result)
	{
		EKIDFeatures? eKIDFeatures = FromString(name);
		if (eKIDFeatures.HasValue)
		{
			result = eKIDFeatures.Value;
			return true;
		}
		result = EKIDFeatures.Voice_Chat;
		return false;
	}
}
