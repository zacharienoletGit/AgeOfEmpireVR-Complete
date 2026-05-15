using UnityEngine;

// Sources consultees:
// - Transform: https://docs.unity3d.com/ScriptReference/Transform.html
// - Vector3.MoveTowards: https://docs.unity3d.com/ScriptReference/Vector3.MoveTowards.html
// - Time.deltaTime: https://docs.unity3d.com/ScriptReference/Time-deltaTime.html
// Aide utilisee: Codex a donne les grandes lignes pour une IA simple qui avance vers la base.
// EnnemiSimple controle un ennemi tres simple:
// il marche vers l'HotelDeVille, puis l'attaque a intervalle regulier.
// Il n'utilise pas NavMesh pour garder le prototype facile a comprendre.
public class EnnemiSimple : MonoBehaviour
{
    // Base a attaquer. Normalement assignee par GestionVagues.
    public VieBase targetBase;
    public float moveSpeed = 0.25f;
    public float stopDistance = 0.22f;
    public float attackDelay = 0.95f;

    // CombatUnite contient les stats de combat comme les degats et la vie.
    CombatUnite combat;
    float attackTimer;
    bool registered;

    void Start()
    {
        // On recupere le component CombatUnite present sur le meme GameObject.
        combat = GetComponent<CombatUnite>();

        // Securite: si GestionVagues n'a pas donne la base, on la cherche dans la scene.
        if (targetBase == null)
            targetBase = FindObjectOfType<VieBase>();

        // Inscrit cet ennemi dans GestionJeu pour que le compteur d'ennemis soit exact.
        if (!registered)
        {
            GestionJeu.Instance?.RegisterEnemy(this);
            registered = true;
        }
    }

    void Update()
    {
        // L'ennemi ne bouge pas dans les menus, game over ou victoire.
        if (GestionJeu.Instance == null || !GestionJeu.Instance.IsPlaying)
            return;

        // Si l'ennemi ou la base n'existe plus, on arrete cette frame.
        if (combat == null || !combat.IsAlive || targetBase == null || targetBase.IsDead)
            return;

        // Compte a rebours du cooldown d'attaque.
        attackTimer -= Time.deltaTime;

        Vector3 targetPosition = targetBase.transform.position;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > stopDistance)
        {
            // Mouvement direct vers la base. On garde le Y actuel pour ne pas faire voler l'ennemi.
            Vector3 flatTarget = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
            Vector3 direction = flatTarget - transform.position;

            // Oriente l'ennemi vers sa direction de deplacement si la direction est assez grande.
            if (direction.sqrMagnitude > 0.0001f)
                transform.forward = direction.normalized;
        }
        else if (attackTimer <= 0f)
        {
            // Une fois assez proche, l'ennemi attaque directement l'HotelDeVille.
            combat.PlayAttackAnimation();
            targetBase.TakeDamage(combat.damage);
            GestionAudio.Instance?.PlayHit();
            attackTimer = attackDelay;
        }
    }
}
