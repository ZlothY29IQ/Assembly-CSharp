namespace GorillaNetworking;

public interface ICosmeticRequestCallback
{
	void OnCosmeticLoaded(string itemName, CosmeticItemInstance instance);
}
