using System;
using TMPro;
using UnityEngine;

public class MonkeBallScoreboard : MonoBehaviour
{
	[Serializable]
	public class TeamDisplay
	{
		public TextMeshPro nameLabel;

		public TextMeshPro scoreLabel;

		public TextMeshPro playersLabel;
	}

	private MonkeBallGame game;

	public TeamDisplay[] teamDisplays;

	public TextMeshPro timeRemainingLabel;

	public AudioSource audioSource;

	public AudioClip scoreSound;

	public float scoreSoundVolume;

	public AudioClip playerJoinSound;

	public AudioClip playerLeaveSound;

	public AudioClip gameStartSound;

	public float gameStartVolume;

	public AudioClip gameEndSound;

	public float gameEndVolume;

	public void Setup(MonkeBallGame game)
	{
		this.game = game;
	}

	public void RefreshScore()
	{
		for (int i = 0; i < game.team.Count; i++)
		{
			teamDisplays[i].scoreLabel.text = game.team[i].score.ToString();
		}
	}

	public void RefreshTeamPlayers(int teamId, int numPlayers)
	{
		teamDisplays[teamId].playersLabel.text = $"PLAYERS: {Mathf.Clamp(numPlayers, 0, 99)}";
	}

	public void PlayScoreFx()
	{
		PlayFX(scoreSound, scoreSoundVolume);
	}

	public void PlayPlayerJoinFx()
	{
		PlayFX(playerJoinSound, 0.5f);
	}

	public void PlayPlayerLeaveFx()
	{
		PlayFX(playerLeaveSound, 0.5f);
	}

	public void PlayGameStartFx()
	{
		PlayFX(gameStartSound, gameStartVolume);
	}

	public void PlayGameEndFx()
	{
		PlayFX(gameEndSound, gameEndVolume);
	}

	private void PlayFX(AudioClip clip, float volume)
	{
		if (audioSource != null)
		{
			audioSource.clip = clip;
			audioSource.volume = volume;
			audioSource.Play();
		}
	}

	public void RefreshTime(string timeString)
	{
		timeRemainingLabel.text = timeString;
	}
}
