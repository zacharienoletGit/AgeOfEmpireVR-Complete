using UnityEngine;

// Sources consultees:
// - MonoBehaviour: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
// - Mathf.Max: https://docs.unity3d.com/ScriptReference/Mathf.Max.html
// Aide utilisee: Codex a propose les grandes lignes pour une vie de base et un Game Over.
// VieBase gere la vie du batiment principal, l'HotelDeVille.
// Quand sa vie tombe a zero, le script avertit GestionJeu pour terminer la partie.
public class VieBase : MonoBehaviour
{
    // Vie maximum et vie courante. currentHealth est public pour etre affiche dans l'UI.
    public int maxHealth = 150;
    public int currentHealth = 150;
    [Range(0.05f, 1f)] public float visualAlpha = 0.35f;

    public bool IsDead
    {
        get { return currentHealth <= 0; }
    }

    void Start()
    {
        // Si la vie n'a pas ete initialisee, on remet la base a sa vie maximum.
        if (currentHealth <= 0)
            currentHealth = maxHealth;

        ApplyTransparentVisuals();
    }

    public void ResetHealth()
    {
        // Utilise au debut d'une partie pour reparer completement l'HotelDeVille.
        currentHealth = maxHealth;
        GestionJeu.Instance?.UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        // Si la base est deja morte, on ignore les degats en trop.
        if (IsDead)
            return;

        // Mathf.Max empeche la vie de descendre sous zero.
        currentHealth = Mathf.Max(0, currentHealth - damage);
        GestionJeu.Instance?.UpdateUI();

        // Si la vie atteint zero, la partie passe en Game Over.
        if (currentHealth <= 0)
            GestionJeu.Instance?.GameOver();
    }

    void ApplyTransparentVisuals()
    {
        // L'HotelDeVille reste visible, mais le joueur voit et clique plus facilement la carte derriere.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
                MakeMaterialTransparent(materials[j]);
        }
    }

    void MakeMaterialTransparent(Material material)
    {
        if (material == null)
            return;

        Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        color.a = visualAlpha;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
