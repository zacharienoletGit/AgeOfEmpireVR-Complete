using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sources consultees:
// - Coroutine: https://docs.unity3d.com/ScriptReference/Coroutine.html
// - WaitForSeconds: https://docs.unity3d.com/ScriptReference/WaitForSeconds.html
// - Instantiate: https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
// GestionVagues controle les vagues d'ennemis.
// Il attend un peu, fait apparaitre les ennemis un par un,
// puis attend que la vague soit terminee avant de lancer la suivante.
[DisallowMultipleComponent]
public class GestionVagues : MonoBehaviour
{
    // Si enemyPrefab est vide, le script cree un ennemi capsule automatiquement.
    // Les spawnPoints sont les positions ou les ennemis apparaissent.
    [Header("Wave setup")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    [Min(0f)]
    public float firstWaveDelay = 1.5f;
    [Min(0f)]
    public float spawnDelay = 0.65f;
    [Min(0f)]
    public float timeBetweenWaves = 2f;
    [Min(1)]
    public int totalWaves = 4;

    public int CurrentWave { get; private set; }

    // Liste utile pour nettoyer les ennemis si on recommence la partie.
    readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    GestionJeu manager;
    VieBase townHall;
    Coroutine waveRoutine;

    void Awake()
    {
        // On garde la reference au GestionJeu si elle est deja disponible.
        manager = GestionJeu.Instance;
    }

    void Start()
    {
        // Securite au cas ou l'ordre d'initialisation Unity change.
        if (manager == null)
            manager = GestionJeu.Instance;

        if (townHall == null)
            townHall = FindFirstObjectByType<VieBase>();
    }

    public void Configure(GestionJeu gameManager, VieBase baseToAttack, Transform[] points)
    {
        // Permet de reconnecter les references si on les assigne par un autre script.
        // Dans la scene de base, les references sont deja placees dans l'inspecteur.
        manager = gameManager;
        townHall = baseToAttack;
        spawnPoints = points;
    }

    public void BeginWaves()
    {
        // Si une ancienne coroutine de vague tourne encore, on l'arrete avant d'en lancer une nouvelle.
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        ClearEnemies();
        CurrentWave = 0;
        waveRoutine = StartCoroutine(WaveRoutine());
    }

    public void ClearEnemies()
    {
        // Detruit les ennemis crees par ce manager. Utile quand on restart une partie
        // sans recharger completement la scene.
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
                Destroy(spawnedEnemies[i]);
        }

        spawnedEnemies.Clear();
    }

    IEnumerator WaveRoutine()
    {
        // Coroutine principale: elle permet d'attendre entre les spawns sans bloquer Unity.
        yield return new WaitForSeconds(firstWaveDelay);

        for (int wave = 1; wave <= totalWaves; wave++)
        {
            // Si la partie n'est plus en cours, on arrete les vagues.
            if (manager == null || !manager.IsPlaying)
                yield break;

            CurrentWave = wave;
            manager.UpdateUI();

            // Formule volontairement simple mais un peu plus dure qu'avant:
            // plus la vague avance, plus il y a d'ennemis.
            int enemyCount = 2 + wave;

            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(spawnDelay);
            }

            // On attend que tous les ennemis de la vague soient morts
            // avant de passer a la prochaine vague.
            while (manager != null && manager.IsPlaying && manager.EnemiesAlive > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
        }

        // Toutes les vagues sont lancees. GestionJeu verifiera s'il reste des ennemis.
        if (manager != null)
            manager.WavesFinished();
    }

    void SpawnEnemy(int wave)
    {
        // Choisit un spawn point au hasard. S'il n'y en a pas, on utilise une position par defaut.
        Transform spawnPoint = GetSpawnPoint();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 0.95f, 3.3f);

        GameObject enemyObject;

        if (enemyPrefab != null)
        {
            enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            // Ennemi de secours fait avec une capsule Unity.
            // Comme ca le prototype reste jouable meme sans assets importes.
            enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "Enemy_Wave_" + wave;
            enemyObject.transform.position = spawnPosition;
            enemyObject.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            AideMateriaux.ApplyMaterial(enemyObject, new Color(0.75f, 0.18f, 0.12f));
        }

        // Assure que l'ennemi possede un CombatUnite, puis ajuste ses stats selon la vague.
        CombatUnite combat = enemyObject.GetComponent<CombatUnite>();
        if (combat == null)
            combat = enemyObject.AddComponent<CombatUnite>();

        combat.team = CombatUnite.Team.Enemy;
        combat.maxHealth = 30 + wave * 10;
        combat.damage = 6 + wave * 2;
        combat.attackRange = 0.18f;
        combat.ResetHealth();

        // Ajoute l'IA si le prefab ne l'avait pas deja.
        EnnemiSimple enemyAI = enemyObject.GetComponent<EnnemiSimple>();
        if (enemyAI == null)
            enemyAI = enemyObject.AddComponent<EnnemiSimple>();

        enemyAI.targetBase = townHall;
        enemyAI.moveSpeed = 0.25f + wave * 0.04f;
        enemyAI.attackDelay = 0.95f;

        spawnedEnemies.Add(enemyObject);
    }

    Transform GetSpawnPoint()
    {
        // Retourne null si aucun point n'est configure. SpawnEnemy utilisera alors sa position par defaut.
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
