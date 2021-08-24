using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier : MonoBehaviour
{
    class LineAndSpheres
    {
        public LineRenderer lineRenderer;
        public List<GameObject> spheres;

        public LineAndSpheres()
        {
            spheres = new List<GameObject>();
        }
    }

    public bool isRand = false;

    public bool isRoot = false;

    public LineRenderer linePrefab;
    public GameObject spherePrefab;

    public Material lineMaterial;

    public Vector3[] initialPoints;

    private float t1 = 0f;
    private const float t1_step = 0.2f;
    private bool dir = true;

    private Vector3 lastSpherePos;
    private bool firstLoop = true;
    private int firstLoopCounter = 0;

    private List<LineAndSpheres> lines;

    private List<GameObject> bezierGraphic;

    // Start is called before the first frame update
    void Start()
    {
        GenerateScene();
    }

    void Clear()
    {
        if (lines != null)
        {
            foreach (var tuple in lines)
            {
                Destroy(tuple.lineRenderer);
                foreach (var sphere in tuple.spheres)
                {
                    Destroy(sphere);
                }
            }

            foreach(var sphere in bezierGraphic)
            {
                Destroy(sphere);
            }
        }
    }

    public void Reanimate()
    {
        t1 = 0f;
        dir = true;
        lastSpherePos = Vector3.zero;
        firstLoop = true;
        firstLoopCounter = 0;

        foreach(var sphere in bezierGraphic)
        {
            Destroy(sphere);
        }
    }

    public void GenerateScene()
    {
        if (isRoot)
        {
            t1 = 0f;
            dir = true;
            lastSpherePos = Vector3.zero;
            firstLoop = true;
            firstLoopCounter = 0;
            Clear();
            lines = new List<LineAndSpheres>();
            bezierGraphic = new List<GameObject>();
            GameObject lastSphere = null;

            if (isRand)
            {
                const float maxVert = 20;
                float maxY = maxVert;
                float maxX = maxVert * Camera.main.aspect;
                Debug.Log(maxX + " " + maxY);

                int numPoints = (int)Random.Range(4, 5);
                initialPoints = new Vector3[numPoints];

                for (int i = 0; i < numPoints; i++)
                {
                    int x = (int)Random.Range(-maxX, maxX);
                    int y = (int)Random.Range(-maxY, maxY);
                    initialPoints[i] = new Vector3(x, y, -1);
                }
            }

            // Adjust the camera to the loose bounding box
            {
                float minX = initialPoints[0].x;
                float maxX = initialPoints[0].x;
                float minY = initialPoints[0].y;
                float maxY = initialPoints[0].y;
                for (int i = 1; i < initialPoints.Length; i++)
                {
                    minX = Mathf.Min(initialPoints[i].x, minX);
                    maxX = Mathf.Max(initialPoints[i].x, maxX);
                    minY = Mathf.Min(initialPoints[i].y, minY);
                    maxY = Mathf.Max(initialPoints[i].y, maxY);
                }

                const float buffer = 3f;

                float centerX = ((maxX + minX) / 2);
                float centerY = ((maxY + minY) / 2);

                Bounds targetBounds = new Bounds(new Vector3(centerX, centerY, -10), new Vector3((maxX - minX) + buffer, (maxY - minY) + buffer, 0f));
                float targetRatio = targetBounds.size.x / targetBounds.size.y;

                if (Camera.main.aspect >= targetRatio)
                {
                    Camera.main.orthographicSize = targetBounds.size.y / 2;
                }
                else
                {
                    float differenceInSize = targetRatio / Camera.main.aspect;
                    Camera.main.orthographicSize = targetBounds.size.y / 2 * differenceInSize;
                }

                Camera.main.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, targetBounds.center.z);

            }

            float linePosZ = 1f;
            for (int x = 0; x < initialPoints.Length - 1; x++)
            {
                LineAndSpheres ls = new LineAndSpheres();

                Vector3[] childPositions = new Vector3[initialPoints.Length - x];
                for (int y = 0; y < childPositions.Length; y++)
                {
                    childPositions[y] = initialPoints[y];
                    childPositions[y].z = linePosZ;
                }

                // Move the lines ahead of each other as we go down in degrees
                linePosZ -= 0.1f;

                // Get a color for the segment and spheres
                Color c = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

                for (int y = 0; y < childPositions.Length - 1; y++)
                {
                    Vector3 spherePos = childPositions[y];
                    spherePos.z = -1;
                    GameObject sphere = Instantiate<GameObject>(spherePrefab, spherePos, Quaternion.identity);
                    sphere.GetComponent<MeshRenderer>().materials[0].color = c;
                    sphere.name = ("Sphere " + x + " " + y);
                    ls.spheres.Add(sphere);
                    lastSphere = sphere;
                }

                // Darken the color a bit
                float h, s, v;
                Color.RGBToHSV(c, out h, out s, out v);
                c = Color.HSVToRGB(h, s - 0.2f, v - 0.2f);

                LineRenderer childRenderer = Instantiate<LineRenderer>(linePrefab, Vector3.zero, Quaternion.identity);
                childRenderer.name = ("Line " + x);
                childRenderer.gameObject.GetComponent<Bezier>().isRoot = false;
                childRenderer.gameObject.GetComponent<Bezier>().linePrefab = linePrefab;
                childRenderer.gameObject.GetComponent<Bezier>().spherePrefab = spherePrefab;
                childRenderer.positionCount = childPositions.Length;
                childRenderer.SetPositions(childPositions);
                Gradient g2 = new Gradient();
                g2.colorKeys = new GradientColorKey[] { new GradientColorKey(c, 0f) };
                childRenderer.colorGradient = g2;
                ls.lineRenderer = childRenderer;

                lines.Add(ls);
            }

            lastSpherePos = lastSphere.transform.position;
        }
    }

    void FixedUpdate()
    {
        if (isRoot)
        {
            if (dir)
            {
                t1 += t1_step * Time.deltaTime;
                if (t1 >= 1)
                {
                    dir = false;
                    firstLoop = false;
                }
            }
            // else
            // {
            //     t1 -= t1_step * Time.deltaTime;
            //     if (t1 <= 0)
            //     {
            //         dir = true;
            //     }
            // }

            Vector3[] segmentPositions = null;
            foreach (LineAndSpheres tuple in lines)
            {
                LineRenderer lr = tuple.lineRenderer;

                if (segmentPositions != null)
                {
                    lr.positionCount = segmentPositions.Length;
                    lr.SetPositions(segmentPositions);
                }

                // For each line segment, we reference one sphere that moves along its two points
                segmentPositions = new Vector3[lr.positionCount - 1];
                for (int x = 0; x < lr.positionCount - 1; x++)
                {
                    Vector3 spherePos = new Vector3(Mathf.Lerp(lr.GetPosition(x).x,
                                                                lr.GetPosition(x + 1).x, t1),
                                                    Mathf.Lerp(lr.GetPosition(x).y,
                                                                lr.GetPosition(x + 1).y, t1), -1);
                    tuple.spheres[x].transform.position = spherePos;
                    lastSpherePos = spherePos;

                    // Render lines behind spheres
                    spherePos.z = 1;
                    segmentPositions[x] = spherePos;
                }
            }

            if (firstLoop && firstLoopCounter % 10 == 0)
            {
                lastSpherePos.z = -2;
                GameObject sphere = Instantiate<GameObject>(spherePrefab, lastSpherePos, Quaternion.identity);
                sphere.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                bezierGraphic.Add(sphere);
            }
            else if (firstLoop)
            {
                firstLoopCounter++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
