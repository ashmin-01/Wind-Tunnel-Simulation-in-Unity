using UnityEngine;

public class WindController : MonoBehaviour
{
    public Shader shader;
    public Material windMaterial;  // The material using the WindShader
    public float windStrength = 1.0f;  // Initial wind strength
    public float windSpeed = 1.0f;  // Initial wind speed

    void Start()
    {
        windMaterial = new Material(shader);
        if (windMaterial == null)
        {
            Debug.LogError("Please assign a material with the WindShader.");
        }
        else
        {
            // Set initial values
            windMaterial.SetFloat("_WindStrength", windStrength);
            windMaterial.SetFloat("_WindSpeed", windSpeed);
        }
    }

    void Update()
    {
        // Update shader properties dynamically
        if (windMaterial != null)
        {
            windMaterial.SetFloat("_WindStrength", windStrength);
            windMaterial.SetFloat("_WindSpeed", windSpeed);
        }
    }

    // Optional: Add GUI controls to adjust wind properties in real-time
    void OnGUI()
    {
        GUILayout.Label("Wind Settings:");
        windStrength = GUILayout.HorizontalSlider(windStrength, 0.0f, 10.0f);
        GUILayout.Label("Wind Strength: " + windStrength);
        windSpeed = GUILayout.HorizontalSlider(windSpeed, 0.0f, 10.0f);
        GUILayout.Label("Wind Speed: " + windSpeed);
    }
}
