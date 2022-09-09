using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeScale : MonoBehaviour
{
    public static int scale = 0;
    SphericalCamera sc;
    SphCircle[] circles;
    int hoverID = -1;

    Color defColor = new Color(0.93f, 0.42f, 0.91f);
    Color selectedColor = new Color(0.7f, 0.18f, 0.68f);
    Color hoverColor = new Color(0.76f, 0.2f, 0.75f);
    // Start is called before the first frame update
    void Start()
    {
        sc = GameObject.Find("camera").GetComponent<SphericalCamera>();
        circles = GetComponentsInChildren<SphCircle>();
    }

    // Update is called once per frame
    void Update()
    {
        hoverID = -1;
        Vector3 mp = sc.GetMousePos();

        for (int i = 0; i < circles.Length; i++)
        {
            if (i > scale) {
                circles[i].ChangeColor(defColor);
            } else {
                circles[i].ChangeColor(selectedColor);
            }
            if (circles[i].collider_.CollidePoint(mp)) {
                hoverID = i;
            }
        }

        if (hoverID != -1) {
            for (int i = 0; i <= hoverID; i++) {
                circles[i].ChangeColor(hoverColor);
            }
        }
    }

    public void Click() {
        if (hoverID != -1) {
            scale = hoverID;
        }
    }
}
