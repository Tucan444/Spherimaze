using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalConverter
{
    public float Rad2Deg = 180 / Mathf.PI;
    public float Deg2Rad = Mathf.PI / 180;
    public float TAU = 2 * Mathf.PI;
    public float HalfPI = Mathf.PI * 0.5f;

    // converts spherical position to cartesian
    public Vector3 Spherical2Cartesian(Vector2 v){ // working
        Vector3 position = new Vector3();
        
        position[0] = (float)(Math.Cos(v[0]) * Math.Cos(v[1]));
        position[2] = (float)(Math.Sin(v[0]) * Math.Cos(v[1]));
        position[1] = (float)(Math.Sin(v[1]));

        return position;
    }

    // converts spherical to polar
    public Vector2 Spherical2Polar(Vector2 v) {
        return Cartesian2Polar(Spherical2Cartesian(v));
    }

    // converts cartesian to spherical
    public Vector2 Cartesian2Spherical(Vector3 v) {
        Vector2 position = new Vector2(0, 0);

        position[1] = Mathf.Asin(v[1]);
        float cosOfz = Mathf.Cos(v[1]);
        position[0] = Mathf.Atan2(v[2] / cosOfz, v[0] / cosOfz);

        return position;
    }

    // converts catresion to polarSpherical
    public Vector2 Cartesian2Polar(Vector3 v) {
        float rotation = Mathf.Acos(Vector3.Dot(v, Vector3.right));
        Vector3 back = Quaternion.AngleAxis(-90 + rotation * Rad2Deg, Vector3.Cross(v, Vector3.right)) * v;
        float angle = Mathf.Acos(Vector3.Dot(Vector3.up, back));
        if (back[2] < 0) {
            angle *= -1;
        }

        return new Vector2(rotation, angle * Rad2Deg);
    }

    // converts polarSpherical to cartesian
    public Vector3 Polar2Cartesian(Vector2 v) {
        Vector3 dir = Quaternion.AngleAxis(v[1], new Vector3(1, 0, 0)) * new Vector3(0, 1, 0);
        Quaternion q = Quaternion.AngleAxis(v[0] * Rad2Deg, Vector3.Cross(new Vector3(1, 0, 0), dir));
        return q * new Vector3(1, 0, 0);
    }

    // converts polarSpherical to spherical
    public Vector2 Polar2Spherical(Vector2 v) {
        return Cartesian2Polar(Polar2Cartesian(v));
    }





    // Used for R3 not S2
    public Vector3 SphericalToCartesian(Vector3 v){ // working
        Vector3 position = new Vector3();
        
        position[0] = (float)(Math.Cos(v[1]) * Math.Cos(v[2]));
        position[2] = (float)(Math.Sin(v[1]) * Math.Cos(v[2]));
        position[1] = (float)(Math.Sin(v[2]));

        position *= v[0];

        return position;
    }

    // Used for R3 not S2
    public Vector3 CartesianToSpherical(Vector3 v) {
        Vector3 position = new Vector3(0, 0, 0);
        Vector3 v_ = new Vector3(v[0], v[1], v[2]);

        position[0] = v.magnitude;
        v_ /= position[0];
        position[2] = Mathf.Asin(v_[1]);
        float cosOfz = Mathf.Cos(v_[1]);
        position[1] = Mathf.Atan2(v_[2] / cosOfz, v_[0] / cosOfz);

        return position;
    }
}
