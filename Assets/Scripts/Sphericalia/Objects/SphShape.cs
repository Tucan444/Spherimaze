using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphShape : MonoBehaviour
{
    public int layer = 0;
    public bool Static = true;
    public bool isCollider = false;
    public bool isTrigger = false;
    public Vector2 sphPosition = new Vector2();
    public Vector2[] polarVertices = new Vector2[6] {
        new Vector2(1, 0),
        new Vector2(1, 60),
        new Vector2(1, 120),
        new Vector2(1, 180),
        new Vector2(1, 240),
        new Vector2(1, 300)
    };
    [Range(-180.0f, 180.0f)] public float rotation = 0.001f;
    [Range(0.01f, 2)] public float scale = 0.1f;
    public Color color =  new Color(0.69f, 0.48f, 0.41f, 1);
    public bool setToNGon = false;
    public bool invisible = false;
    public bool empty = false;

    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);
    Vector2[] vertsPolarPreprocessed = new Vector2[6];
    Vector2[] vertsPolarProcessed = new Vector2[6];
    [HideInInspector] public Vector3[] vertPos = new Vector3[6];
    float[] angles = new float[6];

    [HideInInspector] public bool isQuad = false;
    [HideInInspector] public UnconvexCollider collider_;
    [HideInInspector] public QuadCollider qcollider;

    [HideInInspector] public bool triggered = false;

    // for shape editor
    [HideInInspector] public float handlesRadius = 0.03f;

    SphericalUtilities su = new SphericalUtilities();

    public void GetDefaultSetup() {
        if (setToNGon) {
            SetToNGon();
        }

        vertsPolarPreprocessed = new Vector2[polarVertices.Length];
        vertsPolarProcessed = new Vector2[polarVertices.Length];
        vertPos = new Vector3[polarVertices.Length];
        angles = new float[polarVertices.Length];

        position = su.Spherical2Cartesian(sphPosition);
        for (int i = 0; i < polarVertices.Length; i++) {
            vertsPolarPreprocessed[i] = new Vector2(polarVertices[i][0] * scale, polarVertices[i][1] + rotation);
            vertsPolarProcessed[i] = su.AddPolarSpher(vertsPolarPreprocessed[i], sphPosition);
            vertPos[i] = su.Polar2Cartesian(vertsPolarProcessed[i]);
        }
        angles = su.GetAngles(vertPos);
    }

    public void SetToNGon() {
        for (int i = 0; i < polarVertices.Length; i++) {
            polarVertices[i] = new Vector2(1, i * (360.0f / (float)polarVertices.Length));
        }
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDrawGizmos() {
        if (!empty) {
            su.GizmosDrawShape(vertPos, angles, 0.1f, scale, color);
        }
    }

    void OnDrawGizmosSelected() {
        float sumD = 0;
        for (int i = 0; i < polarVertices.Length; i++) {
            sumD += vertsPolarPreprocessed[i][0];
        }

        Gizmos.DrawWireSphere(position, Mathf.Abs((sumD / (float)vertsPolarPreprocessed.Length) * 0.2f));
    }

    void OnEnable() {
        SphSpaceManager.sphShapes.Add(this);
        if (transform.parent == null) {
            transform.parent = GameObject.Find("___SphericalSpace___").transform;
        }
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDisable() {
        SphSpaceManager.sphShapes.Remove(this);
    }

    void Start() {
        if (isQuad) {qcollider = new QuadCollider(vertPos, color, invisible, empty);} else {collider_ = new UnconvexCollider(vertPos, color, invisible, empty);}
        if (!SphSpaceManager.layers.Contains(layer)) {SphSpaceManager.layers.Add(layer);}
    }

    public void ToggleEmpty() {
        empty = !empty;
        if (isQuad) {qcollider.Update(vertPos, color, invisible, empty);} else {collider_.Update(vertPos, color, invisible, empty);}
    }

    public void ToggleInvisible() {
        invisible = !invisible;
        if (isQuad) {qcollider.Update(vertPos, color, invisible, empty);} else {collider_.Update(vertPos, color, invisible, empty);}
    }

    void Warning() {
        if (Static) {
            Debug.Log("attempting changes on static object, will not take effect");
        }
    }

    public void Rotate(float angle) {
        Quaternion q = Quaternion.AngleAxis(-angle, position);
        MoveQ(q);
    }

    public void Move(Vector3 target, float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.Cross(position, target));
        MoveQ(q);
    }

    public void MoveQ(Quaternion q) {
        if (!float.IsNaN(q.x)) {
            position = q * position;
            for (int i = 0; i < vertPos.Length; i++)
            {
                vertPos[i] = q * vertPos[i];
            }

            if (!isQuad) {
                collider_.MoveRotate(q);
            } else {qcollider.MoveRotate(q);}
        }
        Warning();
    }

    public void ChangeColor(Color c) {
        color = c;
        if (!isQuad) {
            collider_.ChangeColor(color);
        } else {qcollider.ChangeColor(color);}
        Warning();
    }

    public void Scale(float s) {
        for (int i = 0; i < vertPos.Length; i++) {
            vertPos[i] = su.SphLerp(position, vertPos[i], s);
        }

        if (!isQuad) {
            collider_.Update(vertPos, color, invisible, empty);
        } else {qcollider.Update(vertPos, color, invisible, empty);}
        Warning();
    }

    public void ToggleCollider() {isCollider = !isCollider;}
    public void ToggleTrigger() {isTrigger = !isTrigger;}
}
