using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scaler : MonoBehaviour
{
	private int screenWidth;
	private const float BASE_RES = 1080;
	[SerializeField] private bool keepChecking = true;
	[SerializeField] private Transform objectToScale;
	// tiles * cellsize / scale
	// 26 * 1.94 / 3.5
	// Start is called before the first frame update
	void Start()
    {
		screenWidth = Screen.height;
		float scale = screenWidth / BASE_RES;
		objectToScale.localScale = new Vector3(scale, scale, scale);

		Debug.Log("screenWidth: " + screenWidth);
		StartCoroutine(CheckScreenChange());
    }

    IEnumerator CheckScreenChange()
	{
		while (keepChecking)
		{
			int currScreenWidth = Screen.height;
			Debug.Log("currScreenWidth: " + currScreenWidth);
			if (screenWidth != currScreenWidth)
			{
				screenWidth = currScreenWidth;
				float scale = screenWidth / BASE_RES;
				Debug.Log("scale: " + scale);
				objectToScale.localScale = new Vector3(scale, scale, scale);
			}

			yield return new WaitForSecondsRealtime(0.5f);
		}
	}
}
