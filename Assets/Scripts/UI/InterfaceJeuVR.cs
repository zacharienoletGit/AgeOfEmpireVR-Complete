using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UI;

// Sources consultees:
// - Canvas: https://docs.unity3d.com/ScriptReference/Canvas.html
// - Button: https://docs.unity3d.com/ScriptReference/UI.Button.html
// - Text: https://docs.unity3d.com/ScriptReference/UI.Text.html
// - EventSystem: https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.EventSystems.EventSystem.html
// - TrackedDeviceGraphicRaycaster: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/api/UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster.html
// Aide utilisee: Codex a donne les grandes lignes pour creer une UI World Space par code.
// InterfaceJeuVR gere l'interface World Space du prototype.
// Si aucun Canvas n'est place dans la scene, ce script en cree un automatiquement.
public class InterfaceJeuVR : MonoBehaviour
{
    public const string CanvasObjectName = "InterfaceJeuVR_Visible";

    // References UI. Elles peuvent etre assignees a la main dans Unity,
    // mais le prototype les cree par code si elles sont null.
    public Canvas canvas;
    public Text titleText;
    public Text statusText;
    public Text healthText;
    public Text woodText;
    public Text waveText;
    public Text hintText;
    public Button startButton;
    public Button buildButton;
    public Button restartButton;

    GestionJeu manager;

    void Start()
    {
        // GestionJeu contient les donnees a afficher: vie, bois, vague, score, etc.
        manager = GestionJeu.Instance;

        // Construit ou retrouve l'UI. Cette methode marche aussi quand le bootstrap
        // la lance dans l'editeur, donc les boutons restent visibles hors Play mode.
        EnsureWorldUIExists(null);
    }

    public void Configure(GestionJeu gameManager)
    {
        // Permet de reconnecter InterfaceJeuVR au GestionJeu si la reference manque.
        manager = gameManager;
    }

    public void EnsureWorldUIExists(Transform parentForEditor)
    {
        // Si le Canvas a deja ete cree dans la scene, on le reprend au lieu
        // de faire un doublon. C'est important apres un reload de scripts Unity.
        if (canvas == null)
            canvas = FindExistingStudentCanvas();

        if (canvas == null)
            CreateWorldUI(parentForEditor);
        else
        {
            EnsureEventSystem();
            EnsureCanvasRaycasters(canvas.gameObject);
        }

        // Les colliders permettent au rayon physique de la manette de cliquer
        // sur les boutons START / BUILD / RESTART, meme sans UI prefab avance.
        EnsureButtonColliders();
        HookButtons();
        Refresh();
    }

    public void Refresh()
    {
        // Cette methode est appelee chaque fois qu'une valeur importante change.
        // Elle met a jour les textes et l'etat des boutons.
        if (manager == null)
            manager = GestionJeu.Instance;

        if (manager == null)
            return;

        // Titre fixe du projet.
        if (titleText != null)
            titleText.text = "Empire Miniature VR";

        if (statusText != null)
            statusText.text = GetStatusText();

        if (healthText != null)
        {
            // Affiche la vie de l'HotelDeVille. Si la reference est manquante, affiche 0 pour eviter une erreur.
            int health = manager.townHall != null ? manager.townHall.currentHealth : 0;
            int maxHealth = manager.townHall != null ? manager.townHall.maxHealth : 0;
            healthText.text = "HotelDeVille PV: " + health + " / " + maxHealth;
        }

        // Ressources et score du joueur.
        if (woodText != null)
            woodText.text = "Bois: " + manager.wood + "   Score: " + manager.score;

        if (waveText != null)
        {
            // Affiche la vague actuelle et le nombre d'ennemis encore vivants.
            int wave = manager.waveManager != null ? manager.waveManager.CurrentWave : 0;
            waveText.text = "Vague: " + wave + "   Ennemis: " + manager.EnemiesAlive;
        }

        if (hintText != null)
            hintText.text = GetHintText();

        if (startButton != null)
            startButton.gameObject.SetActive(manager.CurrentState != GestionJeu.GameState.Playing);

        // Le bouton build est seulement utilisable quand la partie est en cours.
        if (buildButton != null)
            buildButton.interactable = manager.IsPlaying;

        // Restart apparait seulement quand la partie est finie.
        if (restartButton != null)
            restartButton.gameObject.SetActive(manager.CurrentState == GestionJeu.GameState.GameOver || manager.CurrentState == GestionJeu.GameState.Victory);
    }

    string GetStatusText()
    {
        // Texte principal selon l'etat actuel de la partie.
        if (manager == null)
            return "";

        if (manager.CurrentState == GestionJeu.GameState.MainMenu)
            return "Press Start pour commencer";

        if (manager.CurrentState == GestionJeu.GameState.GameOver)
            return "Game Over - l'HotelDeVille est detruit";

        if (manager.CurrentState == GestionJeu.GameState.Victory)
            return "Victoire - toute les vagues sont fini";

        if (manager.buildSystem != null && manager.buildSystem.IsBuildMode)
            return "Build mode: click sur le sol pour placer une tower";

        return "Defend l'HotelDeVille";
    }

    string GetHintText()
    {
        // Petit rappel des controles. C'est volontairement court pour rester lisible
        // dans le casque VR.
        return "Casque/manette: viser + trigger   A/X: start   B/Y: build   Menu/R: restart";
    }

    void HookButtons()
    {
        // RemoveAllListeners evite de brancher deux fois le meme bouton si Start est rappele.
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                // Le bouton Start joue un son puis lance la partie.
                GestionAudio.Instance?.PlayButton();
                GestionJeu.Instance?.StartGame();
            });
        }

        if (buildButton != null)
        {
            buildButton.onClick.RemoveAllListeners();
            buildButton.onClick.AddListener(() =>
            {
                // Le bouton Build active/desactive le mode construction.
                GestionAudio.Instance?.PlayButton();
                GestionJeu.Instance?.ToggleBuildMode();
            });
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                // Le bouton Restart recharge la scene.
                GestionAudio.Instance?.PlayButton();
                GestionJeu.Instance?.RestartGame();
            });
        }
    }

    void CreateWorldUI(Transform parentForEditor)
    {
        // Cree une interface complete par code pour que le prototype marche sans prefab UI.
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(CanvasObjectName);
        if (parentForEditor != null)
            canvasObject.transform.SetParent(parentForEditor);

        // Position devant le joueur. Le Canvas est petit car il est en World Space.
        canvasObject.transform.position = new Vector3(0f, 1.75f, 0.85f);
        canvasObject.transform.rotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0025f;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        EnsureCanvasRaycasters(canvasObject);

        // Taille virtuelle du Canvas. Le scale du GameObject le rend petit dans le monde.
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(520f, 360f);

        // Fond semi-transparent pour lire le texte meme devant la map.
        GameObject panel = CreateImage("Panneau", canvasObject.transform, new Color(0.08f, 0.08f, 0.08f, 0.72f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Textes principaux.
        titleText = CreateText("Titre", panel.transform, new Vector2(0f, 130f), 30, TextAnchor.MiddleCenter);
        statusText = CreateText("Etat", panel.transform, new Vector2(0f, 88f), 18, TextAnchor.MiddleCenter);
        healthText = CreateText("Vie", panel.transform, new Vector2(0f, 45f), 18, TextAnchor.MiddleCenter);
        woodText = CreateText("Bois", panel.transform, new Vector2(0f, 15f), 18, TextAnchor.MiddleCenter);
        waveText = CreateText("Vague", panel.transform, new Vector2(0f, -15f), 18, TextAnchor.MiddleCenter);
        hintText = CreateText("Aide", panel.transform, new Vector2(0f, -140f), 14, TextAnchor.MiddleCenter);

        // Boutons du prototype.
        startButton = CreateButton("BoutonStart", panel.transform, "START", new Vector2(-135f, -72f));
        buildButton = CreateButton("BoutonBuild", panel.transform, "BUILD", new Vector2(0f, -72f));
        restartButton = CreateButton("BoutonRestart", panel.transform, "RESTART", new Vector2(135f, -72f));
    }

    void EnsureEventSystem()
    {
        // Les boutons Unity UI ont besoin d'un EventSystem dans la scene.
        // On en cree un si la scene n'en contient pas deja.
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        GameObject eventObject;

        if (eventSystem == null)
        {
            eventObject = new GameObject("EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
        }
        else
        {
            eventObject = eventSystem.gameObject;
        }

        // InputSystemUIInputModule garde les controles souris/simulateur.
        if (eventObject.GetComponent<InputSystemUIInputModule>() == null)
            eventObject.AddComponent<InputSystemUIInputModule>();

        // XRUIInputModule permet aussi aux Ray Interactors XR de parler aux Canvas.
        if (eventObject.GetComponent<XRUIInputModule>() == null)
            eventObject.AddComponent<XRUIInputModule>();
    }

    Canvas FindExistingStudentCanvas()
    {
        GameObject canvasObject = GameObject.Find(CanvasObjectName);
        if (canvasObject == null)
            return null;

        return canvasObject.GetComponent<Canvas>();
    }

    void EnsureCanvasRaycasters(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            canvasObject.AddComponent<GraphicRaycaster>();

        if (canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    void EnsureButtonColliders()
    {
        AddButtonCollider(startButton);
        AddButtonCollider(buildButton);
        AddButtonCollider(restartButton);
    }

    void AddButtonCollider(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        BoxCollider boxCollider = button.GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = button.gameObject.AddComponent<BoxCollider>();

        float width = rect != null ? Mathf.Max(1f, rect.sizeDelta.x) : 110f;
        float height = rect != null ? Mathf.Max(1f, rect.sizeDelta.y) : 42f;

        // Les boutons sont dans un Canvas scale tres petit. Le collider utilise
        // les unites locales du RectTransform, donc il suit bien la taille visuelle.
        boxCollider.size = new Vector3(width, height, 8f);
        boxCollider.center = Vector3.zero;
        boxCollider.isTrigger = true;
    }

    GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        // Helper pour creer rapidement une Image UI.
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return imageObject;
    }

    Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, int size, TextAnchor anchor)
    {
        // Helper pour creer un Text UI classique.
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        // Police Unity par defaut pour eviter d'ajouter une font externe au projet.
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;

        // Position et taille dans le Canvas.
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460f, 34f);
        rect.anchoredPosition = anchoredPosition;

        return text;
    }

    Button CreateButton(string objectName, Transform parent, string label, Vector2 anchoredPosition)
    {
        // Un bouton est compose d'une Image de fond + un Text enfant.
        GameObject buttonObject = CreateImage(objectName, parent, new Color(0.25f, 0.35f, 0.25f, 0.95f));
        Button button = buttonObject.AddComponent<Button>();

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110f, 42f);
        rect.anchoredPosition = anchoredPosition;

        // Texte au centre du bouton.
        Text labelText = CreateText("Texte", buttonObject.transform, Vector2.zero, 18, TextAnchor.MiddleCenter);
        labelText.text = label;
        labelText.rectTransform.sizeDelta = rect.sizeDelta;

        return button;
    }
}
