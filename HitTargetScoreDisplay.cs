using System.Collections;
using GorillaTag;
using UnityEngine;

public class HitTargetScoreDisplay : MonoBehaviour
{
	[SerializeField]
	private WatchableIntSO networkedScore;

	private int currentScore;

	private int tensOld;

	private int hundredsOld;

	private float rotateTimeTotal;

	private MaterialPropertyBlock matPropBlock;

	private readonly Vector4[] numberSheet = new Vector4[10]
	{
		new Vector4(1f, 1f, 0.8f, -0.5f),
		new Vector4(1f, 1f, 0f, 0f),
		new Vector4(1f, 1f, 0.2f, 0f),
		new Vector4(1f, 1f, 0.4f, 0f),
		new Vector4(1f, 1f, 0.6f, 0f),
		new Vector4(1f, 1f, 0.8f, 0f),
		new Vector4(1f, 1f, 0f, -0.5f),
		new Vector4(1f, 1f, 0.2f, -0.5f),
		new Vector4(1f, 1f, 0.4f, -0.5f),
		new Vector4(1f, 1f, 0.6f, -0.5f)
	};

	public int rotateSpeed = 180;

	public Transform singlesCard;

	public Transform tensCard;

	public Transform hundredsCard;

	public Renderer singlesRend;

	public Renderer tensRend;

	public Renderer hundredsRend;

	private Coroutine currentRotationCoroutine;

	protected void Awake()
	{
		rotateTimeTotal = 180f / (float)rotateSpeed;
		matPropBlock = new MaterialPropertyBlock();
		networkedScore.AddCallback(OnScoreChanged, shouldCallbackNow: true);
		ResetRotation();
		tensOld = 0;
		hundredsOld = 0;
		matPropBlock.SetVector(ShaderProps._BaseMap_ST, numberSheet[0]);
		singlesRend.SetPropertyBlock(matPropBlock);
		tensRend.SetPropertyBlock(matPropBlock);
		hundredsRend.SetPropertyBlock(matPropBlock);
	}

	private void OnDestroy()
	{
		networkedScore.RemoveCallback(OnScoreChanged);
	}

	private void ResetRotation()
	{
		Quaternion rotation = base.transform.rotation;
		singlesCard.rotation = rotation;
		tensCard.rotation = rotation;
		hundredsCard.rotation = rotation;
	}

	private IEnumerator RotatingCo()
	{
		float timeElapsedSinceHit = 0f;
		int singlesPlace = currentScore % 10;
		int tensPlace = currentScore / 10 % 10;
		bool tensChange = tensOld != tensPlace;
		tensOld = tensPlace;
		int hundredsPlace = currentScore / 100 % 10;
		bool hundredsChange = hundredsOld != hundredsPlace;
		hundredsOld = hundredsPlace;
		bool digitsChange = true;
		for (; !(timeElapsedSinceHit >= rotateTimeTotal); timeElapsedSinceHit += Time.deltaTime)
		{
			singlesCard.Rotate((float)rotateSpeed * Time.deltaTime, 0f, 0f, Space.Self);
			Vector3 localEulerAngles = singlesCard.localEulerAngles;
			localEulerAngles.x = Mathf.Clamp(localEulerAngles.x, 0f, 180f);
			singlesCard.localEulerAngles = localEulerAngles;
			if (tensChange)
			{
				tensCard.Rotate((float)rotateSpeed * Time.deltaTime, 0f, 0f, Space.Self);
				Vector3 localEulerAngles2 = tensCard.localEulerAngles;
				localEulerAngles2.x = Mathf.Clamp(localEulerAngles2.x, 0f, 180f);
				tensCard.localEulerAngles = localEulerAngles2;
			}
			if (hundredsChange)
			{
				hundredsCard.Rotate((float)rotateSpeed * Time.deltaTime, 0f, 0f, Space.Self);
				Vector3 localEulerAngles3 = hundredsCard.localEulerAngles;
				localEulerAngles3.x = Mathf.Clamp(localEulerAngles3.x, 0f, 180f);
				hundredsCard.localEulerAngles = localEulerAngles3;
			}
			if (digitsChange && timeElapsedSinceHit >= rotateTimeTotal / 2f)
			{
				matPropBlock.SetVector(ShaderProps._BaseMap_ST, numberSheet[singlesPlace]);
				singlesRend.SetPropertyBlock(matPropBlock);
				if (tensChange)
				{
					matPropBlock.SetVector(ShaderProps._BaseMap_ST, numberSheet[tensPlace]);
					tensRend.SetPropertyBlock(matPropBlock);
				}
				if (hundredsChange)
				{
					matPropBlock.SetVector(ShaderProps._BaseMap_ST, numberSheet[hundredsPlace]);
					hundredsRend.SetPropertyBlock(matPropBlock);
				}
				digitsChange = false;
			}
			yield return null;
		}
		ResetRotation();
	}

	private void OnScoreChanged(int newScore)
	{
		if (newScore != currentScore)
		{
			if (currentRotationCoroutine != null)
			{
				StopCoroutine(currentRotationCoroutine);
			}
			currentScore = newScore;
			if (base.gameObject.activeInHierarchy)
			{
				currentRotationCoroutine = StartCoroutine(RotatingCo());
			}
		}
	}
}
