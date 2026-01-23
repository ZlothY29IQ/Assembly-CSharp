public interface IEnergyGadget
{
	bool UsesEnergy { get; }

	bool IsFull { get; }

	void UpdateRecharge(float dt);
}
