using UnityEngine;

// Sources consultees:
// - Gizmos: https://docs.unity3d.com/ScriptReference/Gizmos.html
// - Object.FindObjectsOfType: https://docs.unity3d.com/ScriptReference/Object.FindObjectsOfType.html
// - Vector3.Distance: https://docs.unity3d.com/ScriptReference/Vector3.Distance.html
// Aide utilisee: Codex a donne les grandes lignes pour une tour automatique simple.
// TourDefenseSimple est la logique d'une tour defensive.
// La tour cherche automatiquement l'ennemi le plus proche dans sa portee,
// puis lui inflige des degats a intervalle regulier.
public class TourDefenseSimple : MonoBehaviour
{
    // range = rayon de detection, damage = degats par tir, fireDelay = temps entre deux tirs.
    public float range = 0.9f;
    public int damage = 10;
    public float fireDelay = 0.7f;
    public float shootAnimDuration = 0.12f;
    public float shootAnimScale = 1.15f;

    // Timer interne pour limiter la vitesse de tir.
    float fireTimer;
    float shootAnimTimer;
    Vector3 normalScale;

    void Start()
    {
        // Meme idee que les unites: on garde la taille normale pour faire
        // un petit rebond quand la tour tire.
        normalScale = transform.localScale;
    }

    void Update()
    {
        UpdateShootAnimation();

        // La tour ne tire que pendant la partie.
        if (GestionJeu.Instance == null || !GestionJeu.Instance.IsPlaying)
            return;

        fireTimer -= Time.deltaTime;

        // Cherche une cible a chaque frame. Pour un petit prototype c'est correct.
        // Dans un gros jeu, on optimiserait avec des triggers ou une liste d'ennemis.
        CombatUnite target = FindClosestEnemy();
        if (target == null || fireTimer > 0f)
            return;

        // Le tir est instantane: pas de projectile, juste des degats directs.
        target.TakeDamage(damage);
        PlayShootAnimation();
        GestionAudio.Instance?.PlayHit();
        fireTimer = fireDelay;
    }

    CombatUnite FindClosestEnemy()
    {
        // On prend tous les CombatUnite de la scene et on garde seulement les ennemis vivants.
        CombatUnite[] combats = FindObjectsOfType<CombatUnite>();
        CombatUnite closest = null;
        float closestDistance = range;

        for (int i = 0; i < combats.Length; i++)
        {
            if (combats[i] == null || !combats[i].IsAlive || combats[i].team != CombatUnite.Team.Enemy)
                continue;

            // Si cet ennemi est plus proche que l'ancien meilleur choix, il devient la cible.
            float distance = Vector3.Distance(transform.position, combats[i].transform.position);
            if (distance <= closestDistance)
            {
                closest = combats[i];
                closestDistance = distance;
            }
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        // Affiche la portee dans la Scene view quand la tour est selectionnee.
        // Ca aide a placer et debugger les tours dans Unity.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    void PlayShootAnimation()
    {
        if (normalScale == Vector3.zero)
            normalScale = transform.localScale;

        shootAnimTimer = shootAnimDuration;
    }

    void UpdateShootAnimation()
    {
        if (shootAnimTimer <= 0f)
        {
            if (normalScale != Vector3.zero)
                transform.localScale = normalScale;

            return;
        }

        shootAnimTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(shootAnimTimer / shootAnimDuration);
        float punch = Mathf.Sin(progress * Mathf.PI);
        transform.localScale = Vector3.Lerp(normalScale, normalScale * shootAnimScale, punch);
    }
}
