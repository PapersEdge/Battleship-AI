using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
	public enum Orientation
	{
		DOWN = 0,
		RIGHT = 90,
		UP = 180,
		LEFT = 270,
	}

	public string ShipClass;
	public int Length;
	public int Health;
	public bool IsAlive => Health > 0;

	public Vector2Int[] BoardLocations;

	//public delegate void ShipDamagedHandler();
	//public ShipDamagedHandler ShipDamaged;

	public delegate void ShipDestroyedHandler(Ship ship);
	public ShipDestroyedHandler ShipDestroyed;

	protected virtual void Start()
	{
		BoardLocations = new Vector2Int[Length];
	}

	public void TakeDamage()
	{
		Health = Mathf.Max(0, Health - 1);
		if(IsAlive == false)
		{
			ShipDestroyed?.Invoke(this);
		}
	}

	public void ResetBoardLocations()
	{
		BoardLocations = new Vector2Int[Length];
	}

	public bool IsLocatedHere(Vector2Int tile)
	{
		foreach(Vector2Int loc in BoardLocations)
		{
			if(loc == tile)
			{
				return true;
			}
		}
		return false;
	}
}
