using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AI_Behaviour : MonoBehaviour
{
	// Inspiration for the Hard Mode AI Attack Algorithm cames from
	// http://www.datagenetics.com/blog/december32011/
	// This is a partial implementatioon of the algorithm

	private const int BOARD_SIZE = 10;
	public enum Difficulty
	{
		EASY,
		MEDIUM,
		HARD
	}
	private enum AttackState
	{
		HUNT,
		TARGET,
	}
	public Difficulty difficulty;

	private AttackState attackState = AttackState.HUNT;
	private Stack<Vector2Int> targetStack;
	private int parity = 2;
	private const int HEAVY_WEIGHT = 100;

	private Dictionary<Vector2Int, GameManager.TileState> aiTilemap =
		new Dictionary<Vector2Int, GameManager.TileState>(BOARD_SIZE * BOARD_SIZE);

	private Dictionary<Vector2Int, GameManager.TileState> validPlacementTiles =
		new Dictionary<Vector2Int, GameManager.TileState>(BOARD_SIZE * BOARD_SIZE);

	private Dictionary<Vector2Int, GameManager.TileState> playerTilemap =
		new Dictionary<Vector2Int, GameManager.TileState>(BOARD_SIZE * BOARD_SIZE);

	private int[,] probabilities = new int[10, 10];
	private bool isShipHorizontal;
	private int shipLength;
	private int validStartX;
	private int validStartY;

	//public Texture2D test;
	//public int bestProb = -1;

	private void Start()
	{
		for (int y = 0; y < BOARD_SIZE; y++)
		{
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				Vector2Int pos = new Vector2Int(x, y);
				aiTilemap.Add(pos, GameManager.TileState.EMPTY);
				validPlacementTiles.Add(pos, GameManager.TileState.EMPTY);
				playerTilemap.Add(pos, GameManager.TileState.EMPTY);
			}
		}

		for (int i = 0; i < GameManager.PlayerShips.Length; i++)
		{
			GameManager.PlayerShips[i].ShipDestroyed += new Ship.ShipDestroyedHandler(PlayerShipDestroyed);
		}
	}

	#region BoardSetup
	// Uses Dictionaries to determine where to place ships without overlap and at random
	// Each ship has a starting tile and an orientation that it will face, Horizontal or Vertical
	// This algorithm filters out all spaces where the ship cannot start without eventually
	// overlapping another ship.
	// Important note: regardless of orientation, ships will always be placed beginning at their starting
	// point and then approach 0 on the relevant axis
	public void SetupShips()
	{
		foreach (EnemyShip ship in GameManager.EnemyShips)
		{
			isShipHorizontal = Random.Range(0, 2) == 0 ? true : false;
			shipLength = ship.Length;

			//Debug.Log(shipLength + ": " + (isShipHorizontal ? "Horizontal" : "Vertical"));

			// depending on the length and orientation of the ship,
			// it's impossible to place the starting point on certain tiles.
			// validStarts will be used as quick exit cases in MarkInvalidTiles()
			CalculateValidStarts();

			// Marks tiles as invalid based on ships that have been placed so far
			MarkInvalidTiles();

			KeyValuePair<Vector2Int, GameManager.TileState> tile = SelectRandomValidTile();

			PlaceShip(ship, tile.Key);

			// Shallow Copy of tiles to validTiles for the next loop
			validPlacementTiles = aiTilemap.ToDictionary(entry => entry.Key, entry => entry.Value);

			PrintTiles(aiTilemap);
			PrintTiles(validPlacementTiles);
		}
	}

	private void CalculateValidStarts()
	{
		if (isShipHorizontal)
		{
			validStartX = shipLength - 1;
			validStartY = 0;
		}
		else
		{
			validStartX = 0;
			validStartY = shipLength - 1;
		}
	}

	private void MarkInvalidTiles()
	{
		Vector2Int coor;
		for (int y = 0; y < BOARD_SIZE; y++)
		{
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				coor = new Vector2Int(x, y);
				// At this point we already know the orientation/direction of the ship and its length
				// if we're in the invalid area where the ship can't be placed anyway, we skip it,
				// unless the current tile contains a ship. Then we must calculate the valid tiles from this tile
				if ((y < validStartY || x < validStartX) && validPlacementTiles[coor] == GameManager.TileState.EMPTY)
				{
					// marking any invalid tile that is not a Ship as a Miss. This is so we don't lose
					// the information of where ships are in later iterations
					validPlacementTiles[coor] = GameManager.TileState.MISS;
					continue;
				}

				// Finding a ship tile means we have to account for possible overlap with that ship
				if (validPlacementTiles[coor] == GameManager.TileState.SHIP)
				{
					// We need to see what the next tile over is. If it's a ship, we skip it
					// since we'll calculate for that ship tile later on anyway.
					// If it's a Miss tile, we don't have to do anything.
					Vector2Int nextCoor = isShipHorizontal ?
						new Vector2Int(x + 1, y) : new Vector2Int(x, y + 1);

					if (validPlacementTiles.ContainsKey(nextCoor) == false ||
						validPlacementTiles[nextCoor] == GameManager.TileState.SHIP)
					{
						continue;
					}
					else if (validPlacementTiles[nextCoor] == GameManager.TileState.EMPTY)
					{
						// The next tile over is blank, which means this ship can go there, BUT
						// if we do, this ship will overlap with another ship, so we iterate over
						// the length of our current ship and mark all those tiles as invalid so we
						// dont place it there later. Loop breaks if we encounter OOB or a non-empty spot
						for (int newShipSpot = 0; newShipSpot < shipLength - 1; newShipSpot++)
						{
							if (validPlacementTiles.ContainsKey(nextCoor) == false ||
								validPlacementTiles[nextCoor] != GameManager.TileState.EMPTY)
							{
								break;
							}
							else
							{
								validPlacementTiles[nextCoor] = GameManager.TileState.MISS;
								PrintTiles(validPlacementTiles);
								if (isShipHorizontal)
								{
									nextCoor.x++;
								}
								else
								{
									nextCoor.y++;
								}
							}
						}
					}
				}
			}
		}
	}

	private KeyValuePair<Vector2Int, GameManager.TileState> SelectRandomValidTile()
	{
		// Now that we marked all the invalid tiles, we can safely remove them all here.
		// I could have done this in the previous loop, but I would have had to do this to remove the
		// ship tiles anyway, so it was neater to leave it as one line here.
		validPlacementTiles =
				validPlacementTiles.Where(entry => entry.Value == GameManager.TileState.EMPTY)
				.ToDictionary(entry => entry.Key, entry => entry.Value);

		//Debug.Log("Valid Tiles For Length");
		PrintTiles(validPlacementTiles);

		// Pick a random spot from what's left
		return validPlacementTiles.ElementAt(Random.Range(0, validPlacementTiles.Count));
	}

	private void PlaceShip(EnemyShip ship, Vector2Int tileLoc)
	{
		// Place the ship
		for (int i = 0; i < shipLength; i++)
		{
			//Debug.Log(tileLoc);

			aiTilemap[tileLoc] = GameManager.TileState.SHIP;
			GameManager.instance.grid.SetComputerTileToShip(tileLoc);
			ship.BoardLocations[i] = tileLoc;

			if (isShipHorizontal)
			{
				tileLoc.x--;
			}
			else
			{
				tileLoc.y--;
			}
		}
	}
	#endregion #region BoardSetup

	#region Gameplay
	public void TakeTurn()
	{
		switch (difficulty)
		{
			case Difficulty.EASY:
				EasyMode();
				break;
			case Difficulty.MEDIUM:
				MediumMode();
				break;
			case Difficulty.HARD:
				HardMode();
				break;
		}
	}

	// Completely random moves for every attack
	public void EasyMode()
	{
		Dictionary<Vector2Int, GameManager.TileState> validMoves =
				playerTilemap.Where(entry => entry.Value == GameManager.TileState.EMPTY)
				.ToDictionary(entry => entry.Key, entry => entry.Value);

		KeyValuePair<Vector2Int, GameManager.TileState> tile =
					validMoves.ElementAt(Random.Range(0, validMoves.Count));

		GameManager.instance.grid.TryAttackThePlayer(tile.Key, out GameManager.TileState tileState);
		playerTilemap[tile.Key] = tileState;
		PrintTiles(playerTilemap);
	}

	// AI will hunt by targeting every other tile (parity)
	// Once it finds a ship, it will target nearby tiles to destroy the ship
	public void MediumMode()
	{
		if (attackState == AttackState.HUNT)
		{
			// only check empty tiles that are within the current parity.
			// Parity is decided by the shortest length of the living ships
			// For example if Parity = 2, the Destroyer is alive, so we check every other tile
			// if Parity = 4, the Battleship is alive, but not the Destroyer, Sub, or Cruiser, so
			// we check every 4th tile to find the Battleship
			Dictionary<Vector2Int, GameManager.TileState> validMoves =
				playerTilemap
				.Where(entry => entry.Value == GameManager.TileState.EMPTY &&
					(entry.Key.x + entry.Key.y) % parity == 0)
				.ToDictionary(entry => entry.Key, entry => entry.Value);
			//PrintValidTiles(validMoves);

			// Fire at a random tile from those chosen
			KeyValuePair<Vector2Int, GameManager.TileState> tile =
				validMoves.ElementAt(Random.Range(0, validMoves.Count));

			Vector2Int target = tile.Key;
			//Debug.Log("Hunting " + target);
			GameManager.instance.grid.TryAttackThePlayer(target, out GameManager.TileState tileState);
			playerTilemap[tile.Key] = tileState;
			//PrintValidTiles(playerTilemap);

			if (tileState == GameManager.TileState.HIT)
			{
				// Add the 4 directions of the attacked tile to the stack
				// Invalid or duplicate tiles won't be stacked
				targetStack = new Stack<Vector2Int>();
				TryPushTarget(new Vector2Int(target.x + 1, target.y));
				TryPushTarget(new Vector2Int(target.x - 1, target.y));
				TryPushTarget(new Vector2Int(target.x, target.y + 1));
				TryPushTarget(new Vector2Int(target.x, target.y - 1));
				//PrintTargets();

				// If we run out of targets, we go back to Hunting
				attackState = (targetStack.Count > 0) ? AttackState.TARGET : AttackState.HUNT;
			}
		}
		else // Target mode
		{
			Vector2Int target = targetStack.Pop();
			//Debug.Log("Targetting " + target + ": " + playerTilemap[target]);
			//PrintTargets();

			GameManager.instance.grid.TryAttackThePlayer(target, out GameManager.TileState tileState);
			playerTilemap[target] = tileState;
			//PrintValidTiles(playerTilemap);

			if (tileState == GameManager.TileState.HIT)
			{
				// Add more targets onto the stack everytime we score a hit
				TryPushTarget(new Vector2Int(target.x + 1, target.y));
				TryPushTarget(new Vector2Int(target.x - 1, target.y));
				TryPushTarget(new Vector2Int(target.x, target.y + 1));
				TryPushTarget(new Vector2Int(target.x, target.y - 1));
				//PrintTargets();
			}

			// Go back to hunting once we run out of likely targets
			if (targetStack.Count == 0)
			{
				//Debug.Log("No targets remain, HUNTING");
				attackState = AttackState.HUNT;
			}
		}
	}

	public void HardMode()
	{
		if (attackState == AttackState.HUNT)
		{
			// Calculate attack can't go outside of this if/else or it'll keep looping back into
			// target mode everytime
			GameManager.TileState tileState = CalculateAndAttack();

			// Switch to target mode if the last hit was successful
			if (tileState == GameManager.TileState.HIT || tileState == GameManager.TileState.SUNK)
			{
				attackState = AttackState.TARGET;
			}
		}
		else // Target mode
		{
			CalculateAndAttack();
		}
	}

	private GameManager.TileState CalculateAndAttack()
	{
		// Pick the best spot
		Vector2Int bestLoc = CalculateProbalities(attackState);

		GameManager.instance.grid.TryAttackThePlayer(bestLoc, out GameManager.TileState tileState);
		// if the state changed to Sunk, make sure we don't change it back to Hit, or the AI will get confused
		if (playerTilemap[bestLoc] != GameManager.TileState.SUNK)
		{
			playerTilemap[bestLoc] = tileState;
		}
		return tileState;
	}

	private Vector2Int CalculateProbalities(AttackState attackState)
	{
		int highestProb = -1;
		Vector2Int bestLoc = new Vector2Int(-1, -1);
		Ship[] ships = GameManager.PlayerShips;
		probabilities = new int[BOARD_SIZE, BOARD_SIZE];

		// Check to see if ships can be placed on a tile horizontally to the right. If it fits without
		// hitting a Miss or Sunk Ship tile, then those tiles gain 1 weight. We also do this for fitting
		// the ship vertically going downwards. Once it's done, the AI will fire at the highest probabilty
		// location. If the AI lands a hit, then it switches to Target mode; any placement that crosses at
		// least 1 Hit tile will get a heavier weight based on how many Hit tiles it crosses. Once a ship
		// is sunk, it switches back to Hunt mode.
		foreach (Ship ship in ships)
		{
			for (int y = 0; y < BOARD_SIZE; y++)
			{
				for (int x = 0; x < BOARD_SIZE; x++)
				{
					// If the ship can't fit anymore, we can stop and move onto the next tile
					if (x + ship.Length > BOARD_SIZE && y + ship.Length > BOARD_SIZE)
					{
						Debug.Log("Completely Skipping " + x + ", " + y);
						break;
					}

					// Don't bother checking Horiz. if it can't fit anymore
					if (x + ship.Length > BOARD_SIZE)
					{
						Debug.Log("Horiz. Skipping " + x + ", " + y);
					}
					else
					{
						bool shipFits = true;
						int weight = 1;
						// checking every tile the ship needs to fit on
						for (int i = 0; i < ship.Length; i++)
						{
							if (attackState == AttackState.HUNT)
							{
								// stop if it crosses a non-empty tile
								if (playerTilemap[new Vector2Int(x + i, y)] != GameManager.TileState.EMPTY)
								{
									shipFits = false;
									Debug.Log("Ship doesn't fit at " + (x + i) + ", " + y);
									// There's no way a ship could be here, so 0 odds
									probabilities[x + i, y] = 0;
									break;
								}
							}
							else
							{
								// In target mode, we only stop on a Miss or Sunk Ship.
								// Note: Hunt mode never runs into a Hit tile anyway but this also has an
								// added step that Hunt mode does not have.
								if (playerTilemap[new Vector2Int(x + i, y)] == GameManager.TileState.SUNK ||
									playerTilemap[new Vector2Int(x + i, y)] == GameManager.TileState.MISS)
								{
									shipFits = false;
									Debug.Log("Ship doesn't fit at " + (x + i) + ", " + y);
									probabilities[x + i, y] = 0;
									break;
								}
								else if (playerTilemap[new Vector2Int(x + i, y)] == GameManager.TileState.HIT)
								{
									// Every hit tile makes it heavier
									weight += HEAVY_WEIGHT;
								}
							}
						}

						if (shipFits)
						{
							// Iterate through the length again to update the probabilites
							for (int i = 0; i < ship.Length; i++)
							{
								// Double check for Hits
								if (playerTilemap[new Vector2Int(x + i, y)] == GameManager.TileState.HIT)
								{
									probabilities[x + i, y] = 0;
								}
								else
								{
									probabilities[x + i, y] += weight;
									if (probabilities[x + i, y] > highestProb)
									{
										highestProb = probabilities[x + i, y];
										bestLoc.x = x + i;
										bestLoc.y = y;
									}
								}
							}
							//PrintProbabilites();
						}
					}

					// All the same stuff but for Vertical placement
					if (y + ship.Length > BOARD_SIZE)
					{
						Debug.Log("Vert. Skipping " + x + ", " + y);
					}
					else
					{
						bool shipFits = true;
						int weight = 1;
						for (int i = 0; i < ship.Length; i++)
						{
							if (attackState == AttackState.HUNT)
							{
								if (playerTilemap[new Vector2Int(x, y + i)] != GameManager.TileState.EMPTY)
								{
									shipFits = false;
									Debug.Log("Ship doesn't fit at " + x + ", " + (y + i));
									probabilities[x, y + i] = 0;
									break;
								}
							}
							else
							{
								if (playerTilemap[new Vector2Int(x, y + i)] == GameManager.TileState.MISS ||
									playerTilemap[new Vector2Int(x, y + i)] == GameManager.TileState.SUNK)
								{
									shipFits = false;
									Debug.Log("Ship doesn't fit at " + x + ", " + (y + i));
									probabilities[x, y + i] = 0;
									break;
								}
								else if (playerTilemap[new Vector2Int(x, y + i)] == GameManager.TileState.HIT)
								{
									weight += HEAVY_WEIGHT;
								}
							}
						}

						if (shipFits)
						{
							for (int i = 0; i < ship.Length; i++)
							{
								if (playerTilemap[new Vector2Int(x, y + i)] == GameManager.TileState.HIT)
								{
									probabilities[x, y + i] = 0;
								}
								else
								{
									probabilities[x, y + i] += weight;
									if (probabilities[x, y + i] > highestProb)
									{
										highestProb = probabilities[x, y + i];
										bestLoc.x = x;
										bestLoc.y = y + i;
									}
								}
							}
						}
					}
				}
			}
		}

		return bestLoc;
	}

	private void TryPushTarget(Vector2Int target)
	{
		// Must be a valid tile, must be an empty tile, and must not already be in the stack
		if (target.x < BOARD_SIZE && target.x >= 0 &&
			target.y < BOARD_SIZE && target.y >= 0 &&
			playerTilemap[target] == GameManager.TileState.EMPTY &&
			targetStack.Contains(target) == false)
		{
			targetStack.Push(target);
		}
	}


	public void PlayerShipDestroyed(Ship ship)
	{
		if (difficulty == Difficulty.MEDIUM)
		{
			// Adjusts parity based on what ships are destroyed. Parity is the length
			// of the shortest living ship
			parity = 999;
			foreach (Ship playerShip in GameManager.PlayerShips)
			{
				if (playerShip.IsAlive)
				{
					if (playerShip.Length < parity)
					{
						parity = playerShip.Length;
					}
				}
			}
		}
		else if (difficulty == Difficulty.HARD)
		{
			// Update all the previous Hits into Sunk
			foreach (Vector2Int loc in ship.BoardLocations)
			{
				playerTilemap[loc] = GameManager.TileState.SUNK;
			}
			// Back to Hunt mode
			attackState = AttackState.HUNT;
		}
	}

	#endregion Gameplay
	public void PrintTiles(Dictionary<Vector2Int, GameManager.TileState> dict)
	{
		string msg = "\t0\t1\t2\t3\t4\t5\t6\t7\t8\t9\n";

		for (int y = 0; y < BOARD_SIZE; y++)
		{
			msg += y + "\t";
			for (int x = 0; x < BOARD_SIZE; x++)
			{
				Vector2Int key = new Vector2Int(x, y);
				if (dict.ContainsKey(key))
				{
					switch(dict[key])
					{
						case GameManager.TileState.EMPTY:	msg += "O\t";						break;
						case GameManager.TileState.SHIP:	msg += "<color=cyan>S</color>\t";   break;
						case GameManager.TileState.HIT:		msg += "<color=red>X</color>\t";    break;
						case GameManager.TileState.MISS:	msg += "<color=black>X</color>\t";  break;
						case GameManager.TileState.SUNK:	msg += "<color=red>Q</color>\t";	break;
					}
				}
				else
				{
					msg += "<color=black>X</color>\t";
				}
			}
			msg += "\n";
		}

		Debug.Log(msg);
	}

	//private void PrintProbabilites()
	//{
	//	string msg = "Probabilities:\n";
	//	msg += "\t0\t1\t2\t3\t4\t5\t6\t7\t8\t9\n";
	//	for (int y = 0; y < BOARD_SIZE; y++)
	//	{
	//		msg += y + "\t";
	//		for (int x = 0; x < BOARD_SIZE; x++)
	//		{
	//			msg += probabilities[x, y] + "\t";
	//		}
	//		msg += "\n";
	//	}
	//	Debug.Log(msg);
	//}

	//private void UpdateDebugTexture()
	//{
	//	for (int y = 0; y < BOARD_SIZE; y++)
	//	{
	//		for (int x = 0; x < BOARD_SIZE; x++)
	//		{
	//			float grey = 1 - probabilities[x, y] / (float)bestProb;
	//			test.SetPixel(x, y, new Color(grey, grey, grey));
	//		}
	//	}
	//	test.Apply();
	//}
}
