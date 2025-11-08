using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!QATESTING")]
public class TestScript : MonoBehaviour
{
	public GameObject testDelete;

	public int callbackOrder => 0;
}
