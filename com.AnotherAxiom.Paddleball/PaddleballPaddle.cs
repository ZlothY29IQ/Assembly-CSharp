using UnityEngine;

namespace com.AnotherAxiom.Paddleball;

public class PaddleballPaddle : MonoBehaviour
{
	[SerializeField]
	private bool right;

	public bool Right => right;
}
