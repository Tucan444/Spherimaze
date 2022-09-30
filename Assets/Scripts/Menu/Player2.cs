using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player2 : MonoBehaviour  // this is custom script not a part of engine
{
    public PointLight pl;
    public SphCircle entry;
    SphericalCamera sc;
    SphGon player;

    SphericalUtilities su = new SphericalUtilities();
    SphSpaceManager ssm;
    // Start is called before the first frame update

    void OnEnable() {
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void Start()
    {
        sc = GetComponent<SphericalCamera>();
        player = GetComponent<SphGon>();
    }

    // Update is called once per frame
    void Update()
    {
        player.Move(sc.position, su.Rad2Deg * su.SphDistance(sc.position, player.position));
        player.Rotate(50 * Time.deltaTime);

        Vector3 mp = sc.GetMousePos();
        if (mp.x != 10) {
            pl.Move(mp, su.Rad2Deg *  su.SphDistance(mp, pl.position));
        }

        if (entry.triggered) {
            SceneManager.LoadScene(1);
        }
    }
}
