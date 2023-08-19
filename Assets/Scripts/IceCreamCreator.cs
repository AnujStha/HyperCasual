using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCreamCreator : MonoBehaviour
{
    [SerializeField] private int ringVertices;
    [SerializeField] private float ringSpawnRate;
    [SerializeField] private float ringRadius;
    [SerializeField] private Transform createPosition;
    [SerializeField] private Transform center;
    [SerializeField] private List<CreamProperties> creamProperties;
    [SerializeField] private float uvMapSpacing;
    [SerializeField] private float creamMoveSpeed;
    [SerializeField] private float landingPoint;
    [SerializeField] private float fillRateY;
    private MachineState _machineState;
    private float _currentY;
    private bool _create;
    // private const float VerticesSqThreshold = .01f;

    private void Start()
    {
        _currentY = landingPoint;
    }

    public void ChangeState(MachineState machineState)
    {
        if (machineState==_machineState)
        {
            return;
        }
        if (_machineState==MachineState.Filled)
        {
            return;
        }
        _machineState = machineState;

        foreach (CreamProperties creamProperty in creamProperties)
        {
            if (machineState==creamProperty.machineState)
            {
                StartCoroutine(Make(creamProperty));
                return;
            }
        }
    }

    public MachineState GetState()
    {
        return _machineState;
    }

    /// <summary>
    /// It will make a mesh. mesh will consists of body, start and end circle
    /// a mesh will have only one material assigned to it
    /// once stopped, this cannot be started again and should be created new
    /// </summary>
    /// <param name="creamProperty">type of mesh to create</param>
    IEnumerator Make(CreamProperties creamProperty)
    {
        MachineState thisMachineState = creamProperty.machineState;
        var currentPiece = new GameObject();
        var currentMeshFilter = currentPiece.AddComponent<MeshFilter>();
        var meshRenderer = currentPiece.AddComponent<MeshRenderer>();
        meshRenderer.material = creamProperty.machineMaterial;
        var currentMesh = new Mesh();
        currentMeshFilter.mesh = currentMesh;
        float currentUvPosition=0;
        // for moving keep target position in dictionary
        Dictionary<int, Vector3> targetValues=new Dictionary<int, Vector3>();
        StartCoroutine(CreamAnimator(currentMesh, targetValues));
        float fillRate = fillRateY/ringSpawnRate;// (1/ringSpawnRate) gives delta time
        
        CreateCircle(currentMesh,targetValues,true);
        
        WaitForSeconds wait = new WaitForSeconds(1 / ringSpawnRate);
        do
        {
            // time to create ring
            CreateRing(currentMesh, targetValues, currentUvPosition);
            currentUvPosition += uvMapSpacing; // move uv up. uv will repeat itself
            _currentY += fillRate;
            yield return wait;
        } while (thisMachineState == _machineState);
        
        CreateRing(currentMesh, targetValues, currentUvPosition);// create one more ring so it will be connected to end
        CreateCircle(currentMesh,targetValues,false);
    }
    
    /// <summary>
    /// create circles at end and start of mesh
    /// </summary>
    /// <param name="currentMesh">mesh that is being created</param>
    /// <param name="targetValues">landing value</param>
    /// <param name="isStarting">is start circle or ending circle</param>
    private void CreateCircle(Mesh currentMesh,Dictionary<int,Vector3> targetValues, bool isStarting)
    {
        // circle of 6 point will be in this shape
        
        /*
                3---2 
               / \ / \
              4---0---1
               \ / \ /
                5---6
        */

        // start and end circle
        var createPoint = createPosition.position;
        int startingVertexIndex = currentMesh.vertices.Length;
        Quaternion rotation = Quaternion.AngleAxis(90, createPoint - center.position);// rotating axis when calculating landing position

        // NOTE: +1 vertices because of center vertex
        Vector3[] newPoints = new Vector3[startingVertexIndex + ringVertices + 1];
        Vector3[] newNormals = new Vector3[startingVertexIndex + ringVertices + 1];
        Vector2[] newUv = new Vector2[startingVertexIndex + ringVertices + 1];
        Array.Copy(currentMesh.vertices, newPoints, startingVertexIndex);
        Array.Copy(currentMesh.normals, newNormals, startingVertexIndex);
        Array.Copy(currentMesh.uv, newUv, startingVertexIndex);

        // create center point
        newPoints[startingVertexIndex] = createPoint;
        newNormals[startingVertexIndex] = isStarting ? Vector3.down : Vector3.up;
        newUv[startingVertexIndex] = isStarting ? new Vector2(.75f, .75f) : new Vector2(.74f, .25f);
        targetValues.Add(startingVertexIndex,new Vector3(createPoint.x,_currentY,createPoint.z));
        startingVertexIndex++;// created center vertices. so adding 1 so that ring will be created from here
        
        // create points
        for (int i = 0; i < ringVertices; i++)
        {
            float factor = i / (float)ringVertices;
            float angle = factor * MathF.PI * 2;
            Vector3 point = new Vector3(ringRadius * Mathf.Cos(angle) + createPoint.x, createPoint.y,
                ringRadius * Mathf.Sin(angle) + createPoint.z);
            newPoints[startingVertexIndex + i] = point;
            newNormals[startingVertexIndex + i] =
                point - createPoint; // normals are pointing from center to created point
            newUv[startingVertexIndex + i] = new Vector2(MathF.Cos(angle),MathF.Sin(angle))*.25f+
                                             (isStarting?new Vector2(.75f,.75f):new Vector2(.74f,.25f));// coordinates are at first quadrant for starting and 4th quadrant for ending

            // set end point
            var centerPosition = center.position;
            centerPosition.y = _currentY;// because we have custom landing point
            Vector3 pointLand = point;
            pointLand.y = _currentY;
            Vector3 endPosition = rotation * (pointLand-centerPosition)+centerPosition;// transform to center, rotate and transform back
            targetValues.Add(startingVertexIndex + i, endPosition);
        }
        currentMesh.vertices = newPoints;
        currentMesh.normals = newNormals;
        currentMesh.uv = newUv;

        // create triangles
        int startingTriangleIndex = currentMesh.triangles.Length;
        int[] newTriangles =
            new int[startingTriangleIndex +ringVertices*3]; // each new vertex creates a triangle, a triangle requires 3 vertices
        Array.Copy(currentMesh.triangles, newTriangles, startingTriangleIndex);

        int c = startingVertexIndex - 1;//center vertex incex
        if (isStarting)// clock wise or anticockwise for starting and ending
        {
            for (int i = 0; i < ringVertices; i++)
            {
                newTriangles[i*3 + startingTriangleIndex] = c;
                newTriangles[i*3 + startingTriangleIndex+1] = startingVertexIndex + i;
                newTriangles[i*3 + startingTriangleIndex+2] = i==(ringVertices-1)?startingVertexIndex: startingVertexIndex + i+1;// if final vertex, loop back
            }
        }
        else
        {
            for (int i = 0; i < ringVertices; i++)
            {
                newTriangles[i*3 + startingTriangleIndex+2] = startingVertexIndex + i;
                newTriangles[i*3 + startingTriangleIndex+1] = i==(ringVertices-1)?startingVertexIndex: startingVertexIndex + i+1;// if final vertex, loop back
                newTriangles[i*3 + startingTriangleIndex] = c;
            }
        }

        currentMesh.triangles = newTriangles;
    }

    /// <summary>
    /// Creates a ring for body of strip
    /// </summary>
    /// <param name="currentMesh">mesh to append to</param>
    /// <param name="targetValues">landing position</param>
    /// <param name="currentUvPosition">current uv mapped y position of latest vertex</param>
    private void CreateRing(Mesh currentMesh,Dictionary<int,Vector3> targetValues,float currentUvPosition)
    {
        // a strip looks like this
        // number of vertices in ring = 3
                
        /*
        ^   ^   ^   ^   <- other rings will be continuously added
        :   :   :   :
        4---5---6---7   <- ring 2
        | \ | \ | \ |    
        0---1---2---3   <- ring 1
        */
                
        // if we creating ring 2, create first quad as [4,5,1,0] (creating clockwise)
        // two triangles of this quad are [4,1,0] and [4,5,1] (creating clockwise is important)
        // Note: in cylinder first and last lies in same position and are not connected. not filling gap because it causes issue while texturing
        
        var createPoint = createPosition.position;
        int startingVertexIndex = currentMesh.vertices.Length;
        Quaternion rotation = Quaternion.AngleAxis(90, createPoint - center.position);// rotating axis when calculating landing position

        // NOTE: +1 vertices because combining last vertices to first vertex in unity causes texture to wrap backward
        // so we create extra vertex that is also start
        Vector3[] newPoints = new Vector3[startingVertexIndex + ringVertices + 1];
        Vector3[] newNormals = new Vector3[startingVertexIndex + ringVertices + 1];
        Vector2[] newUv = new Vector2[startingVertexIndex + ringVertices + 1];
        Array.Copy(currentMesh.vertices, newPoints, startingVertexIndex);
        Array.Copy(currentMesh.normals, newNormals, startingVertexIndex);
        Array.Copy(currentMesh.uv, newUv, startingVertexIndex);

        // create points
        for (int i = 0; i < ringVertices + 1; i++)
        {
            float factor = i / (float)ringVertices;
            float angle = factor * MathF.PI * 2;
            Vector3 point = new Vector3(ringRadius * Mathf.Cos(angle) + createPoint.x, createPoint.y,
                ringRadius * Mathf.Sin(angle) + createPoint.z);
            newPoints[startingVertexIndex + i] = point;
            newNormals[startingVertexIndex + i] =
                point - createPoint; // normals are pointing from center to created point
            newUv[startingVertexIndex + i] = new Vector2(factor / 2, currentUvPosition);

            // set end point
            var centerPosition = center.position;
            centerPosition.y = _currentY;// because we have custom landing point
            Vector3 pointLand = point;
            pointLand.y = _currentY;
            Vector3 endPosition = rotation * (pointLand-centerPosition)+centerPosition;// transform to center, rotate and transform back
            targetValues.Add(startingVertexIndex + i, endPosition);
        }

        currentMesh.vertices = newPoints;
        currentMesh.normals = newNormals;
        currentMesh.uv = newUv;

        if (startingVertexIndex == 0)
        {
            // if first point don't create triangle
            // TODO: close starting point
            return;
        }

        // create triangles
        int startingTriangleIndex = currentMesh.triangles.Length;
        int[] newTriangles =
            new int[startingTriangleIndex +
                    (ringVertices) * 2 * 3]; // each new vertex creates two triangle and a triangle needs 3 index.
        Array.Copy(currentMesh.triangles, newTriangles, startingTriangleIndex);

        for (int i = 0; i < ringVertices; i++)
        {
            int[] quadPoints = new int[4];
            int pointIndex = startingVertexIndex + i;

            quadPoints[0] = pointIndex;
            quadPoints[1] = pointIndex + 1;
            quadPoints[2] = pointIndex - ringVertices;
            quadPoints[3] = pointIndex - ringVertices - 1;

            newTriangles[startingTriangleIndex + i * 6] = quadPoints[0];
            newTriangles[startingTriangleIndex + i * 6 + 1] = quadPoints[2];
            newTriangles[startingTriangleIndex + i * 6 + 2] = quadPoints[3];
            newTriangles[startingTriangleIndex + i * 6 + 3] = quadPoints[0];
            newTriangles[startingTriangleIndex + i * 6 + 4] = quadPoints[1];
            newTriangles[startingTriangleIndex + i * 6 + 5] = quadPoints[2];
        }

        currentMesh.triangles = newTriangles;
    }

    /// <summary>
    /// animates vertices to end point.
    /// one mesh will trigger one coroutine
    /// stops when there are no vertices to move
    /// </summary>
    /// <param name="movingMesh">reference of mesh to move</param>
    /// <param name="indexToTarget">dictionary containing index of vertices mapped with landing position</param>
    private IEnumerator CreamAnimator(Mesh movingMesh, Dictionary<int,Vector3> indexToTarget)
    {
        yield return null;// wait a frame before starting
        
        while (indexToTarget!=null&&indexToTarget.Count>0)
        {
            Vector3[] newPositions=new Vector3[movingMesh.vertices.Length];
            Array.Copy(movingMesh.vertices,newPositions, movingMesh.vertices.Length);
            List<int> removingArray = new List<int>();
            foreach (var creamVertex in indexToTarget)
            {
                // // calculate normalized direction to travel and move towards it with 'creamMoveSpeed' speed
                // newPositions[creamVertex.Key] += (creamVertex.Value - newPositions[creamVertex.Key]).normalized * (creamMoveSpeed * Time.deltaTime);
                
                // first drop down, when reached height, align
                newPositions[creamVertex.Key].y -= creamMoveSpeed*Time.deltaTime;
                
                if (newPositions[creamVertex.Key].y<creamVertex.Value.y)
                {
                    newPositions[creamVertex.Key] = creamVertex.Value;
                    removingArray.Add(creamVertex.Key);
                }
            }
            movingMesh.vertices = newPositions;
            foreach (int i in removingArray)
            {
                indexToTarget.Remove(i);
            }

            yield return null;
        }
    }

    [Serializable]
    public class CreamProperties
    {
        public MachineState machineState;
        public Material machineMaterial;
    }

    public enum MachineState
    {
        None,
        Vanilla,
        Strawberry,
        Chocolate,
        VanillaAndStrawberry,
        VanillaAndChocolate,
        StrawberryAndChocolate,
        Filled
    }
}
