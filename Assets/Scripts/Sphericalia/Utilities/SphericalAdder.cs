using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalAdder : SphericalConverter
{
    // add spherical point as 2 rotations
    public Vector2 AddSpherSpher(Vector2 a, Vector2 b) {
        return Cartesian2Spherical(AddCartSpher(Spherical2Cartesian(a), b));
    }

    // add spherical point as 2 rotations
    public Vector3 AddCartSpher(Vector3 c, Vector2 s) {
        s = CrampSpherical(s);
        Vector2 added = Cartesian2Spherical(c);
        added[0] += s[0];
        Vector3 added_ = Spherical2Cartesian(added);
        float rAngle = s[1] * (180 / Mathf.PI);
        if (s[1] < 0) {
            rAngle *= -1;
        }
        Quaternion rot = Quaternion.AngleAxis(rAngle, Vector3.Cross(Spherical2Cartesian(new Vector2(s[0], 0)), Spherical2Cartesian(s)));
        return rot * added_;
    }

    // substract spherical point as 2 rotations
    public Vector3 SubstractCartSpher(Vector3 c, Vector2 s){
        s = CrampSpherical(s);
        float rAngle = Mathf.Abs(s[1]) * (180 / Mathf.PI);
        Quaternion rot = Quaternion.AngleAxis(-rAngle, Vector3.Cross(Spherical2Cartesian(new Vector2(s[0], 0)), Spherical2Cartesian(s)));

        Vector2 substracted = Cartesian2Spherical(rot * c);
        substracted[0] -= s[0];

        return Spherical2Cartesian(substracted);
    }
    
    // add spherical point as 2 rotations
    public Vector2 AddPolarSpher(Vector2 p, Vector2 s) {
        return Cartesian2Polar(AddCartSpher(Polar2Cartesian(p), s));
    }

    public Vector2 SubstractPolarSpher(Vector2 p, Vector2 s) {
        return Cartesian2Polar(SubstractCartSpher(Polar2Cartesian(p), s));
    }

    // puts angles in between -pi and pi
    public Vector2 CrampSpherical(Vector2 s) {
        for (int i = 0; i < 2; i++) {
            if (s[i] > Mathf.PI) {
                while (s[i] > Mathf.PI) {
                    s[i] -= TAU;
                }
            } else if (s[i] < -Mathf.PI) {
                while (s[i] < -Mathf.PI) {
                    s[i] += TAU;
                }
            }
        }
        return s;
    }





    // Used for R3
    public Vector3 AddCartSpherR3(Vector3 c, Vector2 s) {
        s = CrampSpherical(s);
        Vector3 added = CartesianToSpherical(c);
        added[1] += s[0];
        added = SphericalToCartesian(added);
        float rAngle = s[1] * (180 / Mathf.PI);
        if (s[1] < 0) {
            rAngle *= -1;
        }
        Quaternion rot = Quaternion.AngleAxis(rAngle, Vector3.Cross(Spherical2Cartesian(new Vector2(s[0], 0)), Spherical2Cartesian(s)));
        return rot * added;
    }
}
