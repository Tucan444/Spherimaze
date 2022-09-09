using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphGon : MonoBehaviour
{
    public int layer = 0;
    public bool Static = true;
    public bool isCollider = false;
    public bool isTrigger = false;
    [Range(3, 20)] public int n = 5;
    public Vector2 sphPosition = new Vector2();
    [Range(-180, 180)] public float rotation = 0;
    [Range(0.01f, -0.001f + Mathf.PI*0.5f)] public float scale = 0.1f;
    public Color color =  new Color(0.69f, 0.48f, 0.41f, 1);
    public bool invisible = false;
    public bool empty = false;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public Vector3[] vertices;

    [HideInInspector] public ConvexCollider collider_;

    [HideInInspector] public bool triggered = false;

    SphericalUtilities su = new SphericalUtilities();

    public void GetDefaultSetup() {
        position = su.Spherical2Cartesian(sphPosition);
        vertices = su.GetCirclePoints(sphPosition, scale, n);
        vertices = ProcessVertices(vertices);
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDrawGizmos() {
        if (!empty) {
            Gizmos.color = color * 1.4f;
            su.GizmosDrawPoints(vertices);
            Gizmos.color = color * 1.2f;
            su.GizmosDrawPoints(GetVerticesScaled(0.9f));
            Gizmos.color = color;
            su.GizmosDrawPoints(GetVerticesScaled(0.8f));
        }
    }

    Vector3[] GetVerticesScaled(float s) {
        float d = Mathf.Acos(Vector3.Dot(vertices[0], position));
        d = (d*s) - d;

        Vector3[] scaledVerts = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) {
            Quaternion q = Quaternion.AngleAxis(su.Rad2Deg * d, Vector3.Cross(position, vertices[i]));
            scaledVerts[i] = q * vertices[i];
        }

        return scaledVerts;
    }

    void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(position, scale * 0.2f);
    }

    void OnEnable() {
        SphSpaceManager.sphGons.Add(this);
        if (transform.parent == null) {
            transform.parent = GameObject.Find("___SphericalSpace___").transform;
        }
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDisable() {
        SphSpaceManager.sphGons.Remove(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        collider_ = new ConvexCollider(vertices, color, invisible, empty);
        if (!SphSpaceManager.layers.Contains(layer)) {SphSpaceManager.layers.Add(layer);}
    }

    public void ToggleEmpty() {
        empty = !empty;
        collider_.Update(vertices, color, invisible, empty);
    }

    public void ToggleInvisible() {
        invisible = !invisible;
        collider_.Update(vertices, color, invisible, empty);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Warning() {
        if (Static) {
            Debug.Log("attempting changes on static object, will not take effect");
        }
    }

    Vector3[] ProcessVertices(Vector3[] verts) {
        Quaternion q = Quaternion.AngleAxis(rotation, position);
        for (int i = 0; i < n; i++) {
            verts[i] = q * verts[i];
        }
        return verts;
    }

    public void Rotate(float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, position);
        MoveQ(q);
    }

    public void Move(Vector3 target, float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.Cross(position, target));
        MoveQ(q);
    }

    public void MoveQ(Quaternion q) {
        if (!float.IsNaN(q.x)) {
            position = q * position;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = q * vertices[i];
            }
            collider_.MoveRotate(q);
        }
        Warning();
    }

    public void ChangeColor(Color c) {
        color = c;
        collider_.ChangeColor(color);
        Warning();
    }

    public void Scale(float s) {
        vertices = GetVerticesScaled(s);

        collider_.Update(vertices, color, invisible, empty);
        Warning();
    }

    public void ToggleCollider() {isCollider = !isCollider;}
    public void ToggleTrigger() {isTrigger = !isTrigger;}
}
