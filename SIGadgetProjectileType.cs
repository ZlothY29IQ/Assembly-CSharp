public interface SIGadgetProjectileType
{
	void LocalProjectileHit(SIPlayer player = null);

	void NetworkedProjectileHit(object[] data);
}
