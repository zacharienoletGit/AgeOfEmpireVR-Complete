using System.Collections.Generic;
using UnityEngine;

// Sources consultees:
// - Object.Instantiate: https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
// - GameObject: https://docs.unity3d.com/ScriptReference/GameObject.html
// - Transform.SetParent: https://docs.unity3d.com/ScriptReference/Transform.SetParent.html
// Aide utilisee: Codex a propose les grandes lignes pour placer des tours avec du bois.
// SystemeConstruction gere la construction des tours.
// Quand le joueur active le mode build, son prochain clic sur le sol essaie de placer une tour.
public class SystemeConstruction : MonoBehaviour
{
    // Si towerPrefab est null, une tour simple est creee avec des primitives Unity.
    public GameObject towerPrefab;
    public int towerCost = 40;
    public bool IsBuildMode { get; private set; }

    // Liste des tours creees pendant la partie. Sert a les supprimer lors d'un restart sans recharger la scene.
    readonly List<GameObject> placedTowers = new List<GameObject>();
    GestionJeu manager;

    void Start()
    {
        // Reference au GestionJeu pour verifier les ressources du joueur.
        manager = GestionJeu.Instance;
    }

    public void Configure(GestionJeu gameManager)
    {
        // Permet de donner la reference au gestionnaire si elle manque.
        manager = gameManager;
    }

    public void ToggleBuildMode()
    {
        // Change le mode construction: false devient true, true devient false.
        IsBuildMode = !IsBuildMode;
    }

    public void CancelBuildMode()
    {
        // Methode utile si plus tard on veut annuler avec un bouton ou une touche.
        IsBuildMode = false;
    }

    public bool TryPlaceTower(Vector3 position)
    {
        // Si le joueur n'est pas en mode construction, le clic ne fait rien ici.
        if (!IsBuildMode)
            return false;

        if (manager == null)
            manager = GestionJeu.Instance;

        // TrySpendWood retourne false si le joueur n'a pas assez de bois.
        if (manager == null || !manager.TrySpendWood(towerCost))
            return false;

        // On remonte un peu la tour sur l'axe Y pour qu'elle soit visible au-dessus du sol.
        Vector3 towerPosition = new Vector3(position.x, position.y + 0.18f, position.z);
        GameObject towerObject;

        if (towerPrefab != null)
        {
            // Cas normal si un prefab de tour est assigne dans Unity.
            towerObject = Instantiate(towerPrefab, towerPosition, Quaternion.identity);
        }
        else
        {
            // Cas de secours pour que le prototype marche sans assets.
            towerObject = CreateBasicTower(towerPosition);
        }

        // La tour est conservee pour pouvoir la supprimer plus tard.
        placedTowers.Add(towerObject);
        IsBuildMode = false;
        GestionAudio.Instance?.PlayBuild();
        manager.UpdateUI();
        return true;
    }

    public void ClearPlacedTowers()
    {
        // Nettoie toutes les tours creees pendant la partie.
        for (int i = placedTowers.Count - 1; i >= 0; i--)
        {
            if (placedTowers[i] != null)
                Destroy(placedTowers[i]);
        }

        placedTowers.Clear();
        IsBuildMode = false;
    }

    GameObject CreateBasicTower(Vector3 position)
    {
        // Tour prototype: un GameObject racine avec un cylindre et un cube enfant.
        GameObject root = new GameObject("DefenseTower_Student");
        root.transform.position = position;

        // Base ronde de la tour.
        GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePart.name = "TowerBase";
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = Vector3.zero;
        basePart.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        AideMateriaux.ApplyMaterial(basePart, new Color(0.45f, 0.38f, 0.28f));

        // Partie du haut, juste pour rendre la tour plus reconnaissable.
        GameObject topPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topPart.name = "TowerTop";
        topPart.transform.SetParent(root.transform);
        topPart.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        topPart.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);
        AideMateriaux.ApplyMaterial(topPart, new Color(0.65f, 0.55f, 0.38f));

        // Ajoute la logique de tir automatique sur la racine.
        root.AddComponent<TourDefenseSimple>();
        return root;
    }
}
