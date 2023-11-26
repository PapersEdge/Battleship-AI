using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShip : Ship
{
	public static bool AllowSelection = false;
	private bool isSelected = false;
	private bool hasBeenSelected = false;

	public Orientation orientation = Orientation.LEFT;

	protected override void Start()
	{
		base.Start();
		orientation = Orientation.RIGHT;
	}

	void Update()
	{
		if (isSelected)
		{
			Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			pos.z = 0;
			SetPosition(pos);

			if (Input.mouseScrollDelta.y > 0)
			{
				Rotate(90);
			}
			else if (Input.mouseScrollDelta.y < 0)
			{
				Rotate(-90);
			}
			else if (Input.GetMouseButtonDown(1))
			{
				Rotate(90);
			}
		}
	}

	public void OnMouseDown()
	{
		if (AllowSelection && hasBeenSelected == false)
		{
			GameManager.instance.grid.SelectShip(this);
			GameManager.instance.ShipWasSelected();
			hasBeenSelected = true;
			isSelected = true;
		}
	}

	public void PlaceShip(Vector3Int startCoor)
	{
		transform.position = new Vector3(startCoor.x + 0.5f, startCoor.y + 0.5f, 0);
		isSelected = false;
	}

	public void SetPosition(Vector3 newPos)
	{
		transform.position = newPos;
	}
	public void Rotate(float angle)
	{
		transform.Rotate(0, 0, angle);
		orientation = (Orientation)Mathf.RoundToInt(transform.rotation.eulerAngles.z);
	}
	public float GetRotation()
	{
		return transform.rotation.eulerAngles.z;
	}
}
