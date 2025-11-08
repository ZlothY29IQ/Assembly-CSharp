using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BitmapFont : ScriptableObject
{
	[Serializable]
	public struct SymbolData
	{
		public char character;

		[Space]
		public int id;

		public int width;

		public int height;

		public int x;

		public int y;

		public int xadvance;

		public int yoffset;
	}

	public Texture2D fontImage;

	public TextAsset fontJson;

	public int symbolPixelsPerUnit = 1;

	public string characterMap;

	[Space]
	public SymbolData[] symbols = new SymbolData[0];

	private Dictionary<char, SymbolData> _charToSymbol;

	private Color[] _empty = new Color[0];

	private void OnEnable()
	{
		_charToSymbol = symbols.ToDictionary((SymbolData s) => s.character, (SymbolData s) => s);
	}

	public void RenderToTexture(Texture2D target, string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		int num = target.width * target.height;
		if (_empty.Length != num)
		{
			_empty = new Color[num];
			for (int i = 0; i < _empty.Length; i++)
			{
				_empty[i] = Color.black;
			}
		}
		target.SetPixels(_empty);
		int length = text.Length;
		int num2 = 1;
		_ = fontImage.width;
		int height = fontImage.height;
		for (int j = 0; j < length; j++)
		{
			char key = text[j];
			SymbolData symbolData = _charToSymbol[key];
			int width = symbolData.width;
			int height2 = symbolData.height;
			int x = symbolData.x;
			int y = symbolData.y;
			Graphics.CopyTexture(fontImage, 0, 0, x, height - (y + height2), width, height2, target, 0, 0, num2, 2 + symbolData.yoffset);
			num2 += width + 1;
		}
		target.Apply(updateMipmaps: false);
	}
}
