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

    public bool IsDead
    {
        get { return currentHealth <= 0; }
    }

    void Start()
    {
        // Si la vie n'a pas ete initialisee, on remet la base a sa vie maximum.
        if (currentHealth <= 0)
            currentHealth = maxHealth;
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
}
