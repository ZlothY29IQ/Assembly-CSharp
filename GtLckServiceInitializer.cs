using Liv.Lck;
using Liv.Lck.Core.Cosmetics;
using Liv.Lck.Cosmetics;
using Liv.Lck.DependencyInjection;
using UnityEngine;

[DefaultExecutionOrder(-950)]
public class GtLckServiceInitializer : MonoBehaviour
{
	[Header("LCK Configuration")]
	[Tooltip("Assign the LCK Quality Config ScriptableObject here.")]
	[SerializeField]
	private LckQualityConfig _qualityConfig;

	private void Awake()
	{
		LckDiContainer instance = LckDiContainer.Instance;
		if (instance.HasService<ILckService>())
		{
			Debug.LogWarning("LCK: Service already configured. Skipping custom GT initialisation.");
			return;
		}
		Debug.Log("LCK: Initializing with GT-SPECIFIC overrides.");
		LckServiceInitializer.ConfigureServices(instance, _qualityConfig, delegate(LckDiContainer container)
		{
			container.AddSingleton<ILckCosmeticsFeatureFlagManager, LckCosmeticsFeatureFlagManagerPlayFab>();
			container.AddSingleton<ILckCosmeticsCoordinator, LckCoreCosmeticsCoordinator>();
			container.AddSingleton<ILckCosmeticsManager, LckCosmeticsManager>();
		});
	}
}
