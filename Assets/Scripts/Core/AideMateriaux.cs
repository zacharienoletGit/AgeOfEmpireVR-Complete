using UnityEngine;

// Sources consultees:
// - Shader.Find: https://docs.unity3d.com/ScriptReference/Shader.Find.html
// - Material: https://docs.unity3d.com/ScriptReference/Material.html
// Aide utilisee: Codex a aide a garder seulement un petit helper de couleur,
// parce que les objets de base sont maintenant places directement dans la scene.
// Cette classe ne cree pas la scene. Elle sert juste a appliquer une couleur
// aux primitives creees pendant le jeu, comme les ennemis et les tours.
public static class AideMateriaux
{
    public static void ApplyMaterial(GameObject target, Color color)
    {
        // Helper partage par plusieurs scripts pour colorer les primitives Unity.
        if (target == null)
            return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;
    }
}
