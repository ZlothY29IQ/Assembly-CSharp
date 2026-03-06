public static class EAssetReleaseTier_Extensions
{
	public static bool ShouldIncludeInBuild(this EAssetReleaseTier assetTier, EBuildReleaseTier buildTier)
	{
		if (assetTier != EAssetReleaseTier.Disabled)
		{
			return (int)assetTier <= (int)buildTier;
		}
		return false;
	}
}
