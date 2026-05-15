using UnityEngine;
using UnityEngine.SceneManagement;

// Sources consultees:
// - MonoBehaviour: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
// - SceneManager: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html
// Aide utilisee: Codex a donne les grandes lignes du GestionJeu et les sources Unity a verifier.
// GestionJeu est le script central du prototype.
// Il garde l'etat de la partie, les ressources, le score, la selection du joueur
// et il appelle les autres systemes comme les vagues, l'interface et la construction.
public class GestionJeu : MonoBehaviour
{
    // Les etats possibles du jeu. Ca evite d'avoir plusieurs booleens partout
    // comme isPlaying, isGameOver, isInMenu, etc.
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver,
        Victory
    }

    public static GestionJeu Instance { get; private set; }

    // References principales. Elles peuvent etre assignees dans Unity,
    // mais le script peut aussi les retrouver automatiquement avec FindObjectOfType.
    [Header("Main references")]
    public GestionVagues waveManager;
    public InterfaceJeuVR gameUI;
    public VieBase townHall;
    public SystemeConstruction buildSystem;

    // Valeurs simples du prototype. Le bois sert a construire des tours,
    // et le score augmente quand un ennemi est detruit.
    [Header("Student values")]
    public int startWood = 70;
    public int wood;
    public int score;

    // Proprietes lues par les autres scripts. Le setter prive empeche
    // les autres classes de changer l'etat sans passer par GestionJeu.
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public UniteSelectionnable SelectedUnit { get; private set; }
    public int EnemiesAlive { get; private set; }

    // Devient true quand GestionVagues a fini de lancer toutes les vagues.
    // La victoire arrive seulement quand ce booleen est true ET qu'il ne reste plus d'ennemis.
    bool allWavesFinished;

    public bool IsPlaying
    {
        get { return CurrentState == GameState.Playing; }
    }

    void Awake()
    {
        // Singleton tres simple: les autres scripts peuvent appeler GestionJeu.Instance.
        // Si un deuxieme GestionJeu apparait par erreur, on le detruit.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Au debut, on s'assure que les references existent et on met l'UI a jour.
        FindMissingReferences();
        wood = startWood;
        UpdateUI();
    }

    public void Configure(GestionVagues wave, InterfaceJeuVR ui, VieBase baseToProtect, SystemeConstruction builder)
    {
        // Permet de reconnecter les references si jamais elles manquent dans l'inspecteur.
        waveManager = wave;
        gameUI = ui;
        townHall = baseToProtect;
        buildSystem = builder;
    }

    public void StartGame()
    {
        // Lance une nouvelle partie sans recharger la scene.
        // On remet les compteurs a zero et on demande au GestionVagues de commencer.
        FindMissingReferences();

        CurrentState = GameState.Playing;
        wood = startWood;
        score = 0;
        EnemiesAlive = 0;
        allWavesFinished = false;
        SelectedUnit = null;

        if (townHall != null)
            townHall.ResetHealth();

        if (buildSystem != null)
            buildSystem.ClearPlacedTowers();

        if (waveManager != null)
            waveManager.BeginWaves();

        UpdateUI();
    }

    public void RestartGame()
    {
        // Recharge la scene active. C'est simple et fiable pour un prototype etudiant.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RegisterEnemy(EnnemiSimple enemy)
    {
        // Appele par EnnemiSimple quand un ennemi devient actif dans la scene.
        // Le compteur sert a savoir quand une vague est terminee.
        if (!IsPlaying || enemy == null)
            return;

        EnemiesAlive++;
        UpdateUI();
    }

    public void EnemyKilled(CombatUnite enemy)
    {
        // Appele par CombatUnite quand un ennemi meurt.
        // On donne un peu de score et de bois pour encourager le joueur a survivre.
        if (!IsPlaying)
            return;

        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
        score += 10;
        wood += 10;

        CheckVictory();
        UpdateUI();
    }

    public void WavesFinished()
    {
        // GestionVagues appelle ceci quand toutes les vagues ont ete lancees.
        // La partie n'est pas forcement gagnee tout de suite: il peut rester des ennemis vivants.
        allWavesFinished = true;
        CheckVictory();
        UpdateUI();
    }

    public void GameOver()
    {
        // Appele par VieBase quand l'HotelDeVille tombe a 0 HP.
        // On garde une protection pour eviter de relancer le Game Over plusieurs fois.
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = GameState.GameOver;
        SelectedUnit = null;
        UpdateUI();
    }

    public void SelectUnit(UniteSelectionnable unit)
    {
        // Gere la selection d'unite. Avant de selectionner une nouvelle unite,
        // on cache le cercle jaune de l'ancienne unite.
        if (!IsPlaying)
            return;

        if (SelectedUnit != null)
            SelectedUnit.SetSelected(false);

        SelectedUnit = unit;

        if (SelectedUnit != null)
            SelectedUnit.SetSelected(true);

        UpdateUI();
    }

    public void MoveSelectedUnit(Vector3 point)
    {
        // Recoit un point clique sur la map et donne l'ordre de mouvement
        // a l'unite actuellement selectionnee.
        if (SelectedUnit == null || !IsPlaying)
            return;

        SelectedUnit.MoveTo(point);
    }

    public void AttackWithSelectedUnit(CombatUnite target)
    {
        // Donne un ordre d'attaque a l'unite selectionnee.
        // CombatUnite gere ensuite les degats, la portee et le cooldown.
        if (SelectedUnit == null || target == null || !IsPlaying)
            return;

        SelectedUnit.AttackTarget(target);
    }

    public bool TrySpendWood(int amount)
    {
        // Methode utilisee par SystemeConstruction. Elle retourne false si le joueur
        // n'a pas assez de bois, donc la tour ne sera pas construite.
        if (wood < amount)
            return false;

        wood -= amount;
        UpdateUI();
        return true;
    }

    public void ToggleBuildMode()
    {
        // Active ou desactive le mode construction.
        // En build mode, un clic sur le sol place une tour au lieu de deplacer une unite.
        if (!IsPlaying || buildSystem == null)
            return;

        buildSystem.ToggleBuildMode();
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Point unique pour rafraichir l'interface.
        // Comme ca les autres scripts n'ont pas besoin de connaitre les Text/Button directement.
        if (gameUI != null)
            gameUI.Refresh();
    }

    void FindMissingReferences()
    {
        // Permet au prototype de marcher meme si les references ne sont pas assignees
        // dans l'inspecteur Unity. C'est moins optimise, mais pratique pour ce petit projet.
        if (waveManager == null)
            waveManager = FindObjectOfType<GestionVagues>();

        if (gameUI == null)
            gameUI = FindObjectOfType<InterfaceJeuVR>();

        if (townHall == null)
            townHall = FindObjectOfType<VieBase>();

        if (buildSystem == null)
            buildSystem = FindObjectOfType<SystemeConstruction>();
    }

    void CheckVictory()
    {
        // Condition de victoire: toutes les vagues ont ete envoyees et il n'y a plus d'ennemi.
        if (allWavesFinished && EnemiesAlive <= 0 && CurrentState == GameState.Playing)
            CurrentState = GameState.Victory;
    }
}
