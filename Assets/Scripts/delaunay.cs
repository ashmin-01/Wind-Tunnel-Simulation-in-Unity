using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using UnityEngine;

public class DelaunayTriangulationExample : MonoBehaviour
{
    public Vector3 modelPosition = Vector3.zero; // Initial position of the model
    public Vector3 modelScale = Vector3.one; // Initial scale of the model
    [Range(0.1f, 1f)]
    public float simplificationFactor = 0.5f; // Factor to simplify the triangulation

    private Vector3[] triangleVertices; // Array to store the vertices of the triangles

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        // Draw the simplified triangles in the scene
        if (triangleVertices != null)
        {
            for (int i = 0; i < triangleVertices.Length; i += 3)
            {
                Gizmos.DrawLine(triangleVertices[i], triangleVertices[i + 1]);
                Gizmos.DrawLine(triangleVertices[i + 1], triangleVertices[i + 2]);
                Gizmos.DrawLine(triangleVertices[i + 2], triangleVertices[i]);
            }
        }
    }

    public Vector3[] SimplifyTriangles(Vector3[] triangles, float factor)
    {
        // Ensure the length of the array is a multiple of 3
        if (triangles.Length % 3 != 0)
        {
            Debug.LogError("The length of the triangles array is not a multiple of 3.");
            return triangles; // Return the original array to avoid further errors
        }

        Dictionary<Vector3, Vector3> vertexClusters = new Dictionary<Vector3, Vector3>();
        List<Vector3> uniqueVertices = triangles.Distinct().ToList(); // Get unique vertices from the triangles
        int targetVertexCount = Mathf.FloorToInt(uniqueVertices.Count * factor); // Calculate the target number of vertices after simplification

        // Create a grid for clustering
        float gridSize = Mathf.Sqrt(uniqueVertices.Count / (float)targetVertexCount);
        foreach (var vertex in uniqueVertices)
        {
            // Calculate grid position for the vertex
            Vector3 gridPos = new Vector3(
                Mathf.Floor(vertex.x / gridSize),
                Mathf.Floor(vertex.y / gridSize),
                Mathf.Floor(vertex.z / gridSize)
            );

            // Add vertex to cluster or merge with existing cluster
            if (!vertexClusters.ContainsKey(gridPos))
            {
                vertexClusters[gridPos] = vertex;
            }
            else
            {
                vertexClusters[gridPos] = (vertexClusters[gridPos] + vertex) / 2;
            }
        }

        // Replace vertices in triangles with their cluster centroids
        List<Vector3> simplifiedTriangles = new List<Vector3>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3[] newTriangle = new Vector3[3];
            for (int j = 0; j < 3; j++)
            {
                // Calculate grid position for the vertex
                Vector3 gridPos = new Vector3(
                    Mathf.Floor(triangles[i + j].x / gridSize),
                    Mathf.Floor(triangles[i + j].y / gridSize),
                    Mathf.Floor(triangles[i + j].z / gridSize)
                );
                newTriangle[j] = vertexClusters[gridPos]; // Replace vertex with cluster centroid
            }

            // Avoid adding degenerate triangles
            if (newTriangle[0] != newTriangle[1] && newTriangle[1] != newTriangle[2] && newTriangle[2] != newTriangle[0])
            {
                simplifiedTriangles.AddRange(newTriangle);
            }
        }

        return simplifiedTriangles.ToArray(); // Return the simplified triangles
    }

    Mesh GetMeshFromModel(GameObject obj)
    {
        // Get the Mesh from a MeshFilter component if available
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            return meshFilter.sharedMesh; // Return mesh from MeshFilter
        }

        // Get the Mesh from a SkinnedMeshRenderer component if available
        SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            return skinnedMeshRenderer.sharedMesh; // Return mesh from SkinnedMeshRenderer
        }

        // Recursively search for a mesh in the child objects
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            Mesh mesh = GetMeshFromModel(obj.transform.GetChild(i).gameObject);
            if (mesh != null)
            {
                return mesh;
            }
        }

        return null; // Return null if no mesh is found
    }

    public Vector3[] GetVerticesAfterTriangulate(GameObject model)
    {
        Debug.Log("Delaunay Triangulation Example started.");

        // Get the mesh from the model
        Mesh mesh = GetMeshFromModel(model);
        if (mesh == null)
        {
            Debug.LogError("No MeshFilter or SkinnedMeshRenderer found on the model.");
            return null;
        }

        Debug.Log($"Mesh found with {mesh.vertexCount} vertices.");

        // Convert mesh vertices to a list of Vector3 points
        Vector3[] vertices = mesh.vertices;
        List<Vector3> points = vertices.ToList();

        if (points.Count == 0)
        {
            Debug.LogError("No vertices found in the mesh.");
            return null;
        }

        Debug.Log($"Number of vertices to process: {points.Count}");

        // Transform vertices based on the model's position and scale
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = Vector3.Scale(points[i], modelScale) + modelPosition;
        }

        // Perform Delaunay triangulation
        double planeDistanceTolerance = 0.001;
        var delaunayTriangulation = DelaunayTriangulation<MyVertex, MyCell>.Create(
            points.Select(v => new MyVertex(v.x, v.y, v.z)).ToList(),
            planeDistanceTolerance
        );

        Debug.Log($"Number of cells created: {delaunayTriangulation.Cells.Count()}");

        // Convert the triangulation result to a flat array of Vector3
        List<Vector3> tempTriangles = new List<Vector3>();
        foreach (var cell in delaunayTriangulation.Cells)
        {
            foreach (var vertex in cell.Vertices)
            {
                tempTriangles.Add(new Vector3((float)vertex.Position[0], (float)vertex.Position[1], (float)vertex.Position[2]));
            }
        }

        // Store the triangulated vertices
        triangleVertices = tempTriangles.ToArray();

        // Ensure the length of the array is a multiple of 3
        if (triangleVertices.Length % 3 != 0)
        {
            int newSize = (triangleVertices.Length / 3) * 3;
            Array.Resize(ref triangleVertices, newSize);
            Debug.LogWarning($"Trimmed the triangleVertices array to a multiple of 3. New length: {triangleVertices.Length}");
        }

        // Print the triangleVertices array in groups of 3
        for (int i = 0; i < triangleVertices.Length; i += 3)
        {
            Debug.Log($"Triangle {i / 3}: [{triangleVertices[i]}, {triangleVertices[i + 1]}, {triangleVertices[i + 2]}]");
        }

        Debug.Log($"Number of triangles before simplification: {triangleVertices.Length / 3}");
        Debug.Log("Delaunay Triangulation completed.");

        // Simplify the triangles using vertex clustering
        triangleVertices = SimplifyTriangles(triangleVertices, simplificationFactor);

        Debug.Log("Simplification of triangles completed.");
        Debug.Log($"Number of triangles after simplification: {triangleVertices.Length / 3}");

        return triangleVertices;
    }

    public class MyVertex : IVertex
    {
        public double[] Position { get; set; } // Position of the vertex in 3D space

        public MyVertex(double x, double y, double z)
        {
            Position = new double[] { x, y, z }; // Initialize the position
        }
    }

    public class MyCell : TriangulationCell<MyVertex, MyCell> { }
}
