using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System;

public class Simulation3D : MonoBehaviour
{
    //show Path mayar

    public bool showParticles = false;

    public Color lineColor = Color.green;  // Color of the lines
    public int maxPositions = 50;  // Maximum number of positions to store
    

    private List<Queue<float3>> positionQueues;  // List of queues to store the positions of each particle
    private List<LineRenderer> lineRenderers;  // List of line renderers for each particle
    //


    //mayar model

    private List<Vector3> modelVertices = new List<Vector3>();
    float3[] positionspointArray;
    [SerializeField] private GameObject model;
    Mesh cubeMesh;
    Vector3 positionsOfModel;
    Vector3 scaleOfModel;
    Quaternion rotationOfModel;
    private  List<int> storedVertexIndices = new List<int>();
    private Vector3[] vector3MeshVertices;
    private float3[] float3MeshVertices;
    private bool isCollision;
    public float sphereRadius = 5f;
    private int[] particlesShowArray;
    //


    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity = -10;

    // Wind Power
    public float3 windDirection = new(1, 0, 0);
    public float windStrength = 0.01f;

    [Range(0, 1)] 
    public float collisionDamping = 0.05f;

    public float smoothingRadius = 0.2f;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float viscosityStrength;

    [Header("References")]
    public ComputeShader compute;
    public Spawner3D spawner;
    public ParticleDisplay3D display;
    public Transform floorDisplay;

    // Buffers
    public ComputeBuffer PositionBuffer { get; private set; }
    public ComputeBuffer particlesShow { get; private set; }
    public ComputeBuffer VelocityBuffer { get; private set; }
    public ComputeBuffer DensityBuffer { get; private set; }
    public ComputeBuffer predictedPositionsBuffer;
    public ComputeBuffer trianglesBuffer { get; private set; }
    ComputeBuffer spatialIndices;
    ComputeBuffer spatialOffsets;

    // Kernel IDs
    const int externalForcesKernel = 0;
    const int spatialHashKernel = 1;
    const int densityKernel = 2;
    const int pressureKernel = 3;
    const int viscosityKernel = 4;
    const int updatePositionsKernel = 5;
    
    GPUSort gpuSort;

    // State
    bool isPaused;
    bool pauseNextFrame;
    Spawner3D.SpawnData spawnData;
    int numParticles;
    void Start()
    {
        
        Debug.Log("Controls: Space = Play/Pause, R = Reset");
        Debug.Log("Use transform tool in scene to scale/rotate simulation bounding box.");

        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;

        spawnData = spawner.GetSpawnData();

        // Create buffers
        numParticles = spawnData.points.Length;
        PositionBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        predictedPositionsBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        VelocityBuffer = ComputeHelper.CreateStructuredBuffer<float3>(numParticles);
        DensityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        spatialIndices = ComputeHelper.CreateStructuredBuffer<uint3>(numParticles);
        spatialOffsets = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);
        particlesShow = ComputeHelper.CreateStructuredBuffer<int>(numParticles);

        // Set buffer data
        SetInitialBufferData(spawnData);

        // Init compute
        ComputeHelper.SetBuffer(compute, PositionBuffer, "Positions", externalForcesKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, predictedPositionsBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, DensityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, VelocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionsKernel);

        compute.SetInt("numParticles", PositionBuffer.count);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);


        // Init display
        display.Init(this);

        //Mayar var for collision
        positionspointArray = new float3[numParticles];
        cubeMesh = model.GetComponent<MeshFilter>().mesh;
        positionsOfModel = model.GetComponent<Transform>().position;
        scaleOfModel = model.GetComponent<Transform>().localScale;
        rotationOfModel = model.GetComponent<Transform>().localRotation;
        storedVertexIndices = GetTrianglesVertexIndices(cubeMesh);
        //here 
        //comment this rima 
        vector3MeshVertices = GetStoredIndicesFromMesh(cubeMesh);

        //uncomment this rima

        //DelaunayTriangulationExample example = new DelaunayTriangulationExample();
        //vector3MeshVertices = example.GetVerticesAfterTriangulate(model);
         float3MeshVertices = ConvertVector3ArrayToFloat3Array(vector3MeshVertices);


        trianglesBuffer = ComputeHelper.CreateStructuredBuffer<float3>(float3MeshVertices.Length);
          
        trianglesBuffer.SetData(float3MeshVertices);
        ComputeHelper.SetBuffer(compute, trianglesBuffer, "Triangles", externalForcesKernel, updatePositionsKernel);
        compute.SetFloat("sphereRadius", sphereRadius);
        compute.SetInt("numTriangles", float3MeshVertices.Length );
        particlesShowArray =new int[numParticles];
        Array.Fill(particlesShowArray , 1);
        particlesShow.SetData(particlesShowArray);
        ComputeHelper.SetBuffer(compute, particlesShow, "ParticlesShow", externalForcesKernel , updatePositionsKernel);

        //Debug.Log("befoooor");
        positionspointArray = GetDataPositionsPoint(PositionBuffer, numParticles);
        modelVertices = ThreeDSReader.ReadVertices("C:\\Users\\mayar\\Documents\\GitHub\\Wind-Tunnel-Simulation-in-Unity\\Assets\\models\\SUV_Car\\Models\\1.3DS");
       foreach (Vector3 vertex in modelVertices)
        {
            Debug.Log(vertex);
        }
        // drow path 
        // Initialize the lists
        positionQueues = new List<Queue<float3>>();
        lineRenderers = new List<LineRenderer>();

        // Initialize the queues and line renderers for each particle
        for (int i = 0; i < positionspointArray.Length / 20; i++)
        {
            positionQueues.Add(new Queue<float3>());

            LineRenderer lineRenderer = new GameObject("LineRenderer_" + i).AddComponent<LineRenderer>();
            lineRenderer.transform.parent = this.transform;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 0;

            lineRenderers.Add(lineRenderer);
        }
        //

    }

    void FixedUpdate()
    {
        // Run simulation if in fixed timestep mode
        if (fixedTimeStep)
        {
            RunSimulationFrame(Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        // Run simulation if not in fixed timestep mode
        // (skip running for first few frames as timestep can be a lot higher than usual)
        if (!fixedTimeStep && Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (pauseNextFrame)
        {
            isPaused = true;
            pauseNextFrame = false;
        }
        floorDisplay.transform.localScale = new Vector3(1, 1 / transform.localScale.y * 0.1f, 1);

        HandleInput();
        //mayar var for Collision
        positionspointArray = GetDataPositionsPoint(PositionBuffer, numParticles);
        for (int i = 0; i < positionspointArray.Length / 20; i++)
        {
            UpdateLineRenderer(i, positionspointArray[i]);
        }
        //Debug.Log(positionspointArray[1000]);
        //Debug.Log(float3MeshVertices[12]);
        //Debug.Log(float3MeshVertices[13]);
        //Debug.Log(float3MeshVertices[14]);


        //isCollision = SphereTriangleCollision.IsSphereIntersecting(positionspointArray[0], 5f, float3MeshVertices);
        //if(isCollision)
        //{
        //    Debug.Log("is collision");
        //}
        //if(!isCollision)
        //{
        //    Debug.Log("is not collsion");
        //}



    }

    void RunSimulationFrame(float frameTime)
    {
        if (!isPaused)
        {
            float timeStep = frameTime / iterationsPerFrame * timeScale;

            UpdateSettings(timeStep);

            for (int i = 0; i < iterationsPerFrame; i++)
            {
                RunSimulationStep();
                SimulationStepCompleted?.Invoke();
            }
        }
    }

    void RunSimulationStep()
    {
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: externalForcesKernel);
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: spatialHashKernel);
        gpuSort.SortAndCalculateOffsets();
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: densityKernel);
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: pressureKernel);
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: viscosityKernel);
        ComputeHelper.Dispatch(compute, PositionBuffer.count, kernelIndex: updatePositionsKernel);


    }

    void UpdateSettings(float deltaTime)
    {
        Vector3 simBoundsSize = transform.localScale;
        Vector3 simBoundsCentre = transform.position;
        Vector3 windDirectionVector = windDirection;

        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        
        // Add wind power to Compute Shader 
        compute.SetVector("windDirection", windDirectionVector);
        compute.SetFloat("windStrength", windStrength);

        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetFloat("smoothingRadius", smoothingRadius);
        compute.SetFloat("targetDensity", targetDensity);
        compute.SetFloat("pressureMultiplier", pressureMultiplier);
        compute.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
        compute.SetFloat("viscosityStrength", viscosityStrength);
        compute.SetVector("boundsSize", simBoundsSize);
        compute.SetVector("centre", simBoundsCentre);

        compute.SetMatrix("localToWorld", transform.localToWorldMatrix);
        compute.SetMatrix("worldToLocal", transform.worldToLocalMatrix);
    }

    void SetInitialBufferData(Spawner3D.SpawnData spawnData)
    {
        float3[] allPoints = new float3[spawnData.points.Length];
        System.Array.Copy(spawnData.points, allPoints, spawnData.points.Length);

        PositionBuffer.SetData(allPoints);
        predictedPositionsBuffer.SetData(allPoints);
        VelocityBuffer.SetData(spawnData.velocities);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            pauseNextFrame = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            SetInitialBufferData(spawnData);
        }
        
    }

    private float3[] GetDataPositionsPoint(ComputeBuffer buffer, int count)
    {
        // Retrieve the data from the buffer into a float[] array
        float[] floatArray = new float[count * 3];
        buffer.GetData(floatArray);

        // Convert the float[] array to a float3[] array
        float3[] float3Array = new float3[count];
        for (int i = 0; i < count; i++)
        {
            float3Array[i] = new float3(floatArray[i * 3], floatArray[i * 3 + 1], floatArray[i * 3 + 2]);
            //Debug.Log(float3Array[i]);
        }

        return float3Array;
    } 
    private List<int> GetTrianglesVertexIndices(Mesh Mesh)
    {
       List<int> storedVertexIndices = new List<int>();
       

        // Store the vertex indices
        for (int i = 0; i < Mesh.triangles.Length; i++)
        {
            storedVertexIndices.Add(Mesh.triangles[i]);
        }
        return storedVertexIndices;

    }
   private  Vector3[] GetStoredIndicesFromMesh(Mesh mesh )
    {
        Vector3[] vertices = mesh.vertices; 
        Vector3[] verticesSorted =  new Vector3 [storedVertexIndices.Count];

        // Iterate over the stored vertex indices and print the corresponding vertex positions
        for (int i = 0; i < storedVertexIndices.Count; i ++)
        {
            int vertexIndex1 = storedVertexIndices[i];
            Vector3 vertex1 = vertices[vertexIndex1];
          //  Debug.Log($"the Vertex {i} is {vertex1} before");
            vertex1.Scale(scaleOfModel);
            vertex1 = rotationOfModel * vertex1;
            vertex1 = vertex1 + positionsOfModel;
            verticesSorted[i] = vertex1;
           // Debug.Log($"the Vertex {i} is {vertex1} after");






        }
        return verticesSorted;
    }
    private float3[] ConvertVector3ArrayToFloat3Array(Vector3[] vector3Array)
    {
        float3[] float3Array = new float3[vector3Array.Length];

        for (int i = 0; i < vector3Array.Length; i++)
        {
            Vector3 vector3 = vector3Array[i];
            float3Array[i] = new float3(vector3.x, vector3.y, vector3.z);
        }

        return float3Array;
    }


    void OnDestroy()
    {
        ComputeHelper.Release(PositionBuffer, predictedPositionsBuffer, VelocityBuffer, DensityBuffer, spatialIndices, spatialOffsets , trianglesBuffer);
    }

    void OnDrawGizmos()
    {
        // Draw Bounds
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = m;

    }

    void UpdateLineRenderer(int index, float3 currentPosition)
    {
        // Get the queue for the current particle
        Queue<float3> positionsQueue = positionQueues[index];

        // Add the new position to the queue if it has moved significantly
        if (positionsQueue.Count == 0 || math.distance(currentPosition, positionsQueue.Peek()) > 0.1f)
        {
            positionsQueue.Enqueue(currentPosition);

            // If the queue exceeds the maximum number of positions, remove the oldest
            if (positionsQueue.Count > maxPositions)
            {
                positionsQueue.Dequeue();
            }

            // Update the LineRenderer
            LineRenderer lineRenderer = lineRenderers[index];
            lineRenderer.positionCount = positionsQueue.Count;
            Vector3[] positionsArray = new Vector3[positionsQueue.Count];
            int i = 0;
            foreach (var pos in positionsQueue)
            {
                positionsArray[i++] = new Vector3(pos.x, pos.y, pos.z);
            }
            lineRenderer.SetPositions(positionsArray);
        }
    }

}