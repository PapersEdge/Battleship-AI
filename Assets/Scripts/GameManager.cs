using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public enum GameState
	{
		CHOOSE_DIFFICULTY,

		ENEMY_SETUP,

		PLAYER_SETUP_SELECT_SHIP_1,
		PLAYER_SETUP_SELECT_TILE_1,

		PLAYER_SETUP_SELECT_SHIP_2,
		PLAYER_SETUP_SELECT_TILE_2,

		PLAYER_SETUP_SELECT_SHIP_3,
		PLAYER_SETUP_SELECT_TILE_3,

		PLAYER_SETUP_SELECT_SHIP_4,
		PLAYER_SETUP_SELECT_TILE_4,

		PLAYER_SETUP_SELECT_SHIP_5,
		PLAYER_SETUP_SELECT_TILE_5,

		PLAYER_TURN,
		ENEMY_TURN,

		END
	}

	public enum TileState
	{
		EMPTY,
		SHIP,
		MISS,
		HIT,
		SUNK
	}

	public static GameManager instance = null;
	[SerializeField] public GridManager grid;
	[SerializeField] public AI_Behaviour ai;
	[SerializeField] public UI_Manager ui;
	[Space]

	[SerializeField] private PlayerShip[] playerShips;
	[SerializeField] private EnemyShip[] enemyShips;

	public static PlayerShip[] PlayerShips => instance.playerShips;
	public static EnemyShip[] EnemyShips => instance.enemyShips;

	public static GameState gameState { get; private set; }

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(this);
		}

		gameState = GameState.CHOOSE_DIFFICULTY;

		for(int i = 0; i < PlayerShips.Length; i++)
		{
			PlayerShips[i].ShipDestroyed += new Ship.ShipDestroyedHandler(CheckIfEnemyWon);
		}
		for (int i = 0; i < EnemyShips.Length; i++)
		{
			EnemyShips[i].ShipDestroyed += new Ship.ShipDestroyedHandler(CheckIfPlayerWon);
		}
	}

	private void Update()
    {
		switch (gameState)
		{
			case GameState.CHOOSE_DIFFICULTY:
				ui.DisplayDifficultyOptions(true);
				break;
			case GameState.ENEMY_SETUP:
				EnemySetup();
				ui.NotifyPlayer("1. Click a SHIP to pick it up\n" +
					"2. Scroll Wheel Up/Down or Right Click to rotate the ship\n" +
					"3. Click anywhere on your board to place the ship.\n" +
					"   Ships may not overlap each other and every part\n" +
					"   of the ship must be inside the board\n" +
					"4. Repeat steps until all ships have been placed");
				break;

			case GameState.PLAYER_SETUP_SELECT_SHIP_1:
			case GameState.PLAYER_SETUP_SELECT_SHIP_2:
			case GameState.PLAYER_SETUP_SELECT_SHIP_3:
			case GameState.PLAYER_SETUP_SELECT_SHIP_4:
			case GameState.PLAYER_SETUP_SELECT_SHIP_5:
				ActivateShipSelection();
				break;

			case GameState.PLAYER_SETUP_SELECT_TILE_1:
			case GameState.PLAYER_SETUP_SELECT_TILE_2:
			case GameState.PLAYER_SETUP_SELECT_TILE_3:
			case GameState.PLAYER_SETUP_SELECT_TILE_4:
			case GameState.PLAYER_SETUP_SELECT_TILE_5:
				PlayerSelectTile();
				break;

			case GameState.PLAYER_TURN:
				PlayerTurn();
				break;
			case GameState.ENEMY_TURN:
				EnemyTurn();
				break;

			case GameState.END:
				// p1 turn
				break;
		}
    }

	public void SetDifficulty(AI_Behaviour.Difficulty difficulty)
	{
		ai.difficulty = difficulty;
		ui.DisplayDifficultyOptions(false);
		gameState++;
	}

	private void ActivateShipSelection()
	{
		grid.ActivateShipSelection(true);
	}
	public void ShipWasSelected()
	{
		grid.ActivateShipSelection(false);
		gameState++;
	}

	private void PlayerSelectTile()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (grid.TrySelectTileForShipPlacement(Camera.main.ScreenToWorldPoint(Input.mousePosition)))
			{
				gameState++;
				if(gameState == GameState.PLAYER_TURN)
				{
					ui.NotifyPlayer("1. Click a tile on the enemy's board to attack it\n" +
					"2. A <color=white>MISS</color> will register as a <color=white>WHITE</color> peg\n" +
					"3. A <color=red>HIT</color> will register as a <color=red>RED</color> peg\n" +
					"4. The <color=green>Status Screens</color> in the center and the Battle Log\n" +
					"   right here will inform you when you or your enemy have\n" +
					"   sunk a ship");
				}
			}
		}
	}
	private void EnemySetup()
	{
		ai.SetupShips();
		gameState++;
	}

	private void PlayerTurn()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3Int coor = grid.WorldToCell(position);
			if (grid.TryAttack(coor, out _))
			{
				if (gameState != GameState.END)
				{
					gameState = GameState.ENEMY_TURN;
				}
			}
		}
	}
	private void EnemyTurn()
	{
		ai.TakeTurn();
		if (gameState != GameState.END)
		{
			gameState = GameState.PLAYER_TURN;
		}
	}

	public bool IsPlayerTurn()
	{
		return gameState == GameState.PLAYER_TURN;
	}

	private void CheckIfEnemyWon(Ship _)
	{
		if (CheckIfGameOver(PlayerShips))
		{
			gameState = GameState.END;
			ui.GameOver(false);
		}
	}

	private void CheckIfPlayerWon(Ship _)
	{
		if (CheckIfGameOver(EnemyShips))
		{
			gameState = GameState.END;
			ui.GameOver(true);
		}
	}
	private bool CheckIfGameOver(Ship[] ships)
	{
		foreach (Ship ship in ships)
		{
			if (ship.IsAlive)
			{
				return false;
			}
		}
		return true;
	}
}
