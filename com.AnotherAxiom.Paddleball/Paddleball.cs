using System;
using GorillaExtensions;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace com.AnotherAxiom.Paddleball;

public class Paddleball : ArcadeGame
{
	private enum ScreenMode
	{
		Title,
		Gameplay,
		WhiteWin,
		BlackWin
	}

	[Serializable]
	private struct PaddleballNetState : IEquatable<PaddleballNetState>
	{
		public byte P0LocY;

		public byte P1LocY;

		public byte P2LocY;

		public byte P3LocY;

		public float BallLocX;

		public byte BallLocY;

		public byte BallTrajectoryX;

		public byte BallTrajectoryY;

		public float BallSpeed;

		public int ScoreLeft;

		public int ScoreRight;

		public int ScreenMode;

		public bool Equals(PaddleballNetState other)
		{
			if (P0LocY != other.P0LocY)
			{
				return false;
			}
			if (P1LocY != other.P1LocY)
			{
				return false;
			}
			if (P2LocY != other.P2LocY)
			{
				return false;
			}
			if (P3LocY != other.P3LocY)
			{
				return false;
			}
			if (!BallLocX.Approx(other.BallLocX))
			{
				return false;
			}
			if (BallLocY != other.BallLocY)
			{
				return false;
			}
			if (BallTrajectoryX != other.BallTrajectoryX)
			{
				return false;
			}
			if (BallTrajectoryY != other.BallTrajectoryY)
			{
				return false;
			}
			if (!BallSpeed.Approx(other.BallSpeed))
			{
				return false;
			}
			if (ScoreLeft != other.ScoreLeft)
			{
				return false;
			}
			if (ScoreRight != other.ScoreRight)
			{
				return false;
			}
			if (ScreenMode != other.ScreenMode)
			{
				return false;
			}
			return true;
		}
	}

	[SerializeField]
	private PaddleballPaddle[] p;

	private float[] requestedPos = new float[4];

	private float[] officialPos = new float[4];

	[SerializeField]
	private Transform ball;

	[SerializeField]
	private Vector2 ballTrajectory;

	[SerializeField]
	private float paddleSpeed = 1f;

	[SerializeField]
	private float initialBallSpeed = 1f;

	[SerializeField]
	private float ballSpeedBoost = 0.02f;

	private float gameBallSpeed = 1f;

	[SerializeField]
	private Vector2 tableSizeBall;

	[SerializeField]
	private Vector2 tableSizePaddle;

	[SerializeField]
	private GameObject blackWinScreen;

	[SerializeField]
	private GameObject whiteWinScreen;

	[SerializeField]
	private GameObject titleScreen;

	[SerializeField]
	private float winScreenDuration;

	private float returnToTitleAfterTimestamp;

	private int scoreL;

	private int scoreR;

	private string scoreFormat;

	[SerializeField]
	private TMP_Text scoreDisplay;

	private float[] paddleIdle;

	private ScreenMode currentScreenMode;

	private const int AUDIO_WALLBOUNCE = 0;

	private const int AUDIO_PADDLEBOUNCE = 1;

	private const int AUDIO_SCORE = 2;

	private const int AUDIO_WIN = 3;

	private const int AUDIO_PLAYERJOIN = 4;

	private const int VAR_REQUESTEDPOS = 0;

	private const int MAXSCORE = 10;

	private float yPosToByteFactor;

	private float byteToYPosFactor;

	private const float directionToByteFactor = 127.5f;

	private const float byteToDirectionFactor = 0.007843138f;

	private PaddleballNetState netStateLast;

	private PaddleballNetState netStateCur;

	protected override void Awake()
	{
		base.Awake();
		yPosToByteFactor = 255f / (2f * tableSizeBall.y);
		byteToYPosFactor = 1f / yPosToByteFactor;
	}

	private void Start()
	{
		whiteWinScreen.SetActive(value: false);
		blackWinScreen.SetActive(value: false);
		titleScreen.SetActive(value: true);
		ball.gameObject.SetActive(value: false);
		currentScreenMode = ScreenMode.Title;
		paddleIdle = new float[p.Length];
		for (int i = 0; i < p.Length; i++)
		{
			p[i].gameObject.SetActive(value: false);
			paddleIdle[i] = 30f;
		}
		gameBallSpeed = initialBallSpeed;
		scoreR = (scoreL = 0);
		scoreFormat = scoreDisplay.text;
		UpdateScore();
	}

	private void Update()
	{
		if (currentScreenMode == ScreenMode.Gameplay)
		{
			ball.Translate(ballTrajectory.normalized * Time.deltaTime * gameBallSpeed);
			if (ball.localPosition.y > tableSizeBall.y)
			{
				ball.localPosition = new Vector3(ball.localPosition.x, tableSizeBall.y, ball.localPosition.z);
				ballTrajectory.y = 0f - ballTrajectory.y;
				PlaySound(0);
			}
			if (ball.localPosition.y < 0f - tableSizeBall.y)
			{
				ball.localPosition = new Vector3(ball.localPosition.x, 0f - tableSizeBall.y, ball.localPosition.z);
				ballTrajectory.y = 0f - ballTrajectory.y;
				PlaySound(0);
			}
			if (ball.localPosition.x > tableSizeBall.x)
			{
				ball.localPosition = new Vector3(tableSizeBall.x, ball.localPosition.y, ball.localPosition.z);
				ballTrajectory.x = 0f - ballTrajectory.x;
				gameBallSpeed = initialBallSpeed;
				scoreL++;
				UpdateScore();
				PlaySound(2);
				if (scoreL >= 10)
				{
					ChangeScreen(ScreenMode.WhiteWin);
				}
			}
			if (ball.localPosition.x < 0f - tableSizeBall.x)
			{
				ball.localPosition = new Vector3(0f - tableSizeBall.x, ball.localPosition.y, ball.localPosition.z);
				ballTrajectory.x = 0f - ballTrajectory.x;
				gameBallSpeed = initialBallSpeed;
				scoreR++;
				UpdateScore();
				PlaySound(2);
				if (scoreR >= 10)
				{
					ChangeScreen(ScreenMode.BlackWin);
				}
			}
		}
		if (returnToTitleAfterTimestamp != 0f && Time.time > returnToTitleAfterTimestamp)
		{
			ChangeScreen(ScreenMode.Title);
		}
		for (int i = 0; i < p.Length; i++)
		{
			if (IsPlayerLocallyControlled(i))
			{
				_ = requestedPos[i];
				if (getButtonState(i, ArcadeButtons.UP))
				{
					requestedPos[i] += Time.deltaTime * paddleSpeed;
				}
				else if (getButtonState(i, ArcadeButtons.DOWN))
				{
					requestedPos[i] -= Time.deltaTime * paddleSpeed;
				}
				requestedPos[i] = Mathf.Clamp(requestedPos[i], 0f - tableSizePaddle.y, tableSizePaddle.y);
			}
			float value = ((NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.IsMasterClient) ? Mathf.MoveTowards(p[i].transform.localPosition.y, officialPos[i], Time.deltaTime * paddleSpeed) : Mathf.MoveTowards(p[i].transform.localPosition.y, requestedPos[i], Time.deltaTime * paddleSpeed));
			p[i].transform.localPosition = p[i].transform.localPosition.WithY(Mathf.Clamp(value, 0f - tableSizePaddle.y, tableSizePaddle.y));
			if (getButtonState(i, ArcadeButtons.GRAB))
			{
				paddleIdle[i] = 0f;
				switch (currentScreenMode)
				{
				case ScreenMode.Title:
					ChangeScreen(ScreenMode.Gameplay);
					break;
				case ScreenMode.Gameplay:
					returnToTitleAfterTimestamp = Time.time + 30f;
					break;
				}
			}
			else
			{
				paddleIdle[i] += Time.deltaTime;
			}
			bool flag = paddleIdle[i] < 30f;
			if (p[i].gameObject.activeSelf != flag)
			{
				if (flag)
				{
					PlaySound(4);
					Vector3 localPosition = p[i].transform.localPosition;
					localPosition.y = 0f;
					requestedPos[i] = localPosition.y;
					p[i].transform.localPosition = localPosition;
				}
				p[i].gameObject.SetActive(paddleIdle[i] < 30f);
			}
			if (p[i].gameObject.activeInHierarchy && Mathf.Abs(ball.localPosition.x - p[i].transform.localPosition.x) < 0.1f && Mathf.Abs(ball.localPosition.y - p[i].transform.localPosition.y) < 0.5f)
			{
				ballTrajectory.y = (ball.localPosition.y - p[i].transform.localPosition.y) * 1.25f;
				float x = ballTrajectory.x;
				if (p[i].Right)
				{
					ballTrajectory.x = Mathf.Abs(ballTrajectory.y) - 1f;
				}
				else
				{
					ballTrajectory.x = 1f - Mathf.Abs(ballTrajectory.y);
				}
				if (x > 0f != ballTrajectory.x > 0f)
				{
					PlaySound(1);
				}
				ballTrajectory.Normalize();
				gameBallSpeed += ballSpeedBoost;
			}
		}
	}

	private void UpdateScore()
	{
		if (scoreFormat != null)
		{
			scoreL = Mathf.Clamp(scoreL, 0, 10);
			scoreR = Mathf.Clamp(scoreR, 0, 10);
			scoreDisplay.text = string.Format(scoreFormat, scoreL, scoreR);
		}
	}

	private float ByteToYPos(byte Y)
	{
		return (float)(int)Y / yPosToByteFactor - tableSizeBall.y;
	}

	private byte YPosToByte(float Y)
	{
		return (byte)Mathf.RoundToInt((Y + tableSizeBall.y) * yPosToByteFactor);
	}

	public override byte[] GetNetworkState()
	{
		netStateCur.P0LocY = YPosToByte(p[0].transform.localPosition.y);
		netStateCur.P1LocY = YPosToByte(p[1].transform.localPosition.y);
		netStateCur.P2LocY = YPosToByte(p[2].transform.localPosition.y);
		netStateCur.P3LocY = YPosToByte(p[3].transform.localPosition.y);
		netStateCur.BallLocX = ball.localPosition.x;
		netStateCur.BallLocY = YPosToByte(ball.localPosition.y);
		netStateCur.BallTrajectoryX = (byte)((ballTrajectory.x + 1f) * 127.5f);
		netStateCur.BallTrajectoryY = (byte)((ballTrajectory.y + 1f) * 127.5f);
		netStateCur.BallSpeed = gameBallSpeed;
		netStateCur.ScoreLeft = scoreL;
		netStateCur.ScoreRight = scoreR;
		netStateCur.ScreenMode = (int)currentScreenMode;
		if (!netStateCur.Equals(netStateLast))
		{
			netStateLast = netStateCur;
			SwapNetStateBuffersAndStreams();
			ArcadeGame.WrapNetState(netStateLast, netStateMemStream);
		}
		return netStateBuffer;
	}

	public override void SetNetworkState(byte[] b)
	{
		PaddleballNetState paddleballNetState = (PaddleballNetState)ArcadeGame.UnwrapNetState(b);
		officialPos[0] = ByteToYPos(paddleballNetState.P0LocY);
		officialPos[1] = ByteToYPos(paddleballNetState.P1LocY);
		officialPos[2] = ByteToYPos(paddleballNetState.P2LocY);
		officialPos[3] = ByteToYPos(paddleballNetState.P3LocY);
		Vector2 vector = new Vector2(paddleballNetState.BallLocX, ByteToYPos(paddleballNetState.BallLocY));
		Vector2 normalized = new Vector2((float)(int)paddleballNetState.BallTrajectoryX * 0.007843138f - 1f, (float)(int)paddleballNetState.BallTrajectoryY * 0.007843138f - 1f).normalized;
		Vector2 vector2 = vector - normalized * Vector2.Dot(vector, normalized);
		Vector2 vector3 = ball.localPosition.xy();
		Vector2 vector4 = vector3 - ballTrajectory * Vector2.Dot(vector3, ballTrajectory);
		if ((vector2 - vector4).IsLongerThan(0.1f))
		{
			ball.localPosition = vector;
			ballTrajectory = normalized.xy();
		}
		gameBallSpeed = paddleballNetState.BallSpeed;
		ChangeScreen((ScreenMode)paddleballNetState.ScreenMode);
		if (scoreL != paddleballNetState.ScoreLeft || scoreR != paddleballNetState.ScoreRight)
		{
			scoreL = paddleballNetState.ScoreLeft;
			scoreR = paddleballNetState.ScoreRight;
			UpdateScore();
		}
	}

	protected override void ButtonUp(int player, ArcadeButtons button)
	{
	}

	protected override void ButtonDown(int player, ArcadeButtons button)
	{
	}

	private void ChangeScreen(ScreenMode mode)
	{
		if (currentScreenMode != mode)
		{
			switch (currentScreenMode)
			{
			case ScreenMode.BlackWin:
				blackWinScreen.SetActive(value: false);
				break;
			case ScreenMode.WhiteWin:
				whiteWinScreen.SetActive(value: false);
				break;
			case ScreenMode.Title:
				titleScreen.SetActive(value: false);
				break;
			case ScreenMode.Gameplay:
				ball.gameObject.SetActive(value: false);
				break;
			}
			currentScreenMode = mode;
			switch (mode)
			{
			case ScreenMode.BlackWin:
				blackWinScreen.SetActive(value: true);
				returnToTitleAfterTimestamp = Time.time + winScreenDuration;
				PlaySound(3);
				break;
			case ScreenMode.WhiteWin:
				whiteWinScreen.SetActive(value: true);
				returnToTitleAfterTimestamp = Time.time + winScreenDuration;
				PlaySound(3);
				break;
			case ScreenMode.Title:
				gameBallSpeed = initialBallSpeed;
				scoreL = 0;
				scoreR = 0;
				UpdateScore();
				returnToTitleAfterTimestamp = 0f;
				titleScreen.SetActive(value: true);
				break;
			case ScreenMode.Gameplay:
				ball.gameObject.SetActive(value: true);
				returnToTitleAfterTimestamp = Time.time + 30f;
				break;
			}
		}
	}

	public override void OnTimeout()
	{
		ChangeScreen(ScreenMode.Title);
	}

	public override void ReadPlayerDataPUN(int player, PhotonStream stream, PhotonMessageInfo info)
	{
		requestedPos[player] = ByteToYPos((byte)stream.ReceiveNext());
	}

	public override void WritePlayerDataPUN(int player, PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(YPosToByte(requestedPos[player]));
	}
}
