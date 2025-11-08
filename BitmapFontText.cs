using UnityEngine;

public class BitmapFontText : MonoBehaviour
{
	public string text;

	public bool uppercaseOnly;

	public Vector2Int textArea;

	[Space]
	public Renderer renderer;

	public Texture2D texture;

	public Material material;

	public BitmapFont font;

	private void Awake()
	{
		Init();
		Render();
	}

	public void Render()
	{
		font.RenderToTexture(texture, uppercaseOnly ? text.ToUpperInvariant() : text);
	}

	public void Init()
	{
		texture = new Texture2D(textArea.x, textArea.y, font.fontImage.format, mipChain: false);
		texture.filterMode = FilterMode.Point;
		material = new Material(renderer.sharedMaterial);
		material.mainTexture = texture;
		renderer.sharedMaterial = material;
	}
}
