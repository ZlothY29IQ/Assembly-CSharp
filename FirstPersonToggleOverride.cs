using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FirstPersonToggleOverride : MonoBehaviour
{
	[SerializeField]
	private Renderer _renderer;

	[SerializeField]
	private bool toggle;

	[SerializeField]
	private bool doNotToggle = true;

	public bool Toggle => toggle;

	public Renderer Renderer => _renderer;
}
