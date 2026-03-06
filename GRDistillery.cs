using System;
using System.Globalization;
using GorillaNetworking;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GRDistillery : MonoBehaviour
{
	[SerializeField]
	private GRCurrencyDepositor sentientCoreDeposit;

	[SerializeField]
	private ApplyMaterialProperty _applyMaterialgauge1;

	[SerializeField]
	private ApplyMaterialProperty _applyMaterialgauge2;

	[SerializeField]
	private ApplyMaterialProperty _applyMaterialgauge3;

	[SerializeField]
	private ApplyMaterialProperty _applyMaterialgauge4;

	[SerializeField]
	private ApplyMaterialProperty _applyMaterialCurrentResearch;

	[FormerlySerializedAs("emptyFillAmount")]
	public float gaugeEmptyFillAmount = 0.44f;

	[FormerlySerializedAs("fullFillAmount")]
	public float gaugeFullFillAmount = 0.56f;

	[SerializeField]
	private Transform depositClosePosition;

	[SerializeField]
	private Transform depositOpenPosition;

	[SerializeField]
	private GameObject depositDoor;

	[SerializeField]
	private float depositDoorCloseSpeed = 0.5f;

	[SerializeField]
	private TextMeshPro currentResearchPoints;

	public float researchGaugeEmptyFillAmount = 0.44f;

	public float researchGaugeFullFillAmount = 0.56f;

	public int secondsToResearchACore;

	public float gaugeDrainTime = 2f;

	public int maxCores = 4;

	public AudioSource feedbackSound;

	private DateTime startTime;

	private bool bProcessing;

	private int cores;

	private bool bFillingGauge;

	private int currentGaugeCore;

	private float currentGaugeFillAmount;

	private double remaingTime;

	private float fillTime;

	private float[] gaugesFill = new float[4];

	private float researchGaugeFill;

	private bool firstUpdate;

	[NonSerialized]
	public GhostReactor reactor;

	private const string grDistilleryCorePrefsKey = "_grDistilleryCore";

	private const string grDistilleryStartTimePrefsKey = "_grDistilleryStartTime";

	public void Init(GhostReactor reactor)
	{
		this.reactor = reactor;
		sentientCoreDeposit.Init(reactor);
		cores = PlayerPrefs.GetInt("_grDistilleryCore", -1);
		if (cores == -1)
		{
			cores = 0;
		}
		RestoreStartTime();
		InitializeGauges();
	}

	private void SaveStartTime(DateTime time)
	{
		string value = time.ToString("O");
		PlayerPrefs.SetString("_grDistilleryStartTime", value);
		PlayerPrefs.Save();
	}

	private void RestoreStartTime()
	{
		string text = PlayerPrefs.GetString("_grDistilleryStartTime", string.Empty);
		if (text != string.Empty)
		{
			startTime = DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		}
	}

	public void StartResearch()
	{
		if (cores > 0)
		{
			startTime = GorillaComputer.instance.GetServerTime();
			SaveStartTime(startTime);
			bProcessing = true;
			InitializeGauges();
		}
	}

	public double CalculateRemaining()
	{
		return (double)secondsToResearchACore - (GorillaComputer.instance.GetServerTime() - startTime).TotalSeconds;
	}

	private void FirstUpdate()
	{
		double num = CalculateRemaining();
		while (cores > 0 && num < (double)(-secondsToResearchACore))
		{
			if (num < (double)(-secondsToResearchACore))
			{
				CompleteResearchingCore();
				num += (double)secondsToResearchACore;
			}
		}
		if (cores > 0 && num < 0.0)
		{
			startTime = GorillaComputer.instance.GetServerTime().AddSeconds(num);
			num = CalculateRemaining();
			SaveStartTime(startTime);
		}
		if (cores > 0)
		{
			bProcessing = true;
			currentGaugeCore = cores - 1;
		}
		else
		{
			currentGaugeCore = 0;
		}
		if (cores >= 4)
		{
			depositDoor.transform.position = depositClosePosition.position;
		}
		else
		{
			depositDoor.transform.position = depositOpenPosition.position;
		}
		UpdateGauges();
	}

	public void Update()
	{
		if (!firstUpdate)
		{
			FirstUpdate();
			firstUpdate = true;
		}
		UpdateDoorPosition();
		UpdateGauges();
		if (bProcessing)
		{
			remaingTime = CalculateRemaining();
			if (remaingTime <= 0.0)
			{
				CompleteResearchingCore();
			}
		}
	}

	private void UpdateDoorPosition()
	{
		if (cores >= 4)
		{
			depositDoor.transform.position = Vector3.MoveTowards(depositDoor.transform.position, depositClosePosition.transform.position, depositDoorCloseSpeed * Time.deltaTime);
		}
		else
		{
			depositDoor.transform.position = Vector3.MoveTowards(depositDoor.transform.position, depositOpenPosition.transform.position, depositDoorCloseSpeed * Time.deltaTime);
		}
	}

	private void CompleteResearchingCore()
	{
		cores = Math.Max(cores - 1, 0);
		currentGaugeCore = Math.Max(cores - 1, 0);
		PlayerPrefs.SetInt("_grDistilleryCore", cores);
		PlayerPrefs.Save();
		if (cores > 0)
		{
			startTime = GorillaComputer.instance.GetServerTime().AddSeconds(remaingTime);
			SaveStartTime(startTime);
			remaingTime = CalculateRemaining();
		}
		if (cores == 0)
		{
			bProcessing = false;
		}
		UpdateGauges();
	}

	public void DepositCore()
	{
		if (cores < maxCores)
		{
			cores++;
			if (!bFillingGauge)
			{
				bFillingGauge = true;
				fillTime = 0f;
			}
			PlayerPrefs.SetInt("_grDistilleryCore", cores);
			PlayerPrefs.Save();
			if (cores == 1)
			{
				StartResearch();
			}
		}
	}

	public void DebugFinishDistill()
	{
	}

	private void OnEnable()
	{
		if ((bool)_applyMaterialgauge1)
		{
			_applyMaterialgauge1.mode = ApplyMaterialProperty.ApplyMode.MaterialPropertyBlock;
		}
		if ((bool)_applyMaterialgauge2)
		{
			_applyMaterialgauge2.mode = ApplyMaterialProperty.ApplyMode.MaterialPropertyBlock;
		}
		if ((bool)_applyMaterialgauge3)
		{
			_applyMaterialgauge3.mode = ApplyMaterialProperty.ApplyMode.MaterialPropertyBlock;
		}
		if ((bool)_applyMaterialgauge4)
		{
			_applyMaterialgauge4.mode = ApplyMaterialProperty.ApplyMode.MaterialPropertyBlock;
		}
		InitializeGauges();
	}

	private void InitializeGauges()
	{
		for (int i = 0; i < gaugesFill.Length - 1; i++)
		{
			gaugesFill[i] = ((cores >= i + 1) ? gaugeFullFillAmount : gaugeEmptyFillAmount);
		}
		researchGaugeFill = gaugesFill[0];
		currentGaugeFillAmount = gaugeEmptyFillAmount;
	}

	private void UpdateGauges()
	{
		for (int i = 0; i < gaugesFill.Length; i++)
		{
			if (i + 1 > cores)
			{
				gaugesFill[i] = gaugeEmptyFillAmount;
			}
		}
		if (bFillingGauge)
		{
			fillTime += Time.deltaTime;
			float num = fillTime / gaugeDrainTime;
			if (currentGaugeCore == cores - 1)
			{
				if (num > 1f)
				{
					bFillingGauge = false;
				}
				else
				{
					gaugesFill[currentGaugeCore] = Mathf.Lerp(currentGaugeFillAmount, Mathf.Lerp(gaugeEmptyFillAmount, gaugeFullFillAmount, (float)remaingTime / (float)secondsToResearchACore), num);
				}
			}
			else
			{
				gaugesFill[currentGaugeCore] = Mathf.Lerp(currentGaugeFillAmount, gaugeFullFillAmount, num);
			}
			if (bFillingGauge && num > 1f)
			{
				currentGaugeCore++;
				currentGaugeFillAmount = gaugeEmptyFillAmount;
				fillTime = 0f;
			}
		}
		else if (bProcessing)
		{
			gaugesFill[currentGaugeCore] = Mathf.Lerp(gaugeEmptyFillAmount, gaugeFullFillAmount, (float)remaingTime / (float)secondsToResearchACore);
			currentGaugeFillAmount = gaugesFill[currentGaugeCore];
		}
		_applyMaterialgauge1.SetFloat("_LiquidFill", gaugesFill[0]);
		_applyMaterialgauge1.Apply();
		_applyMaterialgauge2.SetFloat("_LiquidFill", gaugesFill[1]);
		_applyMaterialgauge2.Apply();
		_applyMaterialgauge3.SetFloat("_LiquidFill", gaugesFill[2]);
		_applyMaterialgauge3.Apply();
		_applyMaterialgauge4.SetFloat("_LiquidFill", gaugesFill[3]);
		_applyMaterialgauge4.Apply();
		_applyMaterialCurrentResearch.SetFloat("_LiquidFill", researchGaugeFill);
		_applyMaterialCurrentResearch.Apply();
	}
}
