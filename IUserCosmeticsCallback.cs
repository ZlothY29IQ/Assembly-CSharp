internal interface IUserCosmeticsCallback
{
	bool PendingUpdate { get; set; }

	bool OnGetUserCosmetics(string cosmetics);
}
