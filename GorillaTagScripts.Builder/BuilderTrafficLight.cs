using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderTrafficLight : MonoBehaviour, IBuilderPieceComponent
{
	private enum LightState
	{
		Red,
		Yellow,
		Green,
		Off
	}

	[SerializeField]
	private BuilderPiece piece;

	[SerializeField]
	private MeshRenderer redLight;

	[SerializeField]
	private MeshRenderer yellowLight;

	[SerializeField]
	private MeshRenderer greenLight;

	[SerializeField]
	private float cycleDuration = 10f;

	[SerializeField]
	private float startPercentageOffset = 0.5f;

	[SerializeField]
	private Color redOn = Color.red;

	[SerializeField]
	private Color redOff = Color.gray;

	[SerializeField]
	private Color yellowOn = Color.yellow;

	[SerializeField]
	private Color yellowOff = Color.gray;

	[SerializeField]
	private Color greenOn = Color.green;

	[SerializeField]
	private Color greenOff = Color.gray;

	private MaterialPropertyBlock materialProps;

	[SerializeField]
	private AnimationCurve stateCurve;

	private LightState lightState = LightState.Off;

	private void Start()
	{
		materialProps = new MaterialPropertyBlock();
	}

	private void SetState(LightState state)
	{
		lightState = state;
		if (materialProps == null)
		{
			materialProps = new MaterialPropertyBlock();
		}
		Color value = yellowOff;
		Color value2 = redOff;
		Color value3 = greenOff;
		switch (state)
		{
		case LightState.Red:
			value2 = redOn;
			break;
		case LightState.Yellow:
			value = yellowOn;
			break;
		case LightState.Green:
			value3 = greenOn;
			break;
		}
		redLight.GetPropertyBlock(materialProps);
		materialProps.SetColor(ShaderProps._BaseColor, value2);
		redLight.SetPropertyBlock(materialProps);
		materialProps.SetColor(ShaderProps._BaseColor, value);
		yellowLight.SetPropertyBlock(materialProps);
		materialProps.SetColor(ShaderProps._BaseColor, value3);
		greenLight.SetPropertyBlock(materialProps);
	}

	private void Update()
	{
		if (!(piece == null) && piece.state != BuilderPiece.State.AttachedAndPlaced)
		{
			return;
		}
		float num = Time.time;
		if (PhotonNetwork.InRoom)
		{
			uint num2 = (num2 = (uint)PhotonNetwork.ServerTimestamp);
			if (piece != null)
			{
				num2 = (uint)(PhotonNetwork.ServerTimestamp - piece.activatedTimeStamp);
			}
			num = (float)num2 / 1000f;
		}
		float num3 = num % cycleDuration / cycleDuration;
		num3 = (num3 + startPercentageOffset) % 1f;
		int num4 = (int)stateCurve.Evaluate(num3);
		if (num4 != (int)lightState)
		{
			SetState((LightState)num4);
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		SetState(LightState.Off);
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
	}

	public void OnPieceDeactivate()
	{
		SetState(LightState.Off);
	}
}
