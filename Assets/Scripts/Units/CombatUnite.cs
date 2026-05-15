using UnityEngine;

// Sources consultees:
// - MonoBehaviour: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
// - Time.deltaTime: https://docs.unity3d.com/ScriptReference/Time-deltaTime.html
// - Object.Destroy: https://docs.unity3d.com/ScriptReference/Object.Destroy.html
// - Transform.localScale: https://docs.unity3d.com/ScriptReference/Transform-localScale.html
// Aide utilisee: Codex a propose les grandes lignes pour vie, degats et cooldown.
// CombatUnite contient les statistiques communes des unites:
// equipe, vie, degats, portee et cooldown d'attaque.
// Il est utilise autant par les unites du joueur que par les ennemis.
public class CombatUnite : MonoBehaviour
{
    // Permet de savoir qui peut attaquer qui.
    public enum Team
    {
        Player,
        Enemy
    }

    // Valeurs simples modifiables dans l'inspecteur ou par GestionVagues.
    public Team team = Team.Player;
    public int maxHealth = 40;
    public int currentHealth = 40;
    public int damage = 8;
    public float attackRange = 0.35f;
    public float attackCooldown = 0.75f;
    public float attackAnimDuration = 0.14f;
    public float attackAnimScale = 1.18f;

    // Temps restant avant la prochaine attaque.
    float attackTimer;
    float attackAnimTimer;
    Vector3 normalScale;

    public bool IsAlive
    {
        get { return currentHealth > 0; }
    }

    void Start()
    {
        // Si la vie actuelle n'a pas ete configuree, on la met au maximum.
        if (currentHealth <= 0)
            currentHealth = maxHealth;

        // On garde l'echelle du depart pour pouvoir faire un petit "punch"
        // quand l'unite attaque, puis revenir a la taille normale.
        normalScale = transform.localScale;
    }

    void Update()
    {
        // Le cooldown baisse avec le temps.
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        UpdateAttackAnimation();
    }

    public void ResetHealth()
    {
        // Utilise quand une unite est creee ou quand on recommence.
        currentHealth = maxHealth;
    }

    public bool CanAttack(CombatUnite target)
    {
        // Validation avant d'attaquer: cible existante, vivante, ennemie et dans la portee.
        if (target == null || !target.IsAlive || !IsAlive)
            return false;

        if (target.team == team)
            return false;

        return Vector3.Distance(transform.position, target.transform.position) <= attackRange;
    }

    public void Attack(CombatUnite target)
    {
        // Si le cooldown n'est pas termine ou la cible invalide, on n'attaque pas.
        if (attackTimer > 0f || !CanAttack(target))
            return;

        target.TakeDamage(damage);
        attackTimer = attackCooldown;
        PlayAttackAnimation();
        GestionAudio.Instance?.PlayHit();
    }

    public void TakeDamage(int amount)
    {
        // Retire des points de vie et appelle Die si la vie tombe a zero.
        if (!IsAlive)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // Pour les ennemis, on informe GestionJeu pour le score, le bois et la victoire.
        if (team == Team.Enemy)
        {
            GestionJeu.Instance?.EnemyKilled(this);
            GestionAudio.Instance?.PlayDeath();
        }

        // Dans ce prototype, une unite morte est simplement detruite.
        Destroy(gameObject);
    }

    public void PlayAttackAnimation()
    {
        // Mini animation tres simple: l'objet grossit un peu puis revient normal.
        // Ce n'est pas un Animator complet, mais c'est suffisant pour voir le coup.
        if (normalScale == Vector3.zero)
            normalScale = transform.localScale;

        attackAnimTimer = attackAnimDuration;
    }

    void UpdateAttackAnimation()
    {
        if (attackAnimTimer <= 0f)
        {
            if (normalScale != Vector3.zero)
                transform.localScale = normalScale;

            return;
        }

        attackAnimTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(attackAnimTimer / attackAnimDuration);

        // Sinus = grossit au milieu de l'animation et revient a la fin.
        float punch = Mathf.Sin(progress * Mathf.PI);
        transform.localScale = Vector3.Lerp(normalScale, normalScale * attackAnimScale, punch);
    }
}
