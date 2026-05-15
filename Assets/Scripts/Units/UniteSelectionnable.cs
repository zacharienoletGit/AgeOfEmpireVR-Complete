using UnityEngine;

// Sources consultees:
// - Transform: https://docs.unity3d.com/ScriptReference/Transform.html
// - Vector3.MoveTowards: https://docs.unity3d.com/ScriptReference/Vector3.MoveTowards.html
// - GameObject.CreatePrimitive: https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html
// Aide utilisee: Codex a donne les grandes lignes pour selectionner et deplacer une unite RTS.
// UniteSelectionnable represente une unite controlee par le joueur.
// Elle peut etre selectionnee, recevoir une destination, ou recevoir une cible a attaquer.
public class UniteSelectionnable : MonoBehaviour
{
    // Vitesse volontairement basse car les objets sont sur une petite map miniature.
    public float moveSpeed = 0.45f;
    public float stopDistance = 0.05f;

    // Destination de mouvement simple, sans NavMesh.
    Vector3 moveTarget;
    bool hasMoveTarget;

    // Cible d'attaque actuelle. Si elle existe, l'unite la poursuit puis attaque.
    CombatUnite attackTarget;
    CombatUnite combat;
    GameObject selectedRing;

    void Awake()
    {
        // CombatUnite gere la vie et les degats. Le cercle sert a montrer la selection.
        combat = GetComponent<CombatUnite>();
        CreateSelectedRing();
    }

    void Update()
    {
        // Pas de controle si la partie n'est pas en cours.
        if (GestionJeu.Instance == null || !GestionJeu.Instance.IsPlaying)
            return;

        if (combat == null || !combat.IsAlive)
            return;

        if (attackTarget != null && attackTarget.IsAlive)
        {
            // Si la cible est trop loin, l'unite avance vers elle.
            // Sinon elle demande a CombatUnite de faire l'attaque.
            float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (distance > combat.attackRange * 0.9f)
            {
                MoveStep(attackTarget.transform.position);
            }
            else
            {
                combat.Attack(attackTarget);
            }

            return;
        }

        if (hasMoveTarget)
        {
            // Deplacement vers le point clique sur la map.
            MoveStep(moveTarget);

            if (Vector3.Distance(transform.position, moveTarget) <= stopDistance)
                hasMoveTarget = false;
        }
    }

    public void SetSelected(bool selected)
    {
        // Active ou cache le cercle jaune sous l'unite.
        if (selectedRing != null)
            selectedRing.SetActive(selected);
    }

    public void MoveTo(Vector3 point)
    {
        // Un ordre de mouvement annule l'ancienne cible d'attaque.
        attackTarget = null;
        moveTarget = new Vector3(point.x, transform.position.y, point.z);
        hasMoveTarget = true;
    }

    public void AttackTarget(CombatUnite target)
    {
        // Le joueur ne peut pas attaquer une unite de son equipe.
        if (target == null || target.team == CombatUnite.Team.Player)
            return;

        attackTarget = target;
        hasMoveTarget = false;
    }

    void MoveStep(Vector3 point)
    {
        // Deplace l'unite d'un petit pas vers le point donne.
        // MoveTowards evite de depasser la cible.
        Vector3 flatTarget = new Vector3(point.x, transform.position.y, point.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
        Vector3 direction = flatTarget - transform.position;

        // Fait regarder l'unite dans la direction de son mouvement.
        if (direction.sqrMagnitude > 0.0001f)
            transform.forward = direction.normalized;
    }

    void CreateSelectedRing()
    {
        // Cercle de selection cree par code pour eviter d'avoir besoin d'un prefab.
        // Le collider est desactive pour que le raycast clique encore sur l'unite.
        selectedRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectedRing.name = "SelectedRing";
        selectedRing.transform.SetParent(transform);
        selectedRing.transform.localPosition = new Vector3(0f, -0.48f, 0f);
        selectedRing.transform.localScale = new Vector3(1.35f, 0.03f, 1.35f);

        Collider ringCollider = selectedRing.GetComponent<Collider>();
        if (ringCollider != null)
            ringCollider.enabled = false;

        AideMateriaux.ApplyMaterial(selectedRing, Color.yellow);
        selectedRing.SetActive(false);
    }
}
