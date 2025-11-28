public interface SIGadgetBlasterType
{
	void OnUpdateAuthority(float dt);

	void OnUpdateRemote(float dt);

	void SetStateShared();

	void NetworkFireProjectile(object[] data);

	void ApplyUpgradeNodes(SIUpgradeSet withUpgrades);
}
