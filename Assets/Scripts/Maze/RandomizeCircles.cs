using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RandomizeCircles : MonoBehaviour
{
    void OnEnable() {
        SphCircle[] circles = GetComponentsInChildren<SphCircle>();

        for (int i = 0; i < circles.Length; i++)
        {
            circles[i].radius = Random.Range(.1f, .3f);
            float alpha = Random.Range(.0f, Mathf.PI * 2);
            float beta = Mathf.PI * 0.5f * (Mathf.Max(Random.Range(.0f, 1.0f), Random.Range(.0f, 1.0f)) * Mathf.Sin(Random.Range(.0f, Mathf.PI * 2)));
            circles[i].sphPosition = new Vector3(alpha, beta);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
