internal class PropHuntPools_Callbacks
{
	private const string preLog = "PropHuntPools_Callbacks: ";

	private const string preLogEd = "(editor only log) PropHuntPools_Callbacks: ";

	private const string preLogBeta = "(beta only log) PropHuntPools_Callbacks: ";

	private const string preErr = "ERROR!!!  PropHuntPools_Callbacks: ";

	private const string preErrEd = "ERROR!!!  (editor only log) PropHuntPools_Callbacks: ";

	private const string preErrBeta = "ERROR!!!  (beta only log) PropHuntPools_Callbacks: ";

	internal static readonly PropHuntPools_Callbacks instance;

	private static bool _isListeningForZoneChanged;

	static PropHuntPools_Callbacks()
	{
		instance = new PropHuntPools_Callbacks();
	}

	internal void ListenForZoneChanged()
	{
		if (!_isListeningForZoneChanged)
		{
			ZoneManagement.OnZoneChange += _OnZoneChanged;
		}
	}

	private void _OnZoneChanged(ZoneData[] zoneDatas)
	{
		if (!(VRRigCache.Instance == null) && !(VRRigCache.Instance.localRig == null) && !(VRRigCache.Instance.localRig.Rig == null) && VRRigCache.Instance.localRig.Rig.zoneEntity.currentZone == GTZone.bayou)
		{
			_isListeningForZoneChanged = false;
			ZoneManagement.OnZoneChange -= _OnZoneChanged;
			PropHuntPools.OnLocalPlayerEnteredBayou();
		}
	}
}
