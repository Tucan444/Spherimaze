using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SphericalCamera : MonoBehaviour
{
    public enum Projection {
        Stereographic, Gnomic, Orthographic, Equirectangular, Gue
    };
    public Projection projection = Projection.Stereographic;
    public int[] resolution = new int[2] {512, 320};
    public Vector2 sphericalPosition = new Vector2();
    [SerializeField][Range(-Mathf.PI, Mathf.PI)]public float directionRotation = 0;
    [SerializeField][Range(0.01f, 200)]public float speed = 40;
    [SerializeField][Range(0.01f, 360)]public float turnSpeed = 180;
    [SerializeField] public float width = 2.6f;
    [SerializeField][Range(0.1f, 3)]public float screenSpeed = 1;
    public bool isCollider = true;
    public bool triggers = true;
    [Range(0, Mathf.PI)] public float colliderR = 0.1f;
    public bool pinScreenToPlayer = false;
    public bool noWidthControl = false;

    float height;

    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);
    Vector3 direction = new Vector3(0, 1, 0);
    Quaternion totalQ = Quaternion.identity;
    [HideInInspector] public Quaternion screenQ = Quaternion.identity;
    Quaternion moveQ;
    Quaternion Q = Quaternion.identity;

    Vector3[][] renderRays;
    [HideInInspector] public Vector3[] sendRays;
    Vector3[] corners;

    SphSpaceManager ssm;
    SphericalUtilities su = new SphericalUtilities();

    Vector2 playerInput = new Vector2();

    public void GetDefaultSetup() {

        if (sphericalPosition[1] == 0) {
            sphericalPosition[1] += 0.0001f;
        }
        // setting cartesian position and initial direction
        position = su.Spherical2Cartesian(sphericalPosition);
        direction = su.GetDirection(position);
        if (sphericalPosition[1] < 0) {
            direction = -direction;
        }
        Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
        direction = Q * direction;

        // setting render rays
        renderRays = new Vector3[resolution[0]][];

        if (!noWidthControl) {
            if (projection == Projection.Gue) { if (width > 3) {width = 3;} }
            if (projection == Projection.Equirectangular) { if (width > Mathf.PI * 2) {width = Mathf.PI * 2;} }
        }

        height = width * ((float)resolution[1] / (float)resolution[0]);

        if (!noWidthControl) {
            if (projection == Projection.Gue) { if (height >= 3) {width *= 3 / height; height = 3;} }
            if (projection == Projection.Equirectangular) { if (height > 3.14f) {width *= 3.14f / height; height = 3.14f;} }
        }


        if (projection == Projection.Gue || projection == Projection.Equirectangular) {

            corners = new Vector3[4] {
                new Vector3(-width * 0.5f, height * 0.5f),
                new Vector3(-width * 0.5f, -height * 0.5f),
                new Vector3(width * 0.5f, height * 0.5f),
                new Vector3(width * 0.5f, -height * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                corners[i] = Q * su.AddCartSpher(su.Spherical2Cartesian(corners[i]), sphericalPosition);
            }
        } else {
            corners = new Vector3[4] {
                new Vector3(1, -height * 0.5f, width * 0.5f),
                new Vector3(1, -height * 0.5f, -width * 0.5f),
                new Vector3(1, height * 0.5f, width * 0.5f),
                new Vector3(1, height * 0.5f, -width * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                corners[i] = Q * su.AddCartSpherR3(corners[i], sphericalPosition);
            }
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        GetDefaultSetup();
        transform.position = position;

        SphSpaceManager.sc = this;
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }


    void OnDrawGizmos() {
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(position, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position  + (0.2f * direction));

        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++) {
            Gizmos.DrawSphere(screenQ * corners[i], 0.05f);
        }

        if (projection == Projection.Gue) {
            GizmosDrawLine(screenQ * corners[0], screenQ * corners[1]);
            GizmosDrawLine(screenQ * corners[1], screenQ * corners[3]);
            GizmosDrawLine(screenQ * corners[2], screenQ * corners[0]);
            GizmosDrawLine(screenQ * corners[3], screenQ * corners[2]);
        } else if (projection == Projection.Equirectangular) {

            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
            Vector3[] newCorners = new Vector3[4] {
                new Vector3(1, -width * 0.5f, height * 0.5f),
                new Vector3(1, -width * 0.5f, -height * 0.5f),
                new Vector3(1, width * 0.5f, -height * 0.5f),
                new Vector3(1, width * 0.5f, height * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                int ii = (i+1) % 4;
                Vector3[] line = GetLine(newCorners[ii], newCorners[i], 4);
                for (int j = 0; j < 4; j++)
                {
                    line[j] = screenQ * (Q * su.AddCartSpher(su.SphericalToCartesian(line[j]), sphericalPosition));
                }
                su.GizmosDrawPointsNoLoop(line);
            }

        } else {
            Gizmos.DrawLine(screenQ * corners[0], screenQ * corners[1]);
            Gizmos.DrawLine(screenQ * corners[1], screenQ * corners[3]);
            Gizmos.DrawLine(screenQ * corners[2], screenQ * corners[0]);
            Gizmos.DrawLine(screenQ * corners[3], screenQ * corners[2]);
        }

        if (isCollider) {
            Gizmos.color = Color.green * 1.4f;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), colliderR));
            Gizmos.color = Color.green * 1.2f;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), colliderR * 0.9f));
            Gizmos.color = Color.green;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), colliderR * 0.8f));
        }
    }

    public void GizmosDrawLine(Vector3 a, Vector3 b, int n=4) {
        Vector3[] points = su.GetLinePoints(a, b, n);
        for (int i = 0; i < n-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    void Start() {

        if (projection == Projection.Gue) {
            Vector3[] upLine = su.GetLinePoints(corners[0], corners[2], resolution[0]);
            Vector3[] downLine = su.GetLinePoints(corners[1], corners[3], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = su.GetLinePoints(upLine[i], downLine[i], resolution[1]);
            }
        } else if (projection == Projection.Gnomic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = renderRays[i][j].normalized;
                }
            }
        } else if (projection == Projection.Orthographic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = RaySphereIntersectionFirst(renderRays[i][j], -position);
                }
            }
        } else if (projection == Projection.Stereographic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = RaySphereIntersectionSecond(-position, (renderRays[i][j] + position).normalized);
                }
            }
        } else if (projection == Projection.Equirectangular) {
            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);

            Vector3[] upLine = GetLine(new Vector3(1, -width * 0.5f, height * 0.5f), new Vector3(1, width * 0.5f, height * 0.5f), resolution[0]);
            Vector3[] downLine = GetLine(new Vector3(1, -width * 0.5f, -height * 0.5f), new Vector3(1, width * 0.5f, -height * 0.5f), resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = Q * su.AddCartSpher(su.SphericalToCartesian(renderRays[i][j]), sphericalPosition);
                }
            }
        }

        sendRays = new Vector3[resolution[0] * resolution[1]];

        for (int i = 0; i < sendRays.Length; i++) {
            int j = (i - (i % resolution[1])) / resolution[1];
            int ii = renderRays[j].Length - (i % resolution[1]) - 1;

            sendRays[i] = renderRays[j][ii];
        }
    }

    Vector3[] GetLine(Vector3 a, Vector3 b, int n) {
        Vector3[] linePoints = new Vector3[n];

        float fraction = 1.0f / (float)(n-1);
        for (int i = 0; i < n; i++) {
            linePoints[i] = (a * (1-(i * fraction))) + (b * (i * fraction));
        } 

        return linePoints;
    }

    Vector3 RaySphereIntersectionFirst(Vector3 o, Vector3 dir) {
        float d = o.magnitude;
        float dtc = Mathf.Sqrt((d*d)-1);
        if (dtc > 1) {return new Vector3(10, 0, 0);}
        return o + (dir * (1 - Mathf.Sqrt(1-(dtc*dtc))));
    }
    Vector3 RaySphereIntersectionSecond(Vector3 o, Vector3 dir) {
        float od = Vector3.Dot(-o, dir);
        return o + (dir * (od * 2));
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = position;
        if (isCollider) {
            MoveWithCollision(playerInput[1] * speed * Time.deltaTime);
        } else {Move(playerInput[1] * speed * Time.deltaTime);}
        Rotate(playerInput[0] * turnSpeed * Time.deltaTime);

        if (triggers) {
            ssm.CollideTriggerCircle(position, colliderR);
        }
    }

    public void QuitApp() {Application.Quit();}

    public void Move(float rAngle) {

        moveQ = Quaternion.AngleAxis(rAngle, Vector3.Cross(position, direction));
        position = moveQ * position;
        direction = moveQ * direction;

        totalQ = moveQ * totalQ;
        if (pinScreenToPlayer) {
            screenQ = totalQ;
        } else {
            screenQ = Quaternion.Lerp(screenQ, totalQ, Mathf.Min((0.1f * speed) * screenSpeed * screenSpeed * Time.deltaTime, 1));
        }

    }

    public void MoveWithCollision(float rAngle) {
        Quaternion[] rotQ = new Quaternion[2] {
            Quaternion.AngleAxis(90, position),
            Quaternion.AngleAxis(60, position)
        };

        moveQ = Quaternion.AngleAxis(rAngle, Vector3.Cross(position, direction));
        Quaternion moveQ1 = Quaternion.AngleAxis(rAngle*0.5f, Vector3.Cross(position, rotQ[0] * direction));
        Quaternion moveQ2 = Quaternion.AngleAxis(rAngle*0.75f, Vector3.Cross(position, rotQ[1] * direction));
        Quaternion moveQ3 = Quaternion.AngleAxis(-rAngle*0.75f, Vector3.Cross(position, rotQ[1] * direction));
        Quaternion moveQ4 = Quaternion.AngleAxis(-rAngle*0.5f, Vector3.Cross(position, rotQ[0] * direction));
        bool[] checks = new bool[5] {CheckMove(moveQ1), CheckMove(moveQ2), CheckMove(moveQ, triggers), CheckMove(moveQ3), CheckMove(moveQ4)};

        if (checks[2]) {
            if (checks[0] && checks[1] && checks[3] && checks[4]) {
                return;
            } else if (!checks[1]) {moveQ = moveQ2;
            } else if (!checks[3]) {moveQ = moveQ3;
            } else if (!checks[0]) {moveQ = moveQ1;
            } else {moveQ = moveQ4;}
        }
        
        position = moveQ * position;
        direction = moveQ * direction;

        totalQ = moveQ * totalQ;
        if (pinScreenToPlayer) {
            screenQ = totalQ;
        } else {
            screenQ = Quaternion.Lerp(screenQ, totalQ, Mathf.Min((0.1f * speed) * screenSpeed * screenSpeed * Time.deltaTime, 1));
        }
    }   

    bool CheckMove(Quaternion q, bool triggerStuff=false) {
        Vector3 movedP = q * position;
        return ssm.CollideCircle(movedP, colliderR, triggerStuff);
    }

    public void Rotate(float rAngle) {
        Q = Quaternion.AngleAxis(rAngle, position);
        direction = Q * direction;

        totalQ = Q * totalQ;
        if (pinScreenToPlayer) {
            screenQ = totalQ;
        } else {
            screenQ = Quaternion.Lerp(screenQ, totalQ, Mathf.Min((0.1f * speed) * screenSpeed * screenSpeed * Time.deltaTime, 1));
        }
    }

    // returs mouse position in cartesian coordinates, (10, 0, 0) if its out of space
    public Vector3 GetMousePos() {
        Vector2 mousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Vector3 ray;

        try{
            ray = renderRays[(int)(mousePos.x * resolution[0])][(resolution[1] - 1) - (int)(mousePos.y * resolution[1])];
        } catch (Exception e) {
            return su.Spherical2Cartesian(new Vector2());
        }

        if (Mathf.Abs(ray.x) + Mathf.Abs(ray.y) + Mathf.Abs(ray.z) > 6) {return new Vector3(10, 0, 0);}
        return (screenQ * ray).normalized;
    }

    public void OnMovement(InputAction.CallbackContext context) {
        Vector2 direction = context.ReadValue<Vector2>();
        playerInput = direction;
    }
}
