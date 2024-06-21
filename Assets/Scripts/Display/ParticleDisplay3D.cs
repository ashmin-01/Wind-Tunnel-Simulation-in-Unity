using System.Data;
using UnityEngine;

public class ParticleDisplay3D : MonoBehaviour
{
    // private float timer =0;
    // private float timer =0;
    private bool showParticles = false;
    public Shader shader;
    public float scale;
    Mesh mesh;
    public Color col;
    Material mat;

    ComputeBuffer argsBuffer;
    
    Bounds bounds;

    public Gradient colourMap;
    public int gradientResolution;
    public float velocityDisplayMax;
    Texture2D gradientTexture;
    bool needsUpdate;

    public int meshResolution;
    public int debug_MeshTriCount;

    public void Init(Simulation3D sim)
    {
        mat = new Material(shader);
        
        mat.SetBuffer("Positions", sim.PositionBuffer);
        mat.SetBuffer("Velocities", sim.VelocityBuffer);
        mat.SetBuffer("ParticlesShow", sim.particlesShow);

        mesh = SphereGeneratorEightFaces.GenerateSphereMesh(meshResolution);
        debug_MeshTriCount = mesh.triangles.Length / 3;
        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.PositionBuffer.count );
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
    }

    void LateUpdate()
    {

        UpdateSettings();


        if (Input.GetKeyDown(KeyCode.J))
        {
            showParticles = !showParticles;
        }
        if (showParticles)
        {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argsBuffer);

        }


    }

    void UpdateSettings()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            TextureFromGradient(ref gradientTexture, gradientResolution, colourMap);
            mat.SetTexture("ColourMap", gradientTexture);
        }
        mat.SetFloat("scale", scale);
        mat.SetColor("colour", col);
        mat.SetFloat("velocityMax", velocityDisplayMax);

        Vector3 s = transform.localScale;
        transform.localScale = Vector3.one;
        var localToWorld = transform.localToWorldMatrix;
        transform.localScale = s;

        mat.SetMatrix("localToWorld", localToWorld);
    }

    	public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
	{
		if (texture == null)
		{
			texture = new Texture2D(width, 1);
		}
		else if (texture.width != width)
		{
			texture.Reinitialize(width, 1);
		}
		if (gradient == null)
		{
			gradient = new Gradient();
			gradient.SetKeys(
				new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) },
				new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
			);
		}
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = filterMode;

		Color[] cols = new Color[width];
		for (int i = 0; i < cols.Length; i++)
		{
			float t = i / (cols.Length - 1f);
			cols[i] = gradient.Evaluate(t);
		}
		texture.SetPixels(cols);
		texture.Apply();
	}

    private void OnValidate()
    {
        needsUpdate = true;
    }

    void OnDestroy()
    {
        ComputeHelper.Release(argsBuffer);
    }

    ComputeBuffer CreateReducedParticleBuffer(ComputeBuffer originalBuffer)
    {
        // Retrieve the original particle data from the original buffer
        int particleCount = originalBuffer.count;
        Vector3[] originalParticleData = new Vector3[particleCount];
        originalBuffer.GetData(originalParticleData);

        // Calculate the number of particles for the new buffer (one-fifth of the original)
        int newParticleCount = Mathf.CeilToInt((float)particleCount / 5);

        // Create an array to hold the reduced particle positions
        Vector3[] reducedParticleData = new Vector3[newParticleCount];

        // Extract every fifth particle position
        for (int i = 0; i < newParticleCount; i++)
        {
            reducedParticleData[i] = originalParticleData[i * 5];
        }

        // Create a new compute buffer and set the data
        ComputeBuffer newBuffer = new ComputeBuffer(newParticleCount, sizeof(float) * 3);
        newBuffer.SetData(reducedParticleData);

        return newBuffer;
    }
}