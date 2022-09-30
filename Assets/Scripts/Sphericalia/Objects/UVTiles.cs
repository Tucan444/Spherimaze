using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UVTiles : MonoBehaviour
{
    public int layer = 0;
    public bool Static = true;
    public bool isCollider = false;
    public bool isTrigger = false;
    public Vector2 sphPosition = new Vector2();
    [Range(-180.0f, 180.0f)] public float rotation = 0.001f;
    public Color color =  new Color(0.69f, 0.48f, 0.41f, 1);
    public Texture2D tiles;
    [Range(1, 5)] public int optimizeKernelSize = 2;
    [Range(0, 1)] public float optimizeScope = 0.6f;
    public bool invisible = false;
    public bool empty = false;

    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);

    float tau = Mathf.PI * 2;

    Vector2[] sphCorners;

    SphShape[] shapes;

    SphericalUtilities su = new SphericalUtilities();

    public void GetDefaultSetup() {
        position = su.Spherical2Cartesian(sphPosition);
        if (rotation == 0) {rotation += 0.001f;}
        
        DestroyChildren();

        if (tiles) {
            Quaternion q = Quaternion.AngleAxis(rotation + 0.001f, Vector3.right);
            Vector2 tileInc = new Vector2(tau / (float)tiles.width, (Mathf.PI - 0.002f) / (float)tiles.height);
            Color[] pix = tiles.GetPixels();
            int counter = 0;

            if (optimizeScope == 0 || optimizeKernelSize == 1) {
                counter = CreateTiles(q, tileInc, pix, counter, new Vector2(0, tiles.height), 1);
            } else if (optimizeScope == 1) {
                counter = CreateTiles(q, tileInc, pix, counter, new Vector2(0, tiles.height), optimizeKernelSize);
            } else {
                float optPass = optimizeScope * 0.5f * (float)tiles.height;
                optPass -= optPass % (float)optimizeKernelSize;
                float secondEnd = (float)tiles.height - optPass;
                counter = CreateTiles(q, tileInc, pix, counter, new Vector2(0, optPass), optimizeKernelSize, 1);
                counter = CreateTiles(q, tileInc, pix, counter, new Vector2(optPass, secondEnd), 1);
                counter = CreateTiles(q, tileInc, pix, counter, new Vector2(secondEnd, tiles.height), optimizeKernelSize, 2);
            }
        }
    }

    int CreateTiles(Quaternion q, Vector2 tileInc, Color[] pix, int counter, Vector2 range_, int k, int indi=0) {
        // created uneffected tiles
        for (int i = (int)range_.x; i < (int)range_.y; i+=k) {
            for (int j = 0; j < tiles.width; j+=k)
            {
                int overflow = Mathf.Max(0, (j+k) - tiles.width);
                int kk = k - overflow;
                if (Decide(counter, pix, k, overflow)) {
                    Vector2[] corners = GetCorners(tileInc, i, j, k);
                    CreateTile(GetCenter(corners, q, k), corners, q, i, j);

                    if (kk > 1) {
                        if (indi==1 && i == (int)range_.y - k) { // k > 1
                            Vector3[] points = new Vector3[kk+1];
                            for (int ii = 0; ii < kk+1; ii++) {
                                Vector2 vs = new Vector2(tileInc.x * (j + ii), tileInc.y * (i+k)) - new Vector2(Mathf.PI, ((0.5f * Mathf.PI) - 0.001f));
                                points[ii] = su.Spherical2Cartesian(vs);
                            }
                            CreateFiller(points, q, i, j);
                        } else if (indi==2 && i == (int)range_.x) { // k > 1
                            Vector3[] points = new Vector3[kk+1];
                            for (int ii = 0; ii < kk+1; ii++) {
                                Vector2 vs = new Vector2(tileInc.x * (j + ii), tileInc.y * i) - new Vector2(Mathf.PI, ((0.5f * Mathf.PI) - 0.001f));
                                points[ii] = su.Spherical2Cartesian(vs);
                            }
                            CreateFiller(points, q, i, j);
                        }
                    }
                }
                counter+= k-overflow;
            }
            counter += (k-1) * tiles.width; 
        }
        return counter;
    }

    void CreateFiller(Vector3[] points, Quaternion q, int i, int j) {
        Vector2 center = GetCenterOfPoints(points);
        center = su.Cartesian2Spherical(q * su.Spherical2Cartesian(center));
        center = su.AddSpherSpher(center, sphPosition);

        // convert to polar + rotate + reorigin + rescale
        Vector2[] polarCorners = new Vector2[points.Length];
        for (int ii = 0; ii < points.Length; ii++)
        {
            polarCorners[ii] = su.Cartesian2Polar(q * points[ii]); // rotation and convertion
            polarCorners[ii] = su.AddPolarSpher(polarCorners[ii], sphPosition);
            polarCorners[ii] = su.SubstractPolarSpher(polarCorners[ii], center); // reorigin
            polarCorners[ii][0] *= 10;
        }

        GameObject child = new GameObject("filler "+ (i + (j * tiles.width)).ToString());
        SphShape ss = child.AddComponent(typeof(SphShape)) as SphShape;
        #if UNITY_EDITOR
        Undo.RecordObject(ss, "Created tile");
        #endif
        ss.layer = layer;
        ss.Static = Static;
        ss.isCollider = isCollider;
        ss.isTrigger = isTrigger;
        ss.sphPosition = center;
        ss.color = color;
        ss.invisible = invisible;
        ss.empty = empty;
        ss.polarVertices = new Vector2[polarCorners.Length];
        for (int ii = 0; ii < polarCorners.Length; ii++) {
            ss.polarVertices[ii] = polarCorners[ii];
        }
        if (polarCorners.Length == 4) {ss.isQuad = true;}
        ss.GetDefaultSetup();
        child.transform.parent = this.gameObject.transform;
    }

    bool Decide(int counter, Color[] pix, int k, int overflow) {
        float together = 0;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k-overflow; j++)
            {
                int index = counter + j + (i * tiles.width);
                if (index < pix.Length) {together += pix[index][0];}
            }
        }
        return (together / (float)(k*k)) < 0.6f;
    }

    Vector3[] shortenArray(Vector3[] arr_, int i) {
        Vector3[] arr = new Vector3[i];
        for (int j = 0; j < i; j++) {
            arr[j] = arr_[j];
        }
        return arr;
    }

    Vector2 GetCenter(Vector2[] corners, Quaternion q, int k=1) {
        Vector2 center = new Vector2();
        for (int i = 0; i < 4; i++)
        {
            center += corners[i];
        }
        center *= 0.25f;
        center = su.Cartesian2Spherical(q * su.Spherical2Cartesian(center));
        center = su.AddSpherSpher(center, sphPosition);
        return center;
    }

    Vector2 GetCenterOfPoints(Vector3[] points) {
        Vector3 sum = new Vector3();
        for (int i = 0; i < points.Length; i++) {
            sum += points[i];
        }
        return su.Cartesian2Spherical(sum.normalized);
    }

    Vector2[] GetCorners(Vector2 tileInc, int i, int j, int k=1) {
        // get corner points
        sphCorners = new Vector2[] {
            new Vector2(tileInc.x * j, tileInc.y * i),
            new Vector2(tileInc.x * j, tileInc.y * (i+k)),
            new Vector2(tileInc.x * (j+k), tileInc.y * (i+k)),
            new Vector2(tileInc.x * (j+k), tileInc.y * i)
        };
        for (int ii = 0; ii < 4; ii++) {
            sphCorners[ii] -= new Vector2(Mathf.PI, ((0.5f * Mathf.PI) - 0.001f));
            sphCorners[ii][0] = Mathf.Min(sphCorners[ii][0], Mathf.PI);
        }
        return sphCorners;
    }

    void CreateTile(Vector2 center, Vector2[] sphCorners, Quaternion q, int i, int j) {
        // convert to polar + rotate + reorigin + rescale
        Vector2[] polarCorners = new Vector2[4];
        for (int ii = 0; ii < 4; ii++)
        {
            polarCorners[ii] = su.Cartesian2Polar(q * su.Spherical2Cartesian(sphCorners[ii])); // rotation and convertion
            polarCorners[ii] = su.AddPolarSpher(polarCorners[ii], sphPosition);
            polarCorners[ii] = su.SubstractPolarSpher(polarCorners[ii], center); // reorigin
            polarCorners[ii][0] *= 10;
        }

        GameObject child = new GameObject((i + (j * tiles.width)).ToString());
        SphShape ss = child.AddComponent(typeof(SphShape)) as SphShape;
        #if UNITY_EDITOR
        Undo.RecordObject(ss, "Created tile");
        #endif
        ss.layer = layer;
        ss.Static = Static;
        ss.isCollider = isCollider;
        ss.isTrigger = isTrigger;
        ss.sphPosition = center;
        ss.color = color;
        ss.invisible = invisible;
        ss.empty = empty;
        ss.polarVertices = new Vector2[4] {polarCorners[0], polarCorners[1], polarCorners[2], polarCorners[3]};
        ss.GetDefaultSetup();
        ss.isQuad = true;
        child.transform.parent = this.gameObject.transform;
    }
    
    // other methods
    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }

    public void OnEnable() {
        if (transform.parent == null) {
            transform.parent = GameObject.Find("___SphericalSpace___").transform;
        }
        GetDefaultSetup();
        transform.position = position;
    }

    void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(position, 0.2f);
    }

    void DestroyChildren() {
        var allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren) { //var child : Transform in allChildren.
            if (child != transform) {
                StartCoroutine(Destroy(child.gameObject));
                if (Application.isPlaying) {
                    SphShape ss = child.gameObject.GetComponent<SphShape>();
                    ss.position.x = 10;
                    Destroy(child.gameObject.GetComponent<SphShape>());
                }
            }
        }
    }

    IEnumerator Destroy(GameObject go)
     {
        yield return null;
        DestroyImmediate(go);
     }

    void Start() {
        SphShape[] shapes_ = GetComponentsInChildren<SphShape>();
        int valid = 0;
        for (int i = 0; i < shapes_.Length; i++) {
            if (shapes_[i].position.x != 10) { valid++;}
        }
        shapes = new SphShape[valid];

        int j = 0;
        for (int i = 0; i < shapes_.Length; i++) {
            if (shapes_[i].position.x != 10) { shapes[j] = shapes_[i]; j++;}
        }
    }

    public void ToggleEmpty() 
    {
        empty = !empty;
        for (int i = 0; i < shapes.Length; i++) {
            shapes[i].ToggleEmpty();
        }
    }

    public void ToggleInvisible() 
    {
        invisible = !invisible;
        for (int i = 0; i < shapes.Length; i++) {
            shapes[i].ToggleInvisible();
        }
    }

    void Warning() {
        if (Static) {
            Debug.Log("attempting changes on static object, will not take effect");
        }
    }

    void Update() {
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
            for (int i = 0; i < shapes.Length; i++)
            {
                shapes[i].MoveQ(q);
            }
        }
        Warning();
    }

    public void ChangeColor(Color c) {
        color = c;
        
        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i].ChangeColor(color);
        }
    }

    public void Scale(float s) {
        for (int i = 0; i < shapes.Length; i++){ shapes[i].Scale(s); }
    }

    public void ToggleCollider() {
        for (int i = 0; i < shapes.Length; i++){ shapes[i].ToggleCollider(); }
    }
    public void ToggleTrigger() {
        for (int i = 0; i < shapes.Length; i++){ shapes[i].ToggleTrigger(); }
    }
}
