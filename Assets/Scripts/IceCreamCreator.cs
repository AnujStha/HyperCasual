using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCreamCreator : MonoBehaviour
{
    private Coroutine _creator;
    [SerializeField] private int ringVertices;
    [SerializeField] private float ringSpawnRate;
    [SerializeField] private float ringRadius;
    [SerializeField] private Transform createPosition;
    [SerializeField] private Material material;
    [SerializeField] private float uvMapSpacing;
    [SerializeField] private float creamMoveSpeed;
    private bool _create;
    private const float VerticesSqThreshold = .01f;

    private void Start()
    {
        _creator = StartCoroutine(Make());
    }

    IEnumerator Make()
    {
        var currentPiece = new GameObject();
        var currentMeshFilter = currentPiece.AddComponent<MeshFilter>();
        var meshRenderer = currentPiece.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        var currentMesh = new Mesh();
        currentMeshFilter.mesh = currentMesh;
        float currentUvPosition=0;
        // for moving keep target position in dictionary
        Dictionary<int, Vector3> targetValues=new Dictionary<int, Vector3>();
        StartCoroutine(CreamAnimator(currentMesh, targetValues));

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
        
        WaitForSeconds waiter = new WaitForSeconds(1/ringSpawnRate);
        while (true)
        {
            var createPoint = createPosition.position;
            int startingVertexIndex = currentMesh.vertices.Length;
            
            // NOTE: +1 vertices because combining last vertices to first vertex in unity causes texture to wrap backward
            // so we create extra vertex that is also start
            Vector3[] newPoints = new Vector3[startingVertexIndex+ringVertices+1];
            Vector3[] newNormals = new Vector3[startingVertexIndex+ringVertices+1];
            Vector2[] newUv = new Vector2[startingVertexIndex+ringVertices+1];
            Array.Copy(currentMesh.vertices,newPoints,startingVertexIndex);
            Array.Copy(currentMesh.normals,newNormals,startingVertexIndex);
            Array.Copy(currentMesh.uv,newUv,startingVertexIndex);

            // create points
            for (int i = 0; i < ringVertices+1; i++)
            {
                float factor = i / (float)ringVertices;
                float angle = factor* MathF.PI*2;
                Vector3 point = new Vector3(ringRadius * Mathf.Cos(angle) + createPoint.x, createPoint.y,
                    ringRadius * Mathf.Sin(angle) + createPoint.z);
                newPoints[startingVertexIndex + i] = point;
                newNormals[startingVertexIndex + i] = point - createPoint;// normals are pointing from center to created point
                newUv[startingVertexIndex + i] = new Vector2(factor / 2, currentUvPosition);
                
                // set end point
                targetValues.Add(startingVertexIndex+i,createPoint-Vector3.up*.4f);
            }
            currentUvPosition += uvMapSpacing;// move uv up. uv will repeat itself
            currentMesh.vertices = newPoints;
            currentMesh.normals = newNormals;
            currentMesh.uv = newUv;

            if (startingVertexIndex==0)
            {
                // if first point don't create triangle
                // TODO: close starting point
                yield return waiter;
                continue;
            }

            // create triangles
            int startingTriangleIndex = currentMesh.triangles.Length;
            int[] newTriangles = new int[startingTriangleIndex + (ringVertices) * 2*3];// each new vertex creates two triangle and a triangle needs 3 index.
            Array.Copy(currentMesh.triangles,newTriangles,startingTriangleIndex);

            for (int i = 0; i < ringVertices; i++)
            {
                int[] quadPoints = new int[4];
                int pointIndex = startingVertexIndex + i;

                quadPoints[0] = pointIndex;
                quadPoints[1] = pointIndex+1;
                quadPoints[2] = pointIndex-ringVertices;
                quadPoints[3] = pointIndex-ringVertices-1;

                newTriangles[startingTriangleIndex + i * 6] = quadPoints[0];
                newTriangles[startingTriangleIndex + i * 6+1] = quadPoints[2];
                newTriangles[startingTriangleIndex + i * 6+2] = quadPoints[3];
                newTriangles[startingTriangleIndex + i * 6+3] = quadPoints[0];
                newTriangles[startingTriangleIndex + i * 6+4] = quadPoints[1];
                newTriangles[startingTriangleIndex + i * 6+5] = quadPoints[2];
            }

            currentMesh.triangles = newTriangles;
            yield return waiter;
        }
    }

    IEnumerator CreamAnimator(Mesh movingMesh, Dictionary<int,Vector3> indexToTarget)
    {
        yield return null;// wait a frame before starting
        
        while (indexToTarget!=null&&indexToTarget.Count>0)
        {
            Vector3[] newPositions=new Vector3[movingMesh.vertices.Length];
            Array.Copy(movingMesh.vertices,newPositions, movingMesh.vertices.Length);
            List<int> removingArray = new List<int>();
            foreach (var creamVertex in indexToTarget)
            {
                // calculate normalized direction to travel and move towards it with 'creamMoveSpeed' speed
                newPositions[creamVertex.Key] += (creamVertex.Value - newPositions[creamVertex.Key]).normalized * (creamMoveSpeed * Time.deltaTime);
                
                if (Vector3.SqrMagnitude(creamVertex.Value-newPositions[creamVertex.Key])<VerticesSqThreshold)
                {
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
}
