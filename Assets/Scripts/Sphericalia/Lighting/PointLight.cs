using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PointLight : MonoBehaviour
{
    public bool linear = true;
    public int layer = 10;
    public Vector2 sphPosition = new Vector2();
    [Range(0, Mathf.PI)] public float radius = 0.05f;

    [HideInInspector] public bool _3D = false; // for non linear lighting

    [HideInInspector] public float boundary = 1; // for linear lighting

    [Range(0, 2)] public float power = 1;
    public Color color = Color.white;
    public bool bakedLighting = false;

    [HideInInspector] public Vector3 position;
    float prevP = 0;

    SphericalUtilities su = new SphericalUtilities();

    void Setup() {
        position = su.Spherical2Cartesian(sphPosition);
    }

    void OnValidate() {Setup();}
    void OnEnable() {Setup();}

    void OnDrawGizmos() {
        if (!linear) {
            for (int i = 0; i < 10; i++) {
                Gizmos.color = color * (1.3f - (i*0.1f));
                su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), power - (i * 0.02f)));
            }
        } else {
            for (int i = 0; i < 10; i++) {
                Gizmos.color = color * (1.3f - (i*0.1f));
                su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), boundary - (i * 0.02f)));
            }
        }
    }

    void OnDrawGizmosSelected() {
        if (!linear) {Gizmos.DrawWireSphere(position, power * 0.2f);} else {Gizmos.DrawWireSphere(position, boundary * 0.2f);}
    }

    public void Move(Vector3 target, float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.Cross(position, target));
        MoveQ(q);
    }

    public void MoveQ(Quaternion q) {
        if (!float.IsNaN(q.x)) {
            position = q * position;
        }
    }

    public void ChangeColor(Color c) {
        color = c;
    }

    public void Toggle() {
        if (power == 0) {
            power = prevP;
        } else {
            prevP = power;
            power = 0;
        }
    }

    public PointLightS GetLinearStruct() {
        PointLightS pl = new PointLightS();
        pl.layer = layer;
        pl.pos = position;
        pl.power = power;
        if (boundary > radius) {
            pl.top = boundary * (power / (boundary - radius));
            pl.slope = pl.top/boundary;
        } else {
            pl.top = pl.power + 10;
            pl.slope = 0.01f;
        }
        pl.color = color;
        return pl;
    }

    public NlPointLightS GetNonLinearStruct() {
        NlPointLightS pl = new NlPointLightS();
        pl.layer = layer;
        pl.pos = position;
        pl.radius = radius;
        pl.power = power;
        pl.color = color;
        if (_3D) { pl.fallout = 2; } else { pl.fallout = 1; }
        return pl;
    }
}

public struct PointLightS {
    public int layer;
    public Vector3 pos;
    public float power;
    public float top;
    public float slope;
    public Color color;
}

public struct NlPointLightS {
    public int layer;
    public int fallout;
    public Vector3 pos;
    public float radius;
    public float power;
    public Color color;
}
