using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Manager : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI battleLog;
	[Space]

	[SerializeField] private TextMeshProUGUI playerInfo;
	[SerializeField] private TextMeshProUGUI enemyInfo;
	[Space]

	[SerializeField] private GameObject gameOverScreen;
	[SerializeField] private TextMeshProUGUI gameOverMessage;
	[Space]

	[SerializeField] private GameObject difficultyScreen;

	private const int MAX_EVENTS = 6;
	private int battleLogEvents = 0;

	private bool loading;

	private void Start()
	{
		if (battleLog != null)
		{
			battleLog.text = "";
		}

		if (GameManager.instance != null && GameManager.PlayerShips != null)
		{
			for (int i = 0; i < GameManager.PlayerShips.Length; i++)
			{
				GameManager.PlayerShips[i].ShipDestroyed += new Ship.ShipDestroyedHandler(PlayerShipDestroyed);
			}
			for (int i = 0; i < GameManager.EnemyShips.Length; i++)
			{
				GameManager.EnemyShips[i].ShipDestroyed += new Ship.ShipDestroyedHandler(ComputerShipDestroyed);
			}
		}
	}

	public void DisplayDifficultyOptions(bool isOn)
	{
		difficultyScreen.SetActive(isOn);
	}
	public void NotifyPlayer(string msg)
	{
		battleLog.text = msg;
	}

	public void PlayerLogAttack(bool hit, Vector2Int pos)
	{
		if (hit)
		{
			AddToBattleLog("<color=#00ffffff>PLAYER</color>: ENEMY SHIP DAMAGED AT " + PositionToBoard(pos));
		}
		else
		{
			AddToBattleLog("<color=#00ffffff>PLAYER</color>: NO ENEMY SHIPS AT " + PositionToBoard(pos));
		}
	}

	public void EnemyLogAttack(bool hit, Vector2Int pos)
	{
		if (hit)
		{
			AddToBattleLog("<color=orange>ENEMY</color>:  YOUR SHIP WAS DAMAGED AT " + PositionToBoard(pos));
		}
		else
		{
			AddToBattleLog("<color=orange>ENEMY</color>:  ENEMY MISSED AT " + PositionToBoard(pos));
		}
	}

	private void AddToBattleLog(string log)
	{
		if(battleLogEvents == 0)
		{
			battleLog.text = "";
		}
		battleLog.text += log + "\n";

		battleLogEvents++;
		if (battleLogEvents > MAX_EVENTS)
		{
			int firstLineIndex = battleLog.text.IndexOf("\n") + 1;
			battleLog.text = battleLog.text.Substring(firstLineIndex, battleLog.text.Length - firstLineIndex);
		}
	}
	private string PositionToBoard(Vector2Int pos)
	{
		return (char)(pos.y + 65) + "_" + (pos.x + 1);
	}

	public void PlayerShipDestroyed(Ship _)
	{
		string info = "<color=#00ffffff>PLAYER</color> STATUS\n";
		info = ShipDestroyed(info, GameManager.PlayerShips);
		playerInfo.text = info;

		AddToBattleLog("<color=orange>ENEMY</color>:  <color=red>WARNING:</color> YOUR SHIP WAS DESTROYED");
	}

	public void ComputerShipDestroyed(Ship _)
	{
		string info = "<color=orange>ENEMY</color> STATUS\n";
		info = ShipDestroyed(info, GameManager.EnemyShips);
		enemyInfo.text = info;

		AddToBattleLog("<color=#00ffffff>PLAYER</color>: <color=green>SUCCESS:</color> ENEMY SHIP WAS DESTROYED");
	}

	private string ShipDestroyed(string info, Ship[] ships)
	{
		info += "+=================+\n";
		foreach (Ship ship in ships)
		{
			info += (ship.ShipClass + ":").PadRight(12) +
				"<color=" + (ship.IsAlive ? "green>ONLINE" : "red>OFFLINE") + "</color>\n";
		}
		info += "+=================+";

		return info;
	}

	public void GameOver(bool didPlayerWin)
	{
		gameOverScreen.SetActive(true);
		gameOverMessage.text = didPlayerWin ? "<color=green>YOU WON" : "<color=red>YOU LOST";
	}

	public void SetDifficultyToEasy()
	{
		GameManager.instance.SetDifficulty(AI_Behaviour.Difficulty.EASY);
	}
	public void SetDifficultyToMedium()
	{
		GameManager.instance.SetDifficulty(AI_Behaviour.Difficulty.MEDIUM);
	}
	public void SetDifficultyToHard()
	{
		GameManager.instance.SetDifficulty(AI_Behaviour.Difficulty.HARD);
	}
	public void StartGame()
	{
		SceneManager.LoadScene(1);
	}
	public void QuitGame()
	{
		Application.Quit();
	}
}
