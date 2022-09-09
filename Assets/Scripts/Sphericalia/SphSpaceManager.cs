using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// main class
public class SphSpaceManager : MonoBehaviour
{
    public static SphBg sb;
    public static SphericalCamera sc;
    public static Lighting lighting;
    public static List<SphCircle> sphCircles = new List<SphCircle>();
    public static List<SphGon> sphGons = new List<SphGon>();
    public static List<SphShape> sphShapes = new List<SphShape>();
    public static List<int> layers = new List<int>();

    // getting triggers
    List<SphCircle> cTrigger = new List<SphCircle>();
    List<SphGon> gTrigger = new List<SphGon>();
    List<SphShape> sTrigger = new List<SphShape>();

    // getting colliders
    List<SphCircle> circleC = new List<SphCircle>();
    List<SphGon> gonC = new List<SphGon>();
    List<SphShape> shapeC = new List<SphShape>();
    
    // sorting by static
    List<SphCircle> sphCirclesS = new List<SphCircle>();
    List<SphGon> sphGonsS = new List<SphGon>();
    List<SphShape> sphShapesS = new List<SphShape>();

    // arrays for layer ordering
    Vector3[] layerSplits;
    Vector3[] staticPrimitivesSplits;

    int[] staticSplit = new int[3];
    int[] primitivesCount = new int[3];
    CircleS[] circles;
    TriangleS[] triangles;
    QuadS[] quads;

    public ComputeShader baseShader;
    public ComputeShader realtimeLightingShader;
    public ComputeShader mixedLightingShader;
    private RenderTexture renderTexture;

    Texture2D black;

    float tau = Mathf.PI * 2;

    EmptyObjects eo = new EmptyObjects();

    // properties
    int ambientLightID = Shader.PropertyToID("ambientLight");
    int gammaID = Shader.PropertyToID("gamma");

    int lightLayersID = Shader.PropertyToID("lightLayers");
    int lightmapID = Shader.PropertyToID("lightmaps");
    int lightmapStepID = Shader.PropertyToID("lightmapStep");
    int lightmapsDepthID = Shader.PropertyToID("lightmapsDepth");

    int lightsID = Shader.PropertyToID("lights");
    int lLengthID = Shader.PropertyToID("lLength");
    int nlLightsID = Shader.PropertyToID("nlLights");
    int nllLengthID = Shader.PropertyToID("nllLength");

    int circlesID = Shader.PropertyToID("circles");
    int trianglesID = Shader.PropertyToID("triangles");
    int quadsID = Shader.PropertyToID("quads");

    int layerNumsID = Shader.PropertyToID("layerNums");
    int layersID = Shader.PropertyToID("layers");
    int layLengthID = Shader.PropertyToID("layLength");

    int resultID = Shader.PropertyToID("Result");
    int bgColorID = Shader.PropertyToID("bgColor");
    int orthoBgID = Shader.PropertyToID("orthoBg");
    int useBgTextureID = Shader.PropertyToID("useBgTexture");
    int bgTextureID = Shader.PropertyToID("bgTexture");
    int bgStepID = Shader.PropertyToID("bgStep");

    int screenQID = Shader.PropertyToID("screenQ");
    int sendRaysID = Shader.PropertyToID("rays");
    int resolutionID = Shader.PropertyToID("resolution");

    // other

    #if UNITY_EDITOR
    void OnEnable() {
        Tools.hidden = true;
    }
    void OnDisable() {
        Tools.hidden = false;
    }
    #endif
    
    // Start is called before the first frame update
    void Start()
    {
        layers.Sort();

        renderTexture = new RenderTexture(sc.resolution[0], sc.resolution[1], 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        black = Texture2D.blackTexture;

        GetPrimitivesCount();
        SortObjects();
        GetLayerVectors();

        /* Debug.Log(layers.Count);
        Debug.Log("layer vectors for cricles");
        for (int i = 0; i < layerSplits.Length; i++)
        {
            Debug.Log("i: " + i + " split: "+layerSplits[i]);
        }

        Debug.Log("static primitives splits");
        for (int i = 0; i < layerSplits.Length-1; i++)
        {
            Debug.Log("i: " + i + " split: "+staticPrimitivesSplits[i]);
        } */

        // setting circles
        circles = new CircleS[primitivesCount[0]];
        triangles = new TriangleS[primitivesCount[1]];
        quads = new QuadS[primitivesCount[2]];

        PopulateAll();
    }

    void GetPrimitivesCount() {
        primitivesCount[0] = sphCircles.Count;
        
        int tcount = 0;
        int qcount = 0;
        for (int i = 0; i < sphGons.Count; i++)
        {
            tcount += sphGons[i].collider_.triangles.Length;
            qcount += sphGons[i].collider_.quads.Length;
        }
        for (int i = 0; i < sphShapes.Count; i++) {
            if (sphShapes[i].isQuad) {
                qcount++;
            } else {
                tcount += sphShapes[i].collider_.triangles.Length;
            }
        }

        primitivesCount[1] = tcount;
        primitivesCount[2] = qcount;
    }

    void SortObjects() {
        SortColliderTrigger();

        // copying lists
        // circles
        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].Static) {
                staticSplit[0]++;
            }
            sphCirclesS.Add(sphCircles[j]);
        }
        // ngons
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].Static) {
                staticSplit[1]++;
            }
            sphGonsS.Add(sphGons[j]);
        }
        // shapes
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].Static) {
                staticSplit[2]++;
            }
            sphShapesS.Add(sphShapes[j]);
        }

        // sorting lists
        Circle0Comparer o0c = new Circle0Comparer();
        Gon0Comparer g0c = new Gon0Comparer();
        Shape0Comparer s0c = new Shape0Comparer();
        Circle1Comparer o1c = new Circle1Comparer();
        Gon1Comparer g1c = new Gon1Comparer();
        Shape1Comparer s1c = new Shape1Comparer();
        sphCirclesS.Sort(o0c);
        sphGonsS.Sort(g0c);
        sphShapesS.Sort(s0c);
        sphCircles.Sort(o1c);
        sphGons.Sort(g1c);
        sphShapes.Sort(s1c);
    }

    void GetLayerVectors() {
        int[] ti = new int[3];
        bool prevStatic = true;

        layerSplits = new Vector3[layers.Count+1];
        staticPrimitivesSplits = new Vector3[layers.Count];

        for (int i = 0; i < staticPrimitivesSplits.Length; i++)
        {
            staticPrimitivesSplits[i] = new Vector3(-1, -1, -1);
        }

        for (int i = 0; i < layers.Count; i++)
        {
            // circles
            while (ti[0] < sphCirclesS.Count && sphCirclesS[ti[0]].layer == layers[i]) {
                layerSplits[i+1][0]++;

                if (prevStatic && !sphCirclesS[ti[0]].Static) {staticPrimitivesSplits[i][0] = layerSplits[i+1][0]-1;}

                prevStatic = sphCirclesS[ti[0]].Static;
                ti[0]++;
            }

            int[] tqc = new int[4];
            // gons
            while (ti[1] < sphGonsS.Count && sphGonsS[ti[1]].layer == layers[i]) {
                if (sphGonsS[ti[1]].Static) {
                    tqc[0] += sphGonsS[ti[1]].collider_.triangles.Length;
                    tqc[1] += sphGonsS[ti[1]].collider_.quads.Length;
                } else {
                    tqc[2] += sphGonsS[ti[1]].collider_.triangles.Length;
                    tqc[3] += sphGonsS[ti[1]].collider_.quads.Length;
                }
                ti[1]++;
            }

            // shapes
            while (ti[2] < sphShapesS.Count && sphShapesS[ti[2]].layer == layers[i]) {
                if (sphShapesS[ti[2]].Static) {
                    if (sphShapesS[ti[2]].isQuad) { tqc[1]++; } 
                    else { tqc[0] += sphShapesS[ti[2]].collider_.triangles.Length; }
                } else {
                    if (sphShapesS[ti[2]].isQuad) { tqc[3]++; } 
                    else { tqc[2] += sphShapesS[ti[2]].collider_.triangles.Length; }
                }
                ti[2]++;
            }

            layerSplits[i+1][1] = tqc[0] + tqc[2];
            layerSplits[i+1][2] = tqc[1] + tqc[3];
            staticPrimitivesSplits[i][1] = tqc[0];
            staticPrimitivesSplits[i][2] = tqc[1];
        }

        for (int i = 1; i < layerSplits.Length; i++) {
            layerSplits[i] = layerSplits[i] + layerSplits[i-1];
        }
        for (int i = 0; i < staticPrimitivesSplits.Length; i++) {
             for (int j = 0; j < 3; j++)
            {
                if (staticPrimitivesSplits[i][j] == -1) {staticPrimitivesSplits[i][j] = layerSplits[i+1][j];}
                else {staticPrimitivesSplits[i][j] = layerSplits[i][j] + staticPrimitivesSplits[i][j];}
            }
        }
    }

    void UpdateNonStatic() {
        // updating non-static objects
        int[] ti = new int[3] {staticSplit[0], staticSplit[1], staticSplit[2]};
        /* Debug.Log("static split for circles: " + staticSplit[0]);
        Debug.Log("static split for gons: " + staticSplit[1]);
        Debug.Log("static split for shapes: " + staticSplit[2]); */
        for (int i = 0; i < layers.Count; i++)
        {
            Vector3 v = staticPrimitivesSplits[i];
            // circles
            while (ti[0] < sphCircles.Count && sphCircles[ti[0]].layer == layers[i]) {
                circles[(int)v.x] = sphCircles[ti[0]].collider_.circleS;
                
                v.x++;
                ti[0]++;
            }

            // gons
            while (ti[1] < sphGons.Count && sphGons[ti[1]].layer == layers[i]) {
                for (int j = 0; j < sphGons[ti[1]].collider_.triangles.Length; j++)
                {
                    triangles[(int)v.y] = sphGons[ti[1]].collider_.triangles[j];
                    v.y++;
                }
                for (int j = 0; j < sphGons[ti[1]].collider_.quads.Length; j++)
                {
                    quads[(int)v.z] = sphGons[ti[1]].collider_.quads[j];
                    v.z++;
                }

                ti[1]++;
            }

            // shapes
            while (ti[2] < sphShapes.Count && sphShapes[ti[2]].layer == layers[i]) {
                if (sphShapes[ti[2]].isQuad) {
                    quads[(int)v.z] = sphShapes[ti[2]].qcollider.q;
                    v.z++;
                } 
                else {
                    for (int j = 0; j < sphShapes[ti[2]].collider_.triangles.Length; j++)
                    {
                        triangles[(int)v.y] = sphShapes[ti[2]].collider_.triangles[j];
                        v.y++;
                    }
                }

                ti[2]++;
            }
        }
    }

    // functions to use outside of class

    // used for lightning (dont use ingame, used for baking lighting in editor)
    public List<SphCircle> GetStaticCircles() {
        List<SphCircle> staticCircles = new List<SphCircle>();
        for (int i = 0; i < sphCircles.Count; i++)
        {
            if (sphCircles[i].Static) {
                staticCircles.Add(sphCircles[i]);
            }
        }
        return staticCircles;
    }

    public List<SphGon> GetStaticGons() {
        List<SphGon> staticGons = new List<SphGon>();
        for (int i = 0; i < sphGons.Count; i++)
        {
            if (sphGons[i].Static) {
                staticGons.Add(sphGons[i]);
            }
        }
        return staticGons;
    }

    public List<SphShape> GetStaticShapes() {
        List<SphShape> staticShapes = new List<SphShape>();
        for (int i = 0; i < sphShapes.Count; i++)
        {
            if (sphShapes[i].Static) {
                staticShapes.Add(sphShapes[i]);
            }
        }
        return staticShapes;
    }
    
    // use 4 functions below after making static empty objects non empty
    public void PopulateAll() { 
        PopulateCircles();

        PopulateTrianglesAndQuads();  
    }

    public void PopulateCircles() {
        for (int i = 0; i < sphCirclesS.Count; i++) {
            circles[i] = sphCirclesS[i].collider_.circleS;
        }
    }

    public void PopulateTrianglesAndQuads() {
        int processedT = 0;
        int processedQ = 0;

        // updating non-static objects
        int[] ti = new int[2] {0, 0};
        for (int i = 0; i < layers.Count; i++)
        {
            // static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i] && sphGonsS[ti[0]].Static) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.triangles.Length; jj++)
                {
                    triangles[processedT] = sphGonsS[ti[0]].collider_.triangles[jj];
                    processedT++;
                }
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.quads.Length; jj++)
                {
                    quads[processedQ] = sphGonsS[ti[0]].collider_.quads[jj];
                    processedQ++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i] && sphShapesS[ti[1]].Static) {
                if (sphShapesS[ti[1]].isQuad) {
                    quads[processedQ] = sphShapesS[ti[1]].qcollider.q;
                    processedQ++;
                } else {
                    for (int jj = 0; jj < sphShapesS[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles[processedT] = sphShapesS[ti[1]].collider_.triangles[jj];
                        processedT++;
                    }
                }

                ti[1]++;
            }

            // non static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i]) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.triangles.Length; jj++)
                {
                    triangles[processedT] = sphGonsS[ti[0]].collider_.triangles[jj];
                    processedT++;
                }
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.quads.Length; jj++)
                {
                    quads[processedQ] = sphGonsS[ti[0]].collider_.quads[jj];
                    processedQ++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i]) {
                if (sphShapesS[ti[1]].isQuad) {
                    quads[processedQ] = sphShapesS[ti[1]].qcollider.q;
                    processedQ++;
                } else {
                    for (int jj = 0; jj < sphShapesS[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles[processedT] = sphShapesS[ti[1]].collider_.triangles[jj];
                        processedT++;
                    }
                }

                ti[1]++;
            }
        }
    }

    public void PopulateTriangles() {
        int processedT = 0;

        // updating non-static objects
        int[] ti = new int[2] {0, 0};
        for (int i = 0; i < layers.Count; i++)
        {
            // static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i] && sphGonsS[ti[0]].Static) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.triangles.Length; jj++)
                {
                    triangles[processedT] = sphGonsS[ti[0]].collider_.triangles[jj];
                    processedT++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i] && sphShapesS[ti[1]].Static) {
                if (!sphShapesS[ti[1]].isQuad) {
                    for (int jj = 0; jj < sphShapesS[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles[processedT] = sphShapesS[ti[1]].collider_.triangles[jj];
                        processedT++;
                    }
                }

                ti[1]++;
            }

            // non static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i]) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.triangles.Length; jj++)
                {
                    triangles[processedT] = sphGonsS[ti[0]].collider_.triangles[jj];
                    processedT++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i]) {
                if (!sphShapesS[ti[1]].isQuad) {
                    for (int jj = 0; jj < sphShapesS[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles[processedT] = sphShapesS[ti[1]].collider_.triangles[jj];
                        processedT++;
                    }
                }

                ti[1]++;
            }
        }
    }

    public void PopulateQuads() {
        int processedQ = 0;

        // updating non-static objects
        int[] ti = new int[2] {0, 0};
        for (int i = 0; i < layers.Count; i++)
        {
            // static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i] && sphGonsS[ti[0]].Static) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.quads.Length; jj++)
                {
                    quads[processedQ] = sphGonsS[ti[0]].collider_.quads[jj];
                    processedQ++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i] && sphShapesS[ti[1]].Static) {
                if (sphShapesS[ti[1]].isQuad) {
                    quads[processedQ] = sphShapesS[ti[1]].qcollider.q;
                    processedQ++;
                }

                ti[1]++;
            }

            // non static

            // gons
            while (ti[0] < sphGonsS.Count && sphGonsS[ti[0]].layer == layers[i]) {
                for (int jj = 0; jj < sphGonsS[ti[0]].collider_.quads.Length; jj++)
                {
                    quads[processedQ] = sphGonsS[ti[0]].collider_.quads[jj];
                    processedQ++;
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapesS.Count && sphShapesS[ti[1]].layer == layers[i]) {
                if (sphShapesS[ti[1]].isQuad) {
                    quads[processedQ] = sphShapesS[ti[1]].qcollider.q;
                    processedQ++;
                }

                ti[1]++;
            }
        }
    }

    // use when object changed from collider to non collider or trigger to non trigger
    public void SortColliderTrigger() {
        cTrigger = new List<SphCircle>();
        gTrigger = new List<SphGon>();
        sTrigger = new List<SphShape>();

        circleC = new List<SphCircle>();
        gonC = new List<SphGon>();
        shapeC = new List<SphShape>();

        // circles
        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].isCollider) {
                circleC.Add(sphCircles[j]);
            }
            if (sphCircles[j].isTrigger) {
                cTrigger.Add(sphCircles[j]);
            }
        }

        // ngons
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].isCollider) {
                gonC.Add(sphGons[j]);
            }
            if (sphGons[j].isTrigger) {
                gTrigger.Add(sphGons[j]);
            }
        }

        // shapes
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].isCollider) {
                shapeC.Add(sphShapes[j]);
            }
            if (sphShapes[j].isTrigger) {
                sTrigger.Add(sphShapes[j]);
            }
        }
    }

    // checks if circle collides with colliders (returns with first collision found)
    public bool CollideCircle(Vector3 center, float r, bool triggerStuff=false) {
        for (int j = 0; j < circleC.Count; j++) {
            if (circleC[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && circleC[j].isTrigger) {
                    circleC[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < gonC.Count; j++) {
            if (gonC[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && gonC[j].isTrigger) {
                    gonC[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < shapeC.Count; j++) {
            if (shapeC[j].isQuad) {
                if (shapeC[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff && shapeC[j].isTrigger) {
                        shapeC[j].triggered = true;
                    }
                    return true;
                }
            } else {
                if (shapeC[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff && shapeC[j].isTrigger) {
                        shapeC[j].triggered = true;
                    }
                    return true;
                }
            }
        }
        return false;
    }

    // checks if circle collides with triggers (goes over all triggers)
    public bool CollideTriggerCircle(Vector3 center, float r, bool triggerStuff=true) {
        bool collidedWithTrigger = false;

        for (int j = 0; j < cTrigger.Count; j++) {
            if (cTrigger[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && cTrigger[j].isTrigger) {
                    cTrigger[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < gTrigger.Count; j++) {
            if (gTrigger[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && gTrigger[j].isTrigger) {
                    gTrigger[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < sTrigger.Count; j++) {
            if (sTrigger[j].isQuad) {
                if (sTrigger[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff && sTrigger[j].isTrigger) {
                        sTrigger[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            } else {
                if (sTrigger[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff && sTrigger[j].isTrigger) {
                        sTrigger[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            }
        }
        return collidedWithTrigger;
    }

    // returns length ray has to travel to hit collider
    public float RayCastColliders(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int j = 0; j < circleC.Count; j++) {
            float t = circleC[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < gonC.Count; j++) {
            float t = gonC[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < shapeC.Count; j++) {
            if (shapeC[j].isQuad) {
                float t = shapeC[j].qcollider.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            } else {
                float t = shapeC[j].collider_.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            }
        }

        if (minT == 10) {return -1;} else {return minT;}
    }

    // returns length ray has to travel to hit trigger
    public float RayCastTriggers(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int j = 0; j < cTrigger.Count; j++) {
            float t = cTrigger[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < gTrigger.Count; j++) {
            float t = gTrigger[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < sTrigger.Count; j++) {
            if (sTrigger[j].isQuad) {
                float t = sTrigger[j].qcollider.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            } else {
                float t = sTrigger[j].collider_.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            }
        }

        if (minT == 10) {return -1;} else {return minT;}
    }

    public List<SphCircle> GetTriggeredCircles() {
        List<SphCircle> circles = new List<SphCircle>();
        for (int i = 0; i < cTrigger.Count; i++) {
            if (cTrigger[i].triggered) { circles.Add(cTrigger[i]); }
        }
        return circles;
    }

    public List<SphGon> GetTriggeredGons() {
        List<SphGon> gons = new List<SphGon>();
        for (int i = 0; i < gTrigger.Count; i++) {
            if (gTrigger[i].triggered) { gons.Add(gTrigger[i]); }
        }
        return gons;
    }

    public List<SphShape> GetTriggeredShapes() {
        List<SphShape> shapes = new List<SphShape>();
        for (int i = 0; i < sTrigger.Count; i++) {
            if (sTrigger[i].triggered) { shapes.Add(sTrigger[i]); }
        }
        return shapes;
    }

    public List<UVTiles> GetTriggeredUVTiles() {
        List<UVTiles> uvts = new List<UVTiles>();
        for (int i = 0; i < sTrigger.Count; i++) {
            if (sTrigger[i].triggered) {
                GameObject parent_ = sTrigger[i].transform.parent.gameObject;
                if (parent_.GetComponent<UVTiles>() != null) {
                    UVTiles uvt = parent_.GetComponent<UVTiles>();
                    if (!uvts.Contains(uvt)) {
                        uvts.Add(uvt);
                    }
                }
            }
        }
        return uvts;
    }

    void ClearTriggered() {
        for (int i = 0; i < cTrigger.Count; i++)
        {
            cTrigger[i].triggered = false;
        }

        for (int i = 0; i < gTrigger.Count; i++)
        {
            gTrigger[i].triggered = false;
        }

        for (int i = 0; i < sTrigger.Count; i++)
        {
            sTrigger[i].triggered = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNonStatic();
        ClearTriggered();
    }

    void RenderBaseShader() {

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Length > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Length, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        baseShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Length > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Length, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        baseShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Length > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Length, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        baseShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Length + " Triangles: " + triangles.Length + " Quads: " + quads.Length);

        // sending layers
        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3); // quads
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); // quads
            layers_buffer.SetData(layerSplits);
        }
        baseShader.SetBuffer(0, layersID, layers_buffer);
        baseShader.SetInt(layLengthID, layers.Count);

        // sending bg data
        baseShader.SetVector(bgColorID, sb.bgColor);
        baseShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            baseShader.SetBool(useBgTextureID, true);
            baseShader.SetTexture(0, bgTextureID, sb.bgTexture);
            baseShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            baseShader.SetBool(useBgTextureID, false);
            baseShader.SetTexture(0, bgTextureID, black);
            baseShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        baseShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        baseShader.SetBuffer(0, sendRaysID, rays_buffer);

        baseShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        baseShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        baseShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    void RenderRealtimeLightingShader() {
        // setting lighting
        realtimeLightingShader.SetVector(ambientLightID, lighting.ambientLight);
        realtimeLightingShader.SetFloat(gammaID, 1 / ((1.2f * lighting.gammaCorrection) + 1));

        // linear point lights
        PointLightS[] lights = lighting.GetLinearStructs();
        ComputeBuffer lights_buffer = new ComputeBuffer(lights.Length, sizeof(float)*10 + sizeof(int)); // point lights
        lights_buffer.SetData(lights);
        realtimeLightingShader.SetBuffer(0, lightsID, lights_buffer);
        realtimeLightingShader.SetInt(lLengthID, lights.Length);

        // nonLinear point lights
        NlPointLightS[] lights_ = lighting.GetNonLinearStructs();
        ComputeBuffer nlLights_buffer = new ComputeBuffer(lights_.Length, sizeof(float)*9 + sizeof(int)*2); // point lights
        nlLights_buffer.SetData(lights_);
        realtimeLightingShader.SetBuffer(0, nlLightsID, nlLights_buffer);
        realtimeLightingShader.SetInt(nllLengthID, lights_.Length);

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Length > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Length, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        realtimeLightingShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Length > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Length, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        realtimeLightingShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Length > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Length, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        realtimeLightingShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Length + " Triangles: " + triangles.Length + " Quads: " + quads.Length);

        // sending layers
        ComputeBuffer layerNums_buffer = new ComputeBuffer(1, sizeof(int)); 
        layerNums_buffer.SetData(new int[1] {0});

        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3);
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); 
            layers_buffer.SetData(layerSplits);

            layerNums_buffer.Dispose();
            layerNums_buffer = new ComputeBuffer(layers.Count, sizeof(int));
            layerNums_buffer.SetData(layers);
        }
        realtimeLightingShader.SetBuffer(0, layersID, layers_buffer);
        realtimeLightingShader.SetBuffer(0, layerNumsID, layerNums_buffer);
        realtimeLightingShader.SetInt(layLengthID, layers.Count);

        // sending bg data
        realtimeLightingShader.SetVector(bgColorID, sb.bgColor);
        realtimeLightingShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            realtimeLightingShader.SetBool(useBgTextureID, true);
            realtimeLightingShader.SetTexture(0, bgTextureID, sb.bgTexture);
            realtimeLightingShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            realtimeLightingShader.SetBool(useBgTextureID, false);
            realtimeLightingShader.SetTexture(0, bgTextureID, black);
            realtimeLightingShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        realtimeLightingShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        realtimeLightingShader.SetBuffer(0, sendRaysID, rays_buffer);

        realtimeLightingShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        realtimeLightingShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        realtimeLightingShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        lights_buffer.Dispose();
        nlLights_buffer.Dispose();
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        layerNums_buffer.Dispose();
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    void RenderMixedLightingShader() {
        // setting lighting
        mixedLightingShader.SetVector(ambientLightID, lighting.ambientLight);
        mixedLightingShader.SetFloat(gammaID, 1 / ((1.2f * lighting.gammaCorrection) + 1));

        // lightmap
        ComputeBuffer lightLayers_buffer = new ComputeBuffer(lighting.lightLayers.Count, sizeof(int));
        lightLayers_buffer.SetData(lighting.lightLayers);
        mixedLightingShader.SetBuffer(0, lightLayersID, lightLayers_buffer);
        mixedLightingShader.SetTexture(0, lightmapID, lighting.lightmaps);
        mixedLightingShader.SetVector(lightmapStepID, lighting.lightmapStep);
        mixedLightingShader.SetInt(lightmapsDepthID, lighting.lightmaps.depth);

        // linear point lights
        PointLightS[] lights = lighting.GetLinearStructs();
        ComputeBuffer lights_buffer = new ComputeBuffer(lights.Length, sizeof(float)*10 + sizeof(int)); // point lights
        lights_buffer.SetData(lights);
        mixedLightingShader.SetBuffer(0, lightsID, lights_buffer);
        mixedLightingShader.SetInt(lLengthID, lights.Length);

        // nonLinear point lights
        NlPointLightS[] lights_ = lighting.GetNonLinearStructs();
        ComputeBuffer nlLights_buffer = new ComputeBuffer(lights_.Length, sizeof(float)*9 + sizeof(int)*2); // point lights
        nlLights_buffer.SetData(lights_);
        mixedLightingShader.SetBuffer(0, nlLightsID, nlLights_buffer);
        mixedLightingShader.SetInt(nllLengthID, lights_.Length);

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Length > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Length, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        mixedLightingShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Length > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Length, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        mixedLightingShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Length > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Length, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        mixedLightingShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Length + " Triangles: " + triangles.Length + " Quads: " + quads.Length);

        // sending layers
        ComputeBuffer layerNums_buffer = new ComputeBuffer(1, sizeof(int)); 
        layerNums_buffer.SetData(new int[1] {0});

        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3);
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); 
            layers_buffer.SetData(layerSplits);

            layerNums_buffer.Dispose();
            layerNums_buffer = new ComputeBuffer(layers.Count, sizeof(int));
            layerNums_buffer.SetData(layers);
        }
        mixedLightingShader.SetBuffer(0, layersID, layers_buffer);
        mixedLightingShader.SetBuffer(0, layerNumsID, layerNums_buffer);
        mixedLightingShader.SetInt(layLengthID, layers.Count);

        // sending bg data
        mixedLightingShader.SetVector(bgColorID, sb.bgColor);
        mixedLightingShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            mixedLightingShader.SetBool(useBgTextureID, true);
            mixedLightingShader.SetTexture(0, bgTextureID, sb.bgTexture);
            mixedLightingShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            mixedLightingShader.SetBool(useBgTextureID, false);
            mixedLightingShader.SetTexture(0, bgTextureID, black);
            mixedLightingShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        mixedLightingShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        mixedLightingShader.SetBuffer(0, sendRaysID, rays_buffer);

        mixedLightingShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        mixedLightingShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        mixedLightingShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        lightLayers_buffer.Dispose();
        lights_buffer.Dispose();
        nlLights_buffer.Dispose();
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        layerNums_buffer.Dispose();
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!lighting.useLighting) {
            RenderBaseShader();
        } else {
            if (!lighting.useBakedLighting) {
                RenderRealtimeLightingShader();
            } else {RenderMixedLightingShader();}
        }
        Graphics.Blit(renderTexture, dest);
    }
}
