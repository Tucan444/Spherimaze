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
            sc.width = 2.6f;
        } else if (maze.subdivisions == 1) {
            sc.speed = 30;
            sc.width = 1.8f;
        } else if (maze.subdivisions == 2) {
            sc.speed = 20;
            sc.width = .9f;
        } else if (maze.subdivisions == 3) {
            sc.speed = 15;
            sc.width = 0.6f;
        }

        if (maze.subdivisions < 2) {
            sc.projection = SphericalCamera.Projection.Stereographic;
        }
    }
}
