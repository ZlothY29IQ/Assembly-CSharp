using UnityEngine;

public class AnimationEventController : MonoBehaviour
{
	public GameObject fxAttack;

	public void TriggerAttackVFX()
	{
		fxAttack.SetActive(value: false);
		fxAttack.SetActive(value: true);
	}
}
