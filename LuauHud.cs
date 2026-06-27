using System.Collections.Generic;
using System.IO;
using System.Text;
using GorillaGameModes;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class LuauHud : MonoBehaviour
{
	private bool useLuauHud;

	private bool buttonDown;

	private bool showLog;

	private GameObject debugHud;

	private TMP_Text text;

	private StringBuilder builder;

	private float resetTimer;

	private string path = "";

	private string script = "";

	private static LuauHud _instance;

	private List<string> luauLogs = new List<string>();

	public static LuauHud Instance => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		_instance = this;
		path = Path.Combine(Application.persistentDataPath, "script.luau");
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	private void Start()
	{
		useLuauHud = true;
		DebugHudStats instance = DebugHudStats.Instance;
		instance.enabled = false;
		debugHud = instance.gameObject;
		text = instance.text;
		text.gameObject.SetActive(value: false);
		text.gameObject.transform.Rotate(180f * Vector3.up, Space.World);
		builder = new StringBuilder(50);
	}

	private void Update()
	{
		if (!CustomMapLoader.IsDevModeEnabled())
		{
			if (showLog && useLuauHud)
			{
				showLog = false;
				DebugHudStats.Instance?.gameObject.SetActive(value: false);
				text.gameObject.SetActive(value: false);
			}
			return;
		}
		GorillaGameManager instance = GorillaGameManager.instance;
		if ((object)instance == null || instance.GameType() != GameModeType.Custom)
		{
			return;
		}
		bool flag = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
		bool flag2 = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
		if (flag != buttonDown && useLuauHud)
		{
			buttonDown = flag;
			if (!buttonDown)
			{
				if (!text.gameObject.activeInHierarchy)
				{
					DebugHudStats.Instance?.gameObject.SetActive(value: true);
					text.gameObject.SetActive(value: true);
					showLog = true;
				}
				else
				{
					DebugHudStats.Instance?.gameObject.SetActive(value: false);
					text.gameObject.SetActive(value: false);
					showLog = false;
				}
			}
		}
		if (!(flag && flag2))
		{
			resetTimer = Time.time;
		}
		if (Time.time - resetTimer > 2f && CustomGameMode.GameModeInitialized)
		{
			RestartLuauScript();
			resetTimer = Time.time;
		}
		if (useLuauHud && showLog)
		{
			builder.Clear();
			builder.AppendLine();
			for (int i = 0; i < luauLogs.Count; i++)
			{
				builder.AppendLine(luauLogs[i]);
			}
			text.text = builder.ToString();
		}
	}

	public void RestartLuauScript()
	{
		LuauLog("Restarting Luau Script");
		LuauScriptRunner gameScriptRunner = CustomGameMode.gameScriptRunner;
		if (gameScriptRunner != null && gameScriptRunner.ShouldTick)
		{
			CustomGameMode.StopScript();
		}
		script = LoadLocalScript();
		if (script != "")
		{
			LuauLog("Loaded script from: " + path);
			LuauLog("Loaded Script Text: \n" + script);
			CustomGameMode.LuaScript = script;
		}
		CustomGameMode.LuaStart();
	}

	public string LoadLocalScript()
	{
		string result = "";
		if (File.Exists(path))
		{
			result = File.ReadAllText(path);
		}
		return result;
	}

	public void LuauLog(string log)
	{
		Debug.Log(log);
		luauLogs.Add(log);
		if (luauLogs.Count > 6)
		{
			luauLogs.RemoveAt(0);
		}
	}
}
