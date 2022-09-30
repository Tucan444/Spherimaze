using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

[ExecuteAlways]
public class Lighting : MonoBehaviour
{
    public bool useLighting = false; 
    public Color ambientLight = new Color(0.05f, 0.05f, 0.05f, 1);
    [Range(0, 1)] public float gammaCorrection = 1;
    public bool useBakedLighting = false;
    [HideInInspector] public Texture2DArray lightmaps;
    [HideInInspector] public bool softShadows = true;
    [HideInInspector] public int softShadowDetail = 1;
    [HideInInspector] public bool softShadowCorrection = false;
    [HideInInspector] public int detail = 5;
    [HideInInspector] public bool stopBaking = false;

    [HideInInspector] public bool baking = false;

    List<PointLight> lights;
    List<PointLight> linearLights;
    List<PointLight> nonLinearLights;

    [HideInInspector] public List<int> lightLayers = new List<int>();

    [HideInInspector] public Vector2 lightmapStep;

    int count = 0;
    
    SphSpaceManager ssm;
    SphericalUtilities su = new SphericalUtilities();

    void OnEnable() {
        SphSpaceManager.lighting = this;
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void Start() {
        if (useBakedLighting && !lightmaps) {Debug.Log("baked lighting is turned on but no lightmap is set");}
        if (useBakedLighting) { lightmapStep = new Vector2(1/su.TAU, 1/Mathf.PI); }
        lights = new List<PointLight>();

        PointLight[] pointLights = GetComponentsInChildren<PointLight>();
        for (int i = 0; i <pointLights.Length; i++) {lights.Add(pointLights[i]);}

        SortLinear();
    }

    public void SortLinear() {
        linearLights = new List<PointLight>();
        nonLinearLights = new List<PointLight>();

        for (int i = 0; i < lights.Count; i++)
        {
            if (!lights[i].bakedLighting && lights[i].linear) {
                linearLights.Add(lights[i]);
            } else if (!lights[i].bakedLighting && !lights[i].linear) {
                nonLinearLights.Add(lights[i]);
            }
        }
    }

    #if UNITY_EDITOR
    public async void BakeLighting() {
        baking = true;

        Assembly assembly = Assembly.GetAssembly (typeof(SceneView));
        Type logEntries = assembly.GetType("UnityEditor.LogEntries");
        MethodInfo clearConsoleMethod = logEntries.GetMethod ("Clear");
        clearConsoleMethod.Invoke (new object (), null);

        Debug.Log("baking");

        // getting lights
        lights = new List<PointLight>();
        lightLayers = new List<int>();

        PointLight[] pointLights = GetComponentsInChildren<PointLight>();
        for (int i = 0; i <pointLights.Length; i++) {
            if (pointLights[i].bakedLighting) {lights.Add(pointLights[i]);}
        }

        LLayerComparer llc = new LLayerComparer();
        lights.Sort(llc);

        for (int i = 0; i < lights.Count; i++){
            if (!lightLayers.Contains(lights[i].layer)) {
                lightLayers.Add(lights[i].layer);
            }
        }

        // getting preps
        int[] resolution = new int[2] {(int)Mathf.Pow(2, detail+1), (int)Mathf.Pow(2, detail)};
        Vector2 step = new Vector2(su.TAU / (float)resolution[0], Mathf.PI / (float)resolution[1]);
        Vector2 start = new Vector2(-Mathf.PI, -su.HalfPI);

        List<SphCircle> circles = ssm.GetStaticCircles();
        List<SphGon> gons = ssm.GetStaticGons();
        List<SphShape> shapes = ssm.GetStaticShapes();

        // trying new stuff here
        Texture2DArray textures = new Texture2DArray(resolution[0], resolution[1], lightLayers.Count, TextureFormat.RGBA32, false);
        textures.filterMode = FilterMode.Point;

        for (int o = 0; o < lightLayers.Count; o++)
        {
            // filling texture with black
            var fillColorArraya = textures.GetPixels(o);
            
            for(var i = 0; i < fillColorArraya.Length; ++i)
            {
                fillColorArraya[i] = Color.black;
            }
            
            textures.SetPixels(fillColorArraya, o);
        }
        textures.Apply();
        lightmaps = textures;

        for (int o = 0; o < lightLayers.Count; o++)
        {
            Color[] pixelColors = textures.GetPixels(o);

            // calculating lighting for pixels
            pixelColors = await Task.Run(() => {
                for (int x = 0; x < resolution[0]; x++)
                {
                    for (int y = 0; y < resolution[1]; y++)
                    {
                        int pixelIndex = (y*resolution[0]) + x;
                        Vector2 sphPos = start + new Vector2(step.x * (0.5f + x), step.y * (0.5f + y)); // spherical position of pixel being calculated
                        Vector3 pos = su.Spherical2Cartesian(sphPos);

                        for (int i = 0; i < lights.Count; i++)
                        {
                            PointLight l = lights[i];
                            if (l.layer == lightLayers[o]) {
                                bool collided;
                                float multip = 1;
                                if (softShadows && su.SphDistance(l.position, pos) > l.radius) {
                                    collided = false;
                                    multip = 0;

                                    Vector3[] softPoints = GetSoftShadowPoints(pos, l.position, l.radius, l.layer, circles, gons, shapes);

                                    bool[] checks = new bool[softPoints.Length];
                                    for (int iii = 0; iii < softPoints.Length; iii++){
                                        checks[iii] = shadowRay(pos, softPoints[iii], l.layer, circles, gons, shapes);
                                    } 

                                    float fraction = 1/(float)(softPoints.Length);
                                    for (int iii = 0; iii < softPoints.Length; iii++)
                                    {
                                        if (!checks[iii]) {multip += fraction;}
                                    }
                                } else {collided = shadowRay(pos, l.position, l.layer, circles, gons, shapes);}

                                // calculating lighting
                                Color c = Color.black;
                                if (!collided) {
                                    if (l.linear) {
                                        float top;
                                        float slope;
                                        if (l.boundary > l.radius) {
                                            top = l.boundary * (l.power / (l.boundary - l.radius));
                                            slope = top/l.boundary;
                                        } else {
                                            top = l.power + 10;
                                            slope = 0.01f;
                                        }

                                        c = multip * l.color * Mathf.Min(l.power, Mathf.Max(0, top - (Mathf.Acos(Vector3.Dot(pos, l.position)) * slope)));
                                    } else {
                                        int fallout = 1;
                                        if (l._3D) { fallout = 2; }

                                        c = multip * l.color * (l.power / Mathf.Pow(Mathf.Max(1, Mathf.Acos(Vector3.Dot(pos, l.position)) + 1 - l.radius), fallout));
                                    }
                                }
                                // adding color to pixel
                                pixelColors[pixelIndex] += c;
                            }
                        }
                    }
                    if (stopBaking) {return pixelColors;}
                    Debug.Log(0.01f * Mathf.Round(10000 * ((x+1) / (float)resolution[0]) * ((float)(o+1)/(float)lightLayers.Count)) + "% done");
                }
                return pixelColors;
            });
            
            textures.SetPixels(pixelColors, o);
        }
        textures.Apply();

        if (!stopBaking) {
            AssetDatabase.CreateAsset(textures, "Assets/Textures/Lightmap/lightmap_" + SceneManager.GetActiveScene().name + ".asset");
            lightmaps = textures;

            SaveLightmapsAsPNG(textures);

            Debug.Log("baking done");
            Debug.Log("lightmap size: [" + resolution[0] + ", " + resolution[1] + ", " + lightLayers.Count + "]");
        } else {
            Debug.Log("stopped baking lightmap of size: [" + resolution[0] + ", " + resolution[1] + ", " + lightLayers.Count + "]");
        }

        baking = false;
        stopBaking = false;
    }
    #endif
    
    // returns true if ray collided with something
    public bool shadowRay(Vector3 pos, Vector3 lpos, int layer, List<SphCircle> circles, List<SphGon> gons, List<SphShape> shapes) {
        bool collided = false;

        // checking collisions with circles
        for (int j = 0; j < circles.Count; j++)
        {
            if (circles[j].layer > layer) {
                if (!collided && !circles[j].invisible && !circles[j].empty) {
                    collided = su.CircleLineCollision(circles[j].position, circles[j].radius, pos, lpos);
                } else if (collided) {break;}
            }
        }

        // checking collisions with gons
        for (int j = 0; j < gons.Count; j++)
        {
            for (int jj = 0; jj < gons[j].vertices.Length; jj++)
            {
                if (!collided && (gons[j].layer > layer) && !gons[j].invisible && !gons[j].empty){
                    collided = su.LineLineCollision(gons[j].vertices[jj], gons[j].vertices[(jj+1) % gons[j].vertices.Length], pos, lpos);
                } else if (collided) {break;}
            }
        }

        // checking collisions with shapes
        for (int j = 0; j < shapes.Count; j++)
        {
            for (int jj = 0; jj < shapes[j].vertPos.Length; jj++)
            {
                if (!collided && (shapes[j].layer > layer) && !shapes[j].invisible && !shapes[j].empty){
                    collided = su.LineLineCollision(shapes[j].vertPos[jj], shapes[j].vertPos[(jj+1) % shapes[j].vertPos.Length], pos, lpos);
                } else if (collided) {break;}
            }
        }

        return collided;
    }

    public Vector3[] GetSoftShadowPoints(Vector3 pos, Vector3 lpos, float radius, int layer, List<SphCircle> circles, List<SphGon> gons, List<SphShape> shapes) {
        Vector3[] softShadowPoints = new Vector3[1 + (softShadowDetail * 2)];
        Quaternion q = Quaternion.AngleAxis(-180/(float)(softShadowDetail*2), lpos);

        softShadowPoints[0] = Quaternion.AngleAxis(90, lpos) * su.SphLerp(lpos, pos, radius/su.SphDistance(pos, lpos));
        for (int i = 1; i < softShadowPoints.Length; i++)
        {
            softShadowPoints[i] = q * softShadowPoints[i-1];
        }

        if (softShadowCorrection) {
            List<Vector3> validOnes = new List<Vector3>();

            for (int i = 0; i < softShadowPoints.Length; i++)
            {
                if (!shadowRay(lpos, softShadowPoints[i], layer, circles, gons, shapes)) {validOnes.Add(softShadowPoints[i]);}
            }

            softShadowPoints = new Vector3[validOnes.Count];
            for (int i = 0; i < softShadowPoints.Length; i++) {
                softShadowPoints[i] = validOnes[i];
            }
        }

        return softShadowPoints;
    }

    public void SaveLightmapsAsPNG(Texture2DArray _textures)
    {
        for (int i = 0; i < _textures.depth; i++)
        {
            Color[] pixels = _textures.GetPixels(i);
            Texture2D tex = new Texture2D(_textures.width, _textures.height);
            tex.SetPixels(pixels);
            byte[] bytes = tex.EncodeToPNG();
            var dirPath = Application.dataPath + "/../Assets/Textures/Lightmap/LightmapPreview_" + SceneManager.GetActiveScene().name + i + ".png";
            File.WriteAllBytes(dirPath, bytes);
        }
    }

    public void AddPointLight() {
        GameObject obj = new GameObject(count.ToString());
        PointLight pl = obj.AddComponent(typeof(PointLight)) as PointLight;
        #if UNITY_EDITOR
        Undo.RecordObject(pl, "Created point light");
        #endif
        obj.transform.parent = gameObject.transform;

        count++;
    }

    public PointLightS[] GetLinearStructs() {
        PointLightS[] lightsS = new PointLightS[1] {new PointLightS()};
        if (linearLights.Count > 0) {
            lightsS = new PointLightS[linearLights.Count];

            for (int i = 0; i < linearLights.Count; i++) {
                lightsS[i] = linearLights[i].GetLinearStruct();
            }
        } else {
            lightsS[0].layer = 0;
            lightsS[0].pos = new Vector3(1, 0, 0);
            lightsS[0].power = 0;
            lightsS[0].top = 0;
            lightsS[0].slope = 0;
            lightsS[0].color = Color.black;
        }

        return lightsS;
    }

    public NlPointLightS[] GetNonLinearStructs() {
        NlPointLightS[] lightsS = new NlPointLightS[1] {new NlPointLightS()};
        if (nonLinearLights.Count > 0) {
            lightsS = new NlPointLightS[nonLinearLights.Count];

            for (int i = 0; i < nonLinearLights.Count; i++) {
                lightsS[i] = nonLinearLights[i].GetNonLinearStruct();
            }
        } else {
            lightsS[0].layer = 0;
            lightsS[0].pos = new Vector3(1, 0, 0);
            lightsS[0].radius = 0;
            lightsS[0].power = 0;
            lightsS[0].color = Color.black;
            lightsS[0].fallout = 1;
        }

        return lightsS;
    }
}
