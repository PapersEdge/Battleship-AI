using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollingBG : MonoBehaviour
{
	public float duration;
	public Vector3 start;
	public Vector3 end;

	private float time;
    // Start is called before the first frame update
    void Start()
    {
		start = transform.position;
	}

    // Update is called once per frame
    void Update()
    {
		if (time < duration)
		{
			transform.position = Vector3.Lerp(start, end, time / duration);
			time += Time.deltaTime;
		}
		else
		{
			transform.position = start;
			time = 0;
		}
	}
}
