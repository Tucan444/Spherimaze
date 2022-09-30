using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Setuper : MonoBehaviour
{
    public Maze maze;
    public SphericalCamera sc;
    
    void OnEnable() {
        maze.subdivisions = MazeScale.scale;
        maze.width = 0.045f + (0.05f * (3 - maze.subdivisions));
        //sc.width = 2.6f / (1+maze.subdivisions);

        if (maze.subdivisions == 0) {
            sc.speed = 50;
            sc.screenSpeed = 1.5f;
            sc.width = 4f;
        } else if (maze.subdivisions == 1) {
            sc.speed = 30;
            sc.screenSpeed = 1.5f;
            sc.width = 2.5f;
        } else if (maze.subdivisions == 2) {
            sc.speed = 20;
            sc.screenSpeed = 2f;
            sc.width = 1.8f;
        } else if (maze.subdivisions == 3) {
            sc.speed = 15;
            sc.screenSpeed = 2.5f;
            sc.width = 1f;
        }

        int pro = Random.Range(0, 3);
        switch (pro) {
            case 0:
                sc.projection = SphericalCamera.Projection.Stereographic;
                break;
            case 1:
                sc.projection = SphericalCamera.Projection.Gnomic;
                break;
            case 2:
                sc.projection = SphericalCamera.Projection.Orthographic;
                sc.width = 3.4f;
                break;
            default:
                break;
        }
    }
}
