using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using UnityEngine;
using UnityEngine.Networking;

public class TextureFromURL : MonoBehaviour
{
	private enum Source
	{
		TitleData,
		URL
	}

	[SerializeField]
	private Renderer _renderer;

	[SerializeField]
	private Source source;

	[Tooltip("If Source is set to 'TitleData' Data should be the id of the title data entry that defines an image URL. If Source is set to 'URL' Data should be a URL that points to an image.")]
	[SerializeField]
	private string data;

	private Texture2D texture;

	private int maxTitleDataAttempts = 10;

	private void OnEnable()
	{
		if (data.Length != 0)
		{
			if (source == Source.TitleData)
			{
				LoadFromTitleData();
			}
			else
			{
				applyRemoteTexture(data);
			}
		}
	}

	private async void LoadFromTitleData()
	{
		for (int attempt = 0; attempt < maxTitleDataAttempts; attempt++)
		{
			if (!(PlayFabTitleDataCache.Instance == null))
			{
				break;
			}
			await Task.Delay(1000);
		}
		if (PlayFabTitleDataCache.Instance != null)
		{
			PlayFabTitleDataCache.Instance.GetTitleData(data, OnTitleDataRequestComplete, OnPlayFabError);
		}
	}

	private void OnDisable()
	{
		if (texture != null)
		{
			Object.Destroy(texture);
			texture = null;
		}
	}

	private void OnPlayFabError(PlayFabError error)
	{
	}

	private void OnTitleDataRequestComplete(string imageUrl)
	{
		imageUrl = imageUrl.Replace("\\r", "\r").Replace("\\n", "\n");
		if (imageUrl[0] == '"' && imageUrl[imageUrl.Length - 1] == '"')
		{
			imageUrl = imageUrl.Substring(1, imageUrl.Length - 2);
		}
		applyRemoteTexture(imageUrl);
	}

	private async void applyRemoteTexture(string imageUrl)
	{
		texture = await GetRemoteTexture(imageUrl);
		if (texture != null)
		{
			_renderer.material.mainTexture = texture;
		}
	}

	private async Task<Texture2D> GetRemoteTexture(string url)
	{
		using UnityWebRequest wr = UnityWebRequestTexture.GetTexture(url);
		UnityWebRequestAsyncOperation asyncOp = wr.SendWebRequest();
		while (!asyncOp.isDone)
		{
			await Task.Delay(1000);
		}
		if (wr.result == UnityWebRequest.Result.Success)
		{
			return DownloadHandlerTexture.GetContent(wr);
		}
		return null;
	}
}
