using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class Simulation3D : MonoBehaviour
{
    //Mayar var for collision
    float3[] positionspointArray;
    [SerializeField] private GameObject cube;
    Mesh cubeMesh;
    Vector3 positionsOfModel;
    Vector3 scaleOfModel;
    private  List<int> storedVertexIndices = new List<int>();
    private Vector3[] vector3MeshVertices;
    private float3[] float3MeshVertices;
    private bool isCollision;
    public float sphereRadius = 5f;
    //


    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity = -10;

    // Wind Power
    public float3 windDirection = new(1, 0, 0);
    public float windStrength = 0.1f;

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
    const int collisionKernel = 6;

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
        cubeMesh = cube.GetComponent<MeshFilter>().mesh;
        positionsOfModel = cube.GetComponent<Transform>().position;
        scaleOfModel = cube.GetComponent<Transform>().localScale;
        storedVertexIndices = GetTrianglesVertexIndices(cubeMesh);

        vector3MeshVertices = GetStoredIndicesFromMesh(cubeMesh);
         float3MeshVertices = ConvertVector3ArrayToFloat3Array(vector3MeshVertices);


        trianglesBuffer = ComputeHelper.CreateStructuredBuffer<float3>(float3MeshVertices.Length);
        trianglesBuffer.SetData(float3MeshVertices);
        compute.SetBuffer(0, "Triangles", trianglesBuffer);
        compute.SetFloat("sphereRadius", sphereRadius);
        compute.SetInt("numTriangles", float3MeshVertices.Length / 3);


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
        //ComputeHelper.Dispatch(compute, trianglesBuffer.count, kernelIndex: collisionKernel);
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
            vertex1 = vertex1 + positionsOfModel;
            vertex1.Scale(scaleOfModel);
            verticesSorted[i] = vertex1;
            

            

            Debug.Log($"vertex ({vertex1})");
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

    
}