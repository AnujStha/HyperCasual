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
        WaitForSeconds wait = new WaitForSeconds(1 / ringSpawnRate);
        do
        {
            // time to create ring
            CreateRing(currentMesh, targetValues, currentUvPosition);
            currentUvPosition += uvMapSpacing; // move uv up. uv will repeat itself
            _currentY += fillRate;
            yield return wait;
        } while (thisMachineState == _machineState);

        if (currentMesh.vertices.Length<=ringVertices+1)
        {
            // only one ring is created so create one more
            CreateRing(currentMesh, targetValues, currentUvPosition);
        }
    }

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
