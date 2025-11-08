using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

internal abstract class WarningsServer : MonoBehaviour
{
	public static volatile WarningsServer Instance;

	public abstract Task<PlayerAgeGateWarningStatus?> FetchPlayerData(CancellationToken token);

	public abstract Task<PlayerAgeGateWarningStatus?> GetOptInFollowUpMessage(CancellationToken token);
}
