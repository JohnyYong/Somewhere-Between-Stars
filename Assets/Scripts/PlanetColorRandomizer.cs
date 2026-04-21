using UnityEngine;

public class PlanetColorRandomizer : MonoBehaviour
{
	[Header("Ocean Color Range")]
	public Color[] oceanColors = {
		new Color(0.05f, 0.15f, 0.4f),   // deep blue
        new Color(0.05f, 0.25f, 0.3f),   // teal
        new Color(0.15f, 0.05f, 0.3f),   // deep purple
        new Color(0.3f,  0.1f,  0.05f),  // burnt orange
        new Color(0.05f, 0.2f,  0.1f),   // dark green
    };

	[Header("Land Color Range")]
	public Color[] landColors = {
		new Color(0.2f,  0.5f,  0.8f),   // light blue
        new Color(0.1f,  0.6f,  0.6f),   // cyan teal
        new Color(0.5f,  0.3f,  0.8f),   // lavender
        new Color(0.8f,  0.4f,  0.1f),   // warm orange
        new Color(0.2f,  0.6f,  0.3f),   // sage green
    };

	[Header("Atmosphere Color Range")]
	public Color[] atmosphereColors = {
		new Color(0.2f,  0.5f,  1.0f),   // blue
        new Color(0.1f,  0.8f,  0.8f),   // cyan
        new Color(0.6f,  0.3f,  1.0f),   // purple
        new Color(1.0f,  0.5f,  0.2f),   // orange
        new Color(0.3f,  1.0f,  0.5f),   // green
    };

	private Material _planetMat;

	public void Randomize()
	{
		// Get the planet surface material
		var renderer = GetComponent<MeshRenderer>();
		if (renderer == null) return;

		// Create a unique material instance so planets don't share colours
		_planetMat = Instantiate(renderer.material);
		renderer.material = _planetMat;

		// Pick random colours from the arrays
		Color ocean = oceanColors[Random.Range(0, oceanColors.Length)];
		Color land = landColors[Random.Range(0, landColors.Length)];
		Color atmos = atmosphereColors[Random.Range(0, atmosphereColors.Length)];

		// Make land slightly lighter than ocean for natural look
		land = Color.Lerp(ocean, land, 0.6f);

		// Apply to material
		_planetMat.SetColor("_OceanColor", ocean);
		_planetMat.SetColor("_LandColor", land);
		_planetMat.SetColor("_AtmosphereColor", atmos);

		// Also randomise size slightly
		float scale = Random.Range(0.7f, 1.3f);
		transform.localScale *= scale;
	}
}