using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CuratedMapDoorway : MonoBehaviour
{
	[Tooltip("Which slot in the DestinationsCurated TitleData ID list this doorway loads")]
	[SerializeField]
	private CuratedDestinationsManager.CuratedDoorway doorway;

	public UnityEvent<Mod> onCuratedMapResolved;

	private Mod CuratedMod { get; set; }

	public bool HasCuratedMap => CuratedMod != null;

	public void OnEnable()
	{
		CuratedDestinationsManager.OnCuratedMapsUpdated.AddListener(OnCuratedMapsUpdated);
		if (CuratedDestinationsManager.HasRetrievedCuratedMaps)
		{
			OnCuratedMapsUpdated();
		}
		else
		{
			CuratedDestinationsManager.RetrieveCuratedMaps();
		}
	}

	public void OnDisable()
	{
		CuratedDestinationsManager.OnCuratedMapsUpdated.RemoveListener(OnCuratedMapsUpdated);
	}

	private void OnCuratedMapsUpdated()
	{
		CuratedDestinationsManager.TryGetCuratedMod(doorway, out var mod);
		CuratedMod = mod;
		onCuratedMapResolved?.Invoke(mod);
	}
}
