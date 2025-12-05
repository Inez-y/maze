using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]   // runs in Edit Mode too
#endif
public class ApplyDayNightShaderToAll : MonoBehaviour
{
    public Shader dayNightShader;

    private void OnEnable()
    {
        if (dayNightShader == null)
        {
            Debug.LogWarning("Assign a Day/Night shader in the inspector.");
            return;
        }

        // Find all renderers in the scene
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer rend in renderers)
        {
            var mats = rend.sharedMaterials; // shared = doesn't duplicate materials

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                // Change just the shader, keep textures/colors on the material
                mats[i].shader = dayNightShader;
            }

            rend.sharedMaterials = mats;
        }

        Debug.Log("Applied day/night shader to all renderers.");

        #if UNITY_EDITOR
        DestroyImmediate(this);
        #else
        Destroy(this);
        #endif
    }
}
