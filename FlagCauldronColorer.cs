using UnityEngine;

public class FlagCauldronColorer : MonoBehaviour
{
	public enum ColorMode
	{
		None,
		Red,
		Green,
		Blue,
		Black,
		Clear
	}

	public ColorMode mode;

	public Transform colorPoint;
}
