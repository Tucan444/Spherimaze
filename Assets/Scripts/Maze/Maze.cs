using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class Maze : MonoBehaviour
{   
    public Vector2 sphPosition = new Vector2();
    [Range(0.01f, 0.2f)] public float width = 0.1f;
    public Color color = new Color(0.6f, 0, 0.4f);
    [Range(0, 3)] public int subdivisions = 1;
    public bool collider = true;

    static int[] edgeRem = new int[6] {0, 1, 2, 4, 10, 20};

    static float phi = (1.0f + Mathf.Sqrt(5)) / 2.0f;
    List<Vector3> vertices;
    List<Node> nodes = new List<Node>();
    List<Edge> edges = new List<Edge>();
    List<Vector3> triangles = new List<Vector3>();

    List<Node> MSTnodes = new List<Node>();
    List<Edge> MSTedges = new List<Edge>();
    MinHeap<Edge> etd;
    int nodeId = 0;

    GameObject edgesHolder;
    GameObject nodesHolder;

    float halfWidth = 1;

    SphericalUtilities su = new SphericalUtilities();

    // terrible performance (for faster perfomance implementing min heap for edges and hash table for nodes is required)
    // O(n**2)
    void GenerateMaze() {
        DestroyChildren();

        halfWidth = width / 2;
        nodeId = 0;

        vertices = new List<Vector3>() {new Vector3(-1,  phi,  0),
                                    new Vector3( 1,  phi,  0),
                                    new Vector3(-1, -phi,  0),
                                    new Vector3( 1, -phi,  0),
                                    new Vector3( 0, -1,  phi),
                                    new Vector3( 0,  1,  phi),
                                    new Vector3( 0, -1, -phi),
                                    new Vector3( 0,  1, -phi),
                                    new Vector3( phi,  0, -1),
                                    new Vector3( phi,  0,  1),
                                    new Vector3(-phi, 0, -1),
                                    new Vector3(-phi, 0,  1)};
        nodes = new List<Node>();
        edges = new List<Edge>();
        triangles = new List<Vector3>();
        MSTnodes = new List<Node>();
        MSTedges = new List<Edge>();
        GetTriangles();

        // getting icosahedron nodes
        for (int i = 0; i < 12; i++){
            nodes.Add(new Node(vertices[i], i));
        }

        // subdividing
        for (int i = 0; i < subdivisions; i++)
        {
            Subdivide();
        }

        int id = 0;
        // getting edges
        for (int i = 0; i < vertices.Count-1; i++){
            for (int j = i+1; j < vertices.Count; j++)
            {
                if (Vector3.Distance(vertices[i], vertices[j]) < 2.1f / (Mathf.Pow(2, subdivisions))) {
                    edges.Add(new Edge(i, j, Random.Range(.0f, 1.0f), id));
                    nodes[i].edges.Add(id);
                    nodes[j].edges.Add(id);
                    id++;
                }
            }
        }

        // normalizing nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].pos = nodes[i].pos.normalized;
        }

        // creating minimum spanning tree

        etd = new MinHeap<Edge>();
        ProcessAddingNode(0);

        for (int i = 0; i < nodes.Count-1; i++)
        {
            Edge minEdge = etd.Pop();
            while (!CheckEdge(minEdge)) {
                minEdge = etd.Pop();
            }

            MSTedges.Add(minEdge);
            if (!nodes[minEdge.a].inGraph) {
                ProcessAddingNode(minEdge.a);
            } else {
                ProcessAddingNode(minEdge.b);
            }
        }

        MSTnodes = new List<Node>();
        for (int i = 0; i < nodes.Count; i++){
            MSTnodes.Add(new Node(nodes[i].pos, i));
        }
        for (int i = 0; i < MSTedges.Count; i++){
            MSTnodes[MSTedges[i].a].edges.Add(MSTedges[i].id);
            MSTnodes[MSTedges[i].b].edges.Add(MSTedges[i].id);
        }

        // removing random edges    
        for (int i = 0; i < edgeRem[subdivisions]; i++)
        {
            Edge e = MSTedges[Random.Range(0, MSTedges.Count-1)];
            MSTnodes[e.a].edges.Remove(e.id);
            MSTnodes[e.b].edges.Remove(e.id);
            MSTedges.Remove(e);
        }

        int cc = MSTnodes.Count;
        int removedOnes = 0;
        for (int i = 0; i < cc; i++)
        {
            if (MSTnodes[i-removedOnes].edges.Count == 0) {
                MSTnodes.RemoveAt(i-removedOnes);
                removedOnes++;
            }
        }

        // construction
        for (int i = 0; i < MSTedges.Count; i++)
        {
            Vector3[] vertices = new Vector3[4];
            Node[] cn = new Node[2] {nodes[MSTedges[i].a], nodes[MSTedges[i].b]};

            Vector3 direction = Quaternion.AngleAxis(su.Rad2Deg * su.HalfPI, cn[0].pos) * cn[1].pos;
            vertices[0] = su.RayTravel(cn[0].pos, direction, halfWidth);
            vertices[1] = su.RayTravel(cn[0].pos, direction, -halfWidth);

            direction = Quaternion.AngleAxis(su.Rad2Deg * su.HalfPI, cn[1].pos) * cn[0].pos;
            vertices[2] = su.RayTravel(cn[1].pos, direction, halfWidth);
            vertices[3] = su.RayTravel(cn[1].pos, direction, -halfWidth);

            CreateEdge(vertices);
        }

        for (int i = 0; i < MSTnodes.Count; i++)
        {
            CreateNode(MSTnodes[i].pos);
        }
    }

    public bool CheckEdge(Edge e) {
        if (!(nodes[e.a].inGraph && nodes[e.b].inGraph)) {return true;} return false;
    }

    public void ProcessAddingNode(int i) {
        nodes[i].inGraph = true;
        Node n = nodes[i];
        for (int j = 0; j < n.edges.Count; j++){
            Edge e = edges[n.edges[j]];
            if (nodes[e.a].id == n.id) {
                nodes[e.b].edges.Remove(e.id);
            } else {
                nodes[e.a].edges.Remove(e.id);
            }
            etd.AddNode(new Node_<Edge>(edges[n.edges[j]], edges[n.edges[j]].w, nodeId, etd));
            nodeId++;
        }
    }

    void CreateEdge(Vector3[] vertices) {
        // convert to polar + rotate + reorigin + rescale
        Vector2[] polarVerts = new Vector2[4];
        for (int ii = 0; ii < 4; ii++)
        {
            polarVerts[ii] = su.Cartesian2Polar(vertices[ii]);
        }

        GameObject child = new GameObject("edge");
        SphShape ss = child.AddComponent(typeof(SphShape)) as SphShape;
        #if UNITY_EDITOR
        Undo.RecordObject(ss, "Created edge");
        #endif
        ss.layer = 3;
        ss.isCollider = collider;
        ss.sphPosition = sphPosition;
        ss.scale = 1;
        ss.color = color;
        ss.polarVertices = polarVerts;
        ss.GetDefaultSetup();
        ss.isQuad = true;
        child.transform.parent = edgesHolder.transform;
    }

    void CreateNode(Vector3 pos) {
        GameObject child = new GameObject("node");
        SphCircle ss = child.AddComponent(typeof(SphCircle)) as SphCircle;
        #if UNITY_EDITOR
        Undo.RecordObject(ss, "Created node");
        #endif
        ss.layer = 3;
        ss.isCollider = collider;
        ss.sphPosition = su.Cartesian2Spherical(su.AddCartSpher(pos, sphPosition));
        ss.radius = halfWidth;
        ss.color = color;
        ss.GetDefaultSetup();
        child.transform.parent = nodesHolder.transform;
    }

    void Subdivide() {
        int nodeID = nodes.Count;

        List<Vector3> tris = new List<Vector3>();

        for (int i = 0; i < triangles.Count; i++) {
            Vector3 t = triangles[i];
            Vector3[] ns = new Vector3[3] {Vector3.Lerp(nodes[(int)t.x].pos, nodes[(int)t.y].pos, 0.5f),
                                           Vector3.Lerp(nodes[(int)t.y].pos, nodes[(int)t.z].pos, 0.5f),
                                           Vector3.Lerp(nodes[(int)t.x].pos, nodes[(int)t.z].pos, 0.5f)};

            int[] ins = new int[3];
                    
            for (int ii = 0; ii < 3; ii++) {
                if (vertices.Contains(ns[ii])) {
                    ins[ii] = vertices.IndexOf(ns[ii]);
                } else {
                    ins[ii] = nodeID;
                    vertices.Add(ns[ii]);
                    nodes.Add(new Node(ns[ii], nodeID));
                    nodeID++;
                }
            }
            
            tris.Add(new Vector3(t.x, ins[0], ins[2]));
            tris.Add(new Vector3(t.y, ins[0], ins[1]));
            tris.Add(new Vector3(t.z, ins[1], ins[2]));
            tris.Add(new Vector3(ins[0], ins[1], ins[2]));
        }

        triangles = tris;
    }

    void DestroyChildren() {
        var allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren) { //var child : Transform in allChildren.
            if (child != transform && child.gameObject.name != "Nodes" && child.gameObject.name != "Edges") {
                StartCoroutine(Destroy(child.gameObject));
                if (Application.isPlaying) {
                    Destroy(child.gameObject.GetComponent<SphShape>());
                    Destroy(child.gameObject.GetComponent<SphCircle>());
                }
            }
        }
    }

    IEnumerator Destroy(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }

    void GetTriangles() {
        triangles.Add(new Vector3(0, 11, 5));
        triangles.Add(new Vector3(0, 5, 1));
        triangles.Add(new Vector3(0, 1, 7));
        triangles.Add(new Vector3(0, 7, 10));
        triangles.Add(new Vector3(0, 10, 11));
        
        // 5 adjacent faces
        triangles.Add(new Vector3(1, 5, 9));
        triangles.Add(new Vector3(5, 11, 4));
        triangles.Add(new Vector3(11, 10, 2));
        triangles.Add(new Vector3(10, 7, 6));
        triangles.Add(new Vector3(7, 1, 8));
        
        // 5 faces around point 3
        triangles.Add(new Vector3(3, 9, 4));
        triangles.Add(new Vector3(3, 4, 2));
        triangles.Add(new Vector3(3, 2, 6));
        triangles.Add(new Vector3(3, 6, 8));
        triangles.Add(new Vector3(3, 8, 9));
        
        // 5 adjacent faces
        triangles.Add(new Vector3(4, 9, 5));
        triangles.Add(new Vector3(2, 4, 11));
        triangles.Add(new Vector3(6, 2, 10));
        triangles.Add(new Vector3(8, 6, 7));
        triangles.Add(new Vector3(9, 8, 1));
    }

    void OnEnable() {
        edgesHolder = GameObject.Find("Edges");
        nodesHolder = GameObject.Find("Nodes");
        GenerateMaze();
    }

    void OnValidate() {
        edgesHolder = GameObject.Find("Edges");
        nodesHolder = GameObject.Find("Nodes");
        GenerateMaze();
    }

    // Start is called before the first frame update
    void Start(){
    }

    // Update is called once per frame
    void Update(){
    }
}


public class Node
{
    public Vector3 pos;
    public int id;
    public bool inGraph = false;
    public List<int> edges;
    public Node(Vector3 pos_, int id_) {
        pos = pos_;
        id = id_;
        edges = new List<int>();
    }
}

public struct Edge {
    public int a;
    public int b;
    public float w;
    public int id;

    public Edge(int a_, int b_, float w_, int id_) {
        a = a_;
        b = b_;
        w = w_;
        id = id_;
    }
}