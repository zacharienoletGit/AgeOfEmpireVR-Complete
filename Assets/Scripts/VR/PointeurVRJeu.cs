using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

// Sources consultees:
// - Physics.Raycast: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
// - Input System Mouse/Keyboard: https://docs.unity3d.com/Packages/com.unity.inputsystem%401.19/manual/index.html
// - XR InputDevices: https://docs.unity3d.com/ScriptReference/XR.InputDevices.html
// - Button.onClick: https://docs.unity3d.com/ScriptReference/UI.Button-onClick.html
// Aide utilisee: Codex a donne les grandes lignes pour relier casque, manettes, souris et gameplay.
// PointeurVRJeu transforme un rayon en actions de jeu.
// Le joueur peut viser avec la manette droite, la manette gauche ou le regard du casque
// si aucune manette n'est trouvee. La souris reste seulement comme simulateur dans Unity.
public class PointeurVRJeu : MonoBehaviour
{
    // Camera utilisee pour la visee au centre du casque et pour le test souris.
    public Camera rayCamera;

    // Les transforms sont remplis automatiquement si les objets s'appellent
    // "Right Controller" et "Left Controller" dans le XR Origin.
    public Transform rightController;
    public Transform leftController;

    public float rayDistance = 10f;
    public LayerMask rayMask = ~0;

    // Ligne visible qui aide beaucoup dans le casque: on voit exactement ou on vise.
    public bool showPointerRay = true;
    public Color pointerColor = new Color(0.1f, 0.85f, 1f, 0.9f);

    bool lastRightTriggerState;
    bool lastLeftTriggerState;
    bool lastPrimaryButtonState;
    bool lastSecondaryButtonState;
    bool lastMenuButtonState;
    bool lastGripButtonState;

    LineRenderer pointerLine;
    readonly List<UnityEngine.XR.InputDevice> rightHandDevices = new List<UnityEngine.XR.InputDevice>();
    readonly List<UnityEngine.XR.InputDevice> leftHandDevices = new List<UnityEngine.XR.InputDevice>();
    readonly List<UnityEngine.XR.InputDevice> controllerDevices = new List<UnityEngine.XR.InputDevice>();

    void Start()
    {
        FindMissingPointerReferences();
        CreatePointerLineIfNeeded();
    }

    void Update()
    {
        GestionJeu manager = GestionJeu.Instance;
        if (manager == null)
            return;

        FindMissingPointerReferences();
        HandleKeyboard(manager);
        HandleControllerShortcuts(manager);

        bool fromMouse = WasMousePressed();
        Transform triggerSource;
        bool fromController = WasControllerTriggerPressed(out triggerSource);

        if (fromMouse || fromController)
            HandlePointer(manager, triggerSource, fromMouse);

        UpdatePointerLine(triggerSource);
    }

    public void FindMissingPointerReferences()
    {
        // Les references peuvent etre perdues si le XR Rig recharge ou si la scene
        // est reconstruite dans l'editeur, donc on les retrouve doucement au besoin.
        if (rayCamera == null)
            rayCamera = Camera.main;

        if (rayCamera == null)
            rayCamera = FindObjectOfType<Camera>();

        if (rightController == null)
            rightController = FindTransformByNames("Right Controller", "RightHand Controller", "XR Controller Right", "RightHand");

        if (leftController == null)
            leftController = FindTransformByNames("Left Controller", "LeftHand Controller", "XR Controller Left", "LeftHand");
    }

    void HandleKeyboard(GestionJeu manager)
    {
        // Raccourcis seulement pour tester dans Unity sans casque.
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame && manager.CurrentState != GestionJeu.GameState.Playing)
            manager.StartGame();

        if (keyboard.rKey.wasPressedThisFrame)
            manager.RestartGame();

        if (keyboard.bKey.wasPressedThisFrame)
            manager.ToggleBuildMode();
    }

    void HandleControllerShortcuts(GestionJeu manager)
    {
        // A ou X: demarrer depuis le casque, sans clavier.
        if (WasAnyControllerButtonPressed(UnityEngine.XR.CommonUsages.primaryButton, ref lastPrimaryButtonState))
        {
            if (manager.CurrentState != GestionJeu.GameState.Playing)
                manager.StartGame();
        }

        // B ou Y: mode construction.
        if (WasAnyControllerButtonPressed(UnityEngine.XR.CommonUsages.secondaryButton, ref lastSecondaryButtonState))
            manager.ToggleBuildMode();

        // Bouton menu: restart.
        if (WasAnyControllerButtonPressed(UnityEngine.XR.CommonUsages.menuButton, ref lastMenuButtonState))
            manager.RestartGame();

        // Grip: annule le build mode, pratique si on a clique par erreur.
        if (WasAnyControllerButtonPressed(UnityEngine.XR.CommonUsages.gripButton, ref lastGripButtonState))
        {
            if (manager.buildSystem != null)
            {
                manager.buildSystem.CancelBuildMode();
                manager.UpdateUI();
            }
        }
    }

    void HandlePointer(GestionJeu manager, Transform controllerSource, bool fromMouse)
    {
        Ray ray = GetPointerRay(controllerSource, fromMouse);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
            return;

        // Les boutons UI ont des BoxCollider ajoutes par InterfaceJeuVR. Donc une gachette
        // peut cliquer START / BUILD / RESTART comme si c'etait un objet du monde.
        Button button = hit.collider.GetComponentInParent<Button>();
        if (button != null)
        {
            ClickUIButton(button);
            return;
        }

        UniteSelectionnable selectable = hit.collider.GetComponentInParent<UniteSelectionnable>();
        CombatUnite combat = hit.collider.GetComponentInParent<CombatUnite>();
        SolMiniCarte ground = hit.collider.GetComponentInParent<SolMiniCarte>();

        if (selectable != null)
        {
            manager.SelectUnit(selectable);
            return;
        }

        if (combat != null && combat.team == CombatUnite.Team.Enemy)
        {
            manager.AttackWithSelectedUnit(combat);
            return;
        }

        if (ground != null)
        {
            if (manager.buildSystem != null && manager.buildSystem.IsBuildMode)
                manager.buildSystem.TryPlaceTower(hit.point);
            else
                manager.MoveSelectedUnit(hit.point);
        }
    }

    void ClickUIButton(Button button)
    {
        if (button == null || !button.isActiveAndEnabled || !button.interactable)
            return;

        button.onClick.Invoke();
    }

    Ray GetPointerRay(Transform controllerSource, bool fromMouse)
    {
        // Souris dans l'editeur: rayon depuis la camera vers le curseur.
        Mouse mouse = Mouse.current;
        if (fromMouse && mouse != null && rayCamera != null)
            return rayCamera.ScreenPointToRay(mouse.position.ReadValue());

        // Si la gachette vient d'une manette precise, on utilise cette manette.
        if (controllerSource != null)
            return new Ray(controllerSource.position, controllerSource.forward);

        // Sinon, priorite a la manette droite, puis gauche, puis regard du casque.
        Transform bestController = rightController != null ? rightController : leftController;
        if (bestController != null)
            return new Ray(bestController.position, bestController.forward);

        if (rayCamera != null)
            return new Ray(rayCamera.transform.position, rayCamera.transform.forward);

        return new Ray(transform.position, transform.forward);
    }

    bool WasMousePressed()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    bool WasControllerTriggerPressed(out Transform triggerSource)
    {
        bool rightPressed = WasHandButtonPressed(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightHandDevices,
            UnityEngine.XR.CommonUsages.triggerButton,
            ref lastRightTriggerState);

        bool leftPressed = WasHandButtonPressed(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftHandDevices,
            UnityEngine.XR.CommonUsages.triggerButton,
            ref lastLeftTriggerState);

        if (rightPressed)
        {
            triggerSource = rightController;
            return true;
        }

        if (leftPressed)
        {
            triggerSource = leftController;
            return true;
        }

        triggerSource = null;
        return false;
    }

    bool WasHandButtonPressed(InputDeviceCharacteristics characteristics, List<UnityEngine.XR.InputDevice> devices, InputFeatureUsage<bool> usage, ref bool lastState)
    {
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        bool pressed = false;
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].TryGetFeatureValue(usage, out bool value) && value)
            {
                pressed = true;
                break;
            }
        }

        bool justPressed = pressed && !lastState;
        lastState = pressed;
        return justPressed;
    }

    bool WasAnyControllerButtonPressed(InputFeatureUsage<bool> usage, ref bool lastState)
    {
        controllerDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, controllerDevices);

        bool pressed = false;
        for (int i = 0; i < controllerDevices.Count; i++)
        {
            if (controllerDevices[i].TryGetFeatureValue(usage, out bool value) && value)
            {
                pressed = true;
                break;
            }
        }

        bool justPressed = pressed && !lastState;
        lastState = pressed;
        return justPressed;
    }

    Transform FindTransformByNames(params string[] possibleNames)
    {
        Transform[] transforms = FindObjectsOfType<Transform>();

        for (int i = 0; i < possibleNames.Length; i++)
        {
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name == possibleNames[i])
                    return transforms[j];
            }
        }

        return null;
    }

    void CreatePointerLineIfNeeded()
    {
        if (!showPointerRay || pointerLine != null)
            return;

        GameObject lineObject = new GameObject("Student VR Pointer Ray");
        lineObject.transform.SetParent(transform);
        pointerLine = lineObject.AddComponent<LineRenderer>();
        pointerLine.positionCount = 2;
        pointerLine.startWidth = 0.01f;
        pointerLine.endWidth = 0.003f;
        pointerLine.useWorldSpace = true;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = pointerColor;
        pointerLine.sharedMaterial = material;
    }

    void UpdatePointerLine(Transform triggerSource)
    {
        if (!showPointerRay)
            return;

        CreatePointerLineIfNeeded();
        if (pointerLine == null)
            return;

        Ray ray = GetPointerRay(triggerSource, false);
        pointerLine.SetPosition(0, ray.origin);
        pointerLine.SetPosition(1, ray.origin + ray.direction * rayDistance);
    }
}
