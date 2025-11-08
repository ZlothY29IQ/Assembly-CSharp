using UnityEngine;

public class RandomizeWavePhaseOffset : MonoBehaviour
{
	[SerializeField]
	private float minPhaseOffset;

	[SerializeField]
	private float maxPhaseOffset;

	private void Start()
	{
		Material material = GetComponent<MeshRenderer>().material;
		UberShader.VertexWavePhaseOffset.SetValue(material, Random.Range(minPhaseOffset, maxPhaseOffset));
	}
}
