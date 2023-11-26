using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
	private const int BOARD_SIZE = 10;


	[SerializeField] Grid grid;
	[SerializeField] Tilemap mainLayer;
	[SerializeField] Tilemap missleLayer;
	[Space]

	[SerializeField] private Vector2Int playerGridStart;
	[SerializeField] private Vector2Int computerGridStart;
	[Space]

	[SerializeField] private TileBase waterTile;
	[SerializeField] private TileBase hit;
	[SerializeField] private TileBase miss;

	private PlayerShip selectedShip = null;

	// These tiles can by any state in TileState
	private GameManager.TileState[,] playerTiles;
	private GameManager.TileState[,] computerTiles;

	private void Start()
	{
		playerTiles = new GameManager.TileState[BOARD_SIZE, BOARD_SIZE];
		computerTiles = new GameManager.TileState[BOARD_SIZE, BOARD_SIZE];

	}

	public Vector3Int WorldToCell(Vector3 position)
	{
		return grid.WorldToCell(position);
	}

	public void ActivateShipSelection(bool isOn)
	{
		PlayerShip.AllowSelection = isOn;
	}

	public void SelectShip(PlayerShip ship)
	{
		selectedShip = ship;
	}

	public bool TrySelectTileForShipPlacement(Vector3 position)
	{
		Vector3Int coor = grid.WorldToCell(position);
		//Debug.Log(coor);

		// Adjust the coordinates to the playerGrid so it lines up with playerTiles
		Vector3Int adjTileCoor = new Vector3Int(coor.x - playerGridStart.x, -1 * (coor.y - playerGridStart.y), 0);
		//Debug.Log(adjTileCoor);
		// Stay in bounds
		if (adjTileCoor.x < 0 || adjTileCoor.x >= BOARD_SIZE ||
			adjTileCoor.y < 0 || adjTileCoor.y >= BOARD_SIZE)
		{
			return false;
		}

		GameManager.TileState selectedTile = playerTiles[adjTileCoor.x, adjTileCoor.y];
		// Tile must be empty to place a ship there
		if (selectedTile == GameManager.TileState.EMPTY)
		{
			GameManager.TileState newTileState = GameManager.TileState.SHIP;
			Ship.Orientation orientation = selectedShip.orientation;
			int shipLength = selectedShip.Length;

			// Keep track of the tiles we change in case we have to revert them
			Dictionary<Vector2Int, GameManager.TileState> changedTiles =
				new Dictionary<Vector2Int, GameManager.TileState>(shipLength);

			// Check the length of the ship and change every tile based on the orientation
			for (int i = 0; i < shipLength; i++)
			{
				int tileX;
				int tileY;
				bool isOutOfBounds;
				switch (orientation)
				{
					case Ship.Orientation.DOWN:
						tileX = adjTileCoor.x;
						tileY = adjTileCoor.y + i;
						isOutOfBounds = adjTileCoor.y + (shipLength - 1) >= BOARD_SIZE;
						break;
					case Ship.Orientation.RIGHT:
						tileX = adjTileCoor.x + i;
						tileY = adjTileCoor.y;
						isOutOfBounds = adjTileCoor.x + (shipLength - 1) >= BOARD_SIZE;
						break;
					case Ship.Orientation.UP:
						tileX = adjTileCoor.x;
						tileY = adjTileCoor.y - i;
						isOutOfBounds = adjTileCoor.y - (shipLength - 1) < 0;
						break;
					case Ship.Orientation.LEFT:
						tileX = adjTileCoor.x - i;
						tileY = adjTileCoor.y;
						isOutOfBounds = adjTileCoor.x - (shipLength - 1) < 0;
						break;
					default:
						Debug.LogError(orientation);
						return false;
				}

				// if at any point we run into an invalid tile, restore all the tiles we changed so far
				// and stop. Another tile will have to be chosen
				if (isOutOfBounds || playerTiles[tileX, tileY] != GameManager.TileState.EMPTY)
				{
					//Debug.Log("Invalid Placement");
					RestoreChangedTiles(changedTiles);
					selectedShip.ResetBoardLocations();
					//PrintTiles(playerTiles);
					return false;
				}

				Vector2Int tileLoc = new Vector2Int(tileX, tileY);
				// Keep track of the changed tiles and update playerTiles
				changedTiles.Add(tileLoc, playerTiles[tileX, tileY]);
				playerTiles[tileX, tileY] = newTileState;
				selectedShip.BoardLocations[i] = tileLoc;
			}

			selectedShip.PlaceShip(coor);
			selectedShip = null;
			//PrintTiles(playerTiles);
			return true;
		}

		return false;
	}

	private void RestoreChangedTiles(Dictionary<Vector2Int, GameManager.TileState> changedTiles)
	{
		foreach(KeyValuePair<Vector2Int, GameManager.TileState> tile in changedTiles)
		{
			playerTiles[tile.Key.x, tile.Key.y] = tile.Value;
		}
	}

	public bool TryAttackThePlayer(Vector2Int relativeTileCoor, out GameManager.TileState tileState)
	{
		// Adjust coordinates for the player grid
		Vector3Int tileCoor =
			new Vector3Int(
				relativeTileCoor.x + playerGridStart.x,
				-1 * (relativeTileCoor.y - playerGridStart.y),
				0);
		return TryAttack(tileCoor, out tileState);
	}

	// Returns true if valid move, false otherwise
	public bool TryAttack(Vector3Int tileCoor, out GameManager.TileState tileState)
	{
		tileCoor.z = 0;
		tileState = GameManager.TileState.EMPTY;

		bool isPlayerTurn = GameManager.instance.IsPlayerTurn();
		//Debug.Log("tileCoor: " + tileCoor);
		TileBase tile = mainLayer.GetTile(tileCoor);
		// Only the enemy board has Hidden tiles
		if (tile != null && tile.name.Equals("Hidden"))
		{
			mainLayer.SetTile(tileCoor, waterTile);
		}
		// Player has to chose another tile. Enemy is targeting the player board which has no Hidden tiles,
		// so it continues forward regardless
		else if (isPlayerTurn)
		{
			return false;
		}

		// adjust coordinates for the relevant board
		Vector2Int adjTileCoor = isPlayerTurn ?
			new Vector2Int(tileCoor.x - computerGridStart.x, -1 * (tileCoor.y - computerGridStart.y)) :
			new Vector2Int(tileCoor.x - playerGridStart.x, -1 * (tileCoor.y - playerGridStart.y));

		//Debug.Log("adjTileCoor: " + adjTileCoor);

		GameManager.TileState targetedTileState = isPlayerTurn ?
				computerTiles[adjTileCoor.x, adjTileCoor.y] : playerTiles[adjTileCoor.x, adjTileCoor.y];

		///Debug.Log(currTileState);
		GameManager.TileState newTileState = targetedTileState;

		// if the targetedTile is empty, then it's a MISS, any SHIP is a hit, and otherwise return false,
		// it's an invalid choice and they need to pick again
		switch (targetedTileState)
		{
			case GameManager.TileState.EMPTY:
				newTileState = GameManager.TileState.MISS;
				missleLayer.SetTile(tileCoor, miss);
				break;
			case GameManager.TileState.SHIP:
				newTileState = GameManager.TileState.HIT;
				missleLayer.SetTile(tileCoor, hit);
				// Update the ship's health here
				if (isPlayerTurn)
				{
					DamageShip(GameManager.EnemyShips, new Vector2Int(adjTileCoor.x, adjTileCoor.y));
				}
				else
				{
					DamageShip(GameManager.PlayerShips, new Vector2Int(adjTileCoor.x, adjTileCoor.y));
				}
				break;
			case GameManager.TileState.HIT:
			case GameManager.TileState.MISS:
				return false;
		}

		//Debug.Log(newTileState);
		// Add the results to the Battle Log so the player can understand what happened
		if (isPlayerTurn)
		{
			GameManager.instance.ui.PlayerLogAttack(newTileState == GameManager.TileState.HIT, adjTileCoor);
			computerTiles[adjTileCoor.x, adjTileCoor.y] = newTileState;
			//PrintTiles(computerTiles);
		}
		else
		{
			GameManager.instance.ui.EnemyLogAttack(newTileState == GameManager.TileState.HIT, adjTileCoor);
			playerTiles[adjTileCoor.x, adjTileCoor.y] = newTileState;
			//PrintTiles(playerTiles);
		}
		tileState = newTileState;
		return true;
	}

	public void SetComputerTileToShip(Vector2Int pos)
	{
		computerTiles[pos.x, pos.y] = GameManager.TileState.SHIP;
	}

	private void DamageShip(Ship[] ships, Vector2Int loc)
	{
		foreach (Ship ship in ships)
		{
			if (ship.IsLocatedHere(loc))
			{
				ship.TakeDamage();
			}
		}
	}	

	public void PrintTiles(GameManager.TileState[,] tiles)
	{
		string msg = "Tiles\n\t0\t1\t2\t3\t4\t5\t6\t7\t8\t9\n";

		for (int y = 0; y < BOARD_SIZE; y++)
		{
			msg += y + "\t";
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				switch(tiles[x, y])
				{
					case GameManager.TileState.EMPTY:		msg += "O\t";						break;
					case GameManager.TileState.SHIP:	msg += "<color=cyan>S</color>\t";   break;
					case GameManager.TileState.HIT:			msg += "<color=red>X</color>\t";    break;
					case GameManager.TileState.MISS:		msg += "<color=black>X</color>\t";  break;
				}
			}
			msg += "\n";
		}
		Debug.Log(msg);
	}
}
