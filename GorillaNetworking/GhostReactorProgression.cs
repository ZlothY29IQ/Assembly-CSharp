using UnityEngine;

namespace GorillaNetworking;

public class GhostReactorProgression : MonoBehaviour
{
	public static GhostReactorProgression instance;

	private string progressionTrackId = "a0208736-e696-489b-81cd-c0c772489cc5";

	private GRPlayer _grPlayer;

	private GhostReactor _reactor;

	public static GRProgressionScriptableObject grPSO;

	public const string grPSODirectory = "ProgressionTiersData";

	public void Awake()
	{
		instance = this;
	}

	public void Start()
	{
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.OnTrackRead += OnTrackRead;
			ProgressionManager.Instance.OnTrackSet += OnTrackSet;
			ProgressionManager.Instance.OnNodeUnlocked += delegate
			{
				OnNodeUnlocked();
			};
		}
		else
		{
			Debug.Log("GRP: ProgressionManager is null!");
		}
	}

	public async void GetStartingProgression(GRPlayer grPlayer)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		_grPlayer = grPlayer;
		ProgressionManager.Instance.GetProgression(progressionTrackId);
		if (_grPlayer.gamePlayer.IsLocal())
		{
			_grPlayer.mothershipId = MothershipClientContext.MothershipId;
			ProgressionManager.Instance.GetShiftCredit(_grPlayer.mothershipId);
		}
	}

	public void SetProgression(int progressionAmountToAdd, GRPlayer grPlayer)
	{
		_grPlayer = grPlayer;
		ProgressionManager.Instance.SetProgression(progressionTrackId, progressionAmountToAdd);
	}

	public void UnlockProgressionTreeNode(string treeId, string nodeId, GhostReactor reactor)
	{
		_reactor = reactor;
		ProgressionManager.Instance.UnlockNode(treeId, nodeId);
	}

	private void OnTrackRead(string trackId, int progress)
	{
		if (_grPlayer == null)
		{
			Debug.Log("GRP: OnTrackRead Failure: player is null");
		}
		else if (trackId != progressionTrackId)
		{
			Debug.Log($"GRP: OnTrackRead Failure: track [{trackId}] progressionTrack [{progressionTrackId}] progress {progress}");
		}
		else
		{
			_grPlayer.SetProgressionData(progress, progress);
		}
	}

	private void OnTrackSet(string trackId, int progress)
	{
		if (!(_grPlayer == null) && !(trackId != progressionTrackId))
		{
			_grPlayer.SetProgressionData(progress, _grPlayer.CurrentProgression.redeemedPoints);
		}
	}

	private void OnNodeUnlocked()
	{
		if (_reactor != null && _reactor.toolProgression != null)
		{
			_reactor.UpdateLocalPlayerFromProgression();
		}
	}

	public static (int tier, int grade, int totalPointsToNextLevel, int partialPointsToNextLevel) GetGradePointDetails(int points)
	{
		LoadGRPSO();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (num3 = 0; num3 < grPSO.progressionData.Count; num3++)
		{
			num2 = num;
			num += grPSO.progressionData[num3].grades * grPSO.progressionData[num3].pointsPerGrade;
			if (points < num)
			{
				break;
			}
		}
		if (points > num)
		{
			return (tier: num3 - 1, grade: 0, totalPointsToNextLevel: 0, partialPointsToNextLevel: 0);
		}
		int pointsPerGrade = grPSO.progressionData[num3].pointsPerGrade;
		int item = (points - num2) / pointsPerGrade;
		int item2 = (points - num2) % pointsPerGrade;
		return (tier: num3, grade: item, totalPointsToNextLevel: pointsPerGrade, partialPointsToNextLevel: item2);
	}

	public static string GetTitleNameAndGrade(int points)
	{
		LoadGRPSO();
		int num = 0;
		for (int i = 0; i < grPSO.progressionData.Count; i++)
		{
			num += grPSO.progressionData[i].grades * grPSO.progressionData[i].pointsPerGrade;
			if (points < num)
			{
				return grPSO.progressionData[i].tierName + " " + (grPSO.progressionData[i].grades - Mathf.FloorToInt((num - points) / grPSO.progressionData[i].pointsPerGrade) + 1);
			}
		}
		return "null";
	}

	public static string GetTitleName(int points)
	{
		LoadGRPSO();
		int num = 0;
		for (int i = 0; i < grPSO.progressionData.Count; i++)
		{
			num += grPSO.progressionData[i].grades * grPSO.progressionData[i].pointsPerGrade;
			if (points < num)
			{
				return grPSO.progressionData[i].tierName;
			}
		}
		return "null";
	}

	public static string GetTitleNameFromLevel(int level)
	{
		LoadGRPSO();
		for (int i = 0; i < grPSO.progressionData.Count; i++)
		{
			if (grPSO.progressionData[i].tierId >= level)
			{
				return grPSO.progressionData[i].tierName;
			}
		}
		return "null";
	}

	public static int GetGrade(int points)
	{
		LoadGRPSO();
		int num = 0;
		for (int i = 0; i < grPSO.progressionData.Count; i++)
		{
			num += grPSO.progressionData[i].grades * grPSO.progressionData[i].pointsPerGrade;
			if (points < num)
			{
				return grPSO.progressionData[i].grades - Mathf.FloorToInt((num - points) / grPSO.progressionData[i].pointsPerGrade) + 1;
			}
		}
		return -1;
	}

	public static int GetTitleLevel(int points)
	{
		LoadGRPSO();
		int num = 0;
		for (int i = 0; i < grPSO.progressionData.Count; i++)
		{
			num += grPSO.progressionData[i].grades * grPSO.progressionData[i].pointsPerGrade;
			if (points < num)
			{
				return grPSO.progressionData[i].tierId;
			}
		}
		return -1;
	}

	public static void LoadGRPSO()
	{
		if (grPSO == null)
		{
			grPSO = Resources.Load<GRProgressionScriptableObject>("ProgressionTiersData");
		}
	}
}
