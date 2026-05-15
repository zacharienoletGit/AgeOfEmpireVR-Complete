using UnityEngine;

// Sources consultees:
// - Component.GetComponentInParent: https://docs.unity3d.com/ScriptReference/Component.GetComponentInParent.html
// - MonoBehaviour: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
// SolMiniCarte est un script marqueur.
// Il ne contient pas de logique, mais il permet a PointeurVRJeu
// de reconnaitre quelles surfaces peuvent recevoir un clic de mouvement ou de construction.
public class SolMiniCarte : MonoBehaviour
{
    // Exemple: le sol et le chemin de la map ont ce component.
    // Si un objet n'a pas SolMiniCarte, un clic dessus ne deplace pas les unites.
}
