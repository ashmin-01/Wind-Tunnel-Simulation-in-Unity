using Unity.Mathematics;
using UnityEngine;

public class Spawner3D : MonoBehaviour
{
    public int particleCount;
    public int linesCount = 20;  // Number of horizontal lines per layer
    public int layersCount = 10; // Number of layers
    public Vector3 centre;
    public Vector2 spawnSize;   // Size on the xz-plane
    public float3 initialVel;
    public float jitterStrength;
    public bool showSpawnBounds;

    [Header("Info")]
    public int debug_numParticles;

    public SpawnData GetSpawnData()
    {
        int particlesPerLine = Mathf.CeilToInt(particleCount / (float)(linesCount * layersCount));
        float3[] points = new float3[particleCount];
        float3[] velocities = new float3[particleCount];
        var rng = new Unity.Mathematics.Random(42);
        int i = 0;

        for (int layer = 0; layer < layersCount; layer++)
        {
            if (i >= particleCount) break;
            // y 
            float layerPosition = (float)layer / (layersCount - 1);  // Normalize layer position within 0 to 1 range

            for (int line = 0; line < linesCount; line++)
            {
                if (i >= particleCount) break;
                // z 
                float linePosition = (float)line / (linesCount - 1);  // Normalize line position within the layer

                for (int j = 0; j < particlesPerLine; j++)
                {
                    if (i >= particleCount) break;
                    // x 
                    float xPosition = j / (float)(particlesPerLine - 1);  // Normalize particle positions along the line

                    // Calculate positions based on normalized values
                    float x = Mathf.Lerp(-0.5f * spawnSize.x, 0.5f * spawnSize.x, linePosition) + centre.x;
                    float y = Mathf.Lerp(-0.5f * spawnSize.y, 0.5f * spawnSize.y, layerPosition) + centre.y;
                    float z = Mathf.Lerp(-0.5f * spawnSize.y, 0.5f * spawnSize.y, xPosition) + centre.z;

                    // Jitter calculation for adding randomness
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2;
                    float3 dir = new float3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    float3 jitter = dir * jitterStrength * ((float)rng.NextDouble() - 0.5f);

                    points[i] = new float3(x, y, z) + jitter;
                    velocities[i] = initialVel;
                    i++;
                }
            }
        }

        return new SpawnData() { points = points, velocities = velocities };
    }

    public struct SpawnData
    {
        public float3[] points;
        public float3[] velocities;
    }

    void OnValidate()
    {
        debug_numParticles = particleCount;
    }

    void OnDrawGizmos()
    {
        if (showSpawnBounds && !Application.isPlaying)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireCube(centre, new Vector3(spawnSize.x, spawnSize.y, spawnSize.y));
        }
    }
}
