using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightMover : MonoBehaviour
{
    [Range(0.0001f, 0.1f)] public float solidity = 0.01f;
    [Range(0.5f, 5)] public float speed = 2;
    PointLight pl;
    Vector2 noisePosx;
    Vector2 noisePosy;
    // Start is called before the first frame update
    void Start()
    {
        pl = GetComponent<PointLight>();
        float rand = Random.Range(.0f, 20000.0f);
        noisePosx = new Vector2(0, rand);
        noisePosy = new Vector2(rand, 0);
    }

    // Update is called once per frame
    void Update()
    {
        noisePosx.x += Time.deltaTime*solidity;
        noisePosy.y += Time.deltaTime*solidity;

        // custom function
        pl.SetPos(pl.sphPosition + speed * Time.deltaTime * new Vector2(Mathf.PerlinNoise(noisePosx.x, noisePosx.y)-0.5f, Mathf.PerlinNoise(noisePosy.x, noisePosy.y)-0.5f));
    }
}
