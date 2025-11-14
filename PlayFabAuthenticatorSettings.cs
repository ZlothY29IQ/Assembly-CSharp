using UnityEngine;

public class PlayFabAuthenticatorSettings
{
	public static string TitleId;

	public static string AuthApiBaseUrl;

	public static string DailyQuestsApiBaseUrl;

	public static string FriendApiBaseUrl;

	public static string HpPromoApiBaseUrl;

	public static string IapApiBaseUrl;

	public static string KidApiBaseUrl;

	public static string MmrApiBaseUrl;

	public static string ModerationApiBaseUrl;

	public static string ProgressionApiBaseUrl;

	public static string TitleDataApiBaseUrl;

	public static string VotingApiBaseUrl;

	static PlayFabAuthenticatorSettings()
	{
		Load("PlayFabAuthenticatorSettings");
	}

	public static void Load(string path)
	{
		PlayFabAuthenticatorSettingsScriptableObject playFabAuthenticatorSettingsScriptableObject = Resources.Load<PlayFabAuthenticatorSettingsScriptableObject>(path);
		TitleId = playFabAuthenticatorSettingsScriptableObject.TitleId;
		AuthApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.AuthApiBaseUrl;
		DailyQuestsApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.DailyQuestsApiBaseUrl;
		FriendApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.FriendApiBaseUrl;
		HpPromoApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.HpPromoApiBaseUrl;
		IapApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.IapApiBaseUrl;
		KidApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.KidApiBaseUrl;
		MmrApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.MmrApiBaseUrl;
		ModerationApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.ModerationApiBaseUrl;
		ProgressionApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.ProgressionApiBaseUrl;
		TitleDataApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.TitleDataApiBaseUrl;
		VotingApiBaseUrl = playFabAuthenticatorSettingsScriptableObject.VotingApiBaseUrl;
	}
}
