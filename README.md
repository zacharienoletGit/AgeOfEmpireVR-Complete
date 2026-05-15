# Empire Miniature VR

Projet final - Environnements immersifs  
Hiver 2026 - 420-6B3-VI

Nom : Zacharie Nolet
DA : 2012487
---------------------------------------------------------------
## Genre

RTS / Tower Defense en réalité virtuelle pour casque Meta Quest.

## Objectif

Le joueur doit protéger son Hôtel de Ville et survivre à plusieurs vagues d'ennemis. Il peut sélectionner des unités, les déplacer sur une carte miniature et construire des défenses simples.

## Contrôles

- Pointer une unité avec le contrôleur droit.
- Appuyer sur la gâchette pour sélectionner.
- Pointer le sol et appuyer sur la gâchette pour déplacer l'unité.
- Pointer un ennemi et appuyer sur la gâchette pour attaquer.
- Utiliser les boutons du menu VR pour commencer, construire ou recommencer.

## Éléments du projet

- Menu principal avec titre et bouton pour commencer.
- Environnement médiéval miniature adapté au genre RTS.
- Interactions VR avec raycast du contrôleur.
- Objectif clair affiché au joueur.
- Sons liés aux actions importantes, comme construire ou attaquer.
- Écran de Game Over avec bouton pour recommencer facilement.

## Bonnes pratiques VR utilisées

- Le joueur reste principalement immobile devant une carte miniature pour limiter le motion sickness.
- Les menus sont placés en World Space avec de gros boutons faciles à viser.
- Les contrôles sont simples et basés sur le pointage naturel du contrôleur.
- L'objectif du jeu est visible dès le début.
- Les déplacements rapides de caméra sont évités.

## Sources utilisées

- Cours Environnements immersifs - Frédérik Taleb, Cégep de Victoriaville : http://envimmersif-cegepvicto.github.io/
- Projet intégrateur 1 - Alexandre Ouellet : https://cours-alexandre-ouellet.github.io/projet-integrateur-1/
- Unity Documentation - Develop for Meta Quest workflow : https://docs.unity.cn/6000.2/Documentation/Manual/xr-meta-quest-develop.html
- Unity Documentation - Meta OpenXR package setup : https://docs.unity.cn/Packages/com.unity.xr.meta-openxr%402.1/manual/get-started.html
- Unity Documentation - XR Interaction Toolkit : https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit%403.1/manual/index.html
- Unity Scripting API - MonoBehaviour : https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
- Unity Scripting API - GameObject : https://docs.unity3d.com/ScriptReference/GameObject.html
- Unity Scripting API - Physics.Raycast : https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Unity Scripting API - AudioSource : https://docs.unity3d.com/ScriptReference/AudioSource.html
- Unity Scripting API - SceneManager : https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html
- Unity Manual - UI system : https://docs.unity3d.com/Manual/UIToolkits.html
- Unity Input System package : https://docs.unity3d.com/Packages/com.unity.inputsystem%401.19/manual/index.html
- OpenAI ChatGPT / Codex : utilisé comme aide ponctuelle pour organiser les idées, comparer des approches de scripts et retrouver les pages de documentation Unity à vérifier.

## Sources du code

Le code n'est pas copié-collé d'un site internet. Il est fait pour le projet, mais il s'inspire de la documentation Unity pour les classes et méthodes comme `MonoBehaviour`, `GameObject`, `Physics.Raycast`, `AudioSource`, `SceneManager`, `Coroutine`, `Canvas`, `Button` et le `Input System`.

Les commentaires au début des scripts citent les sources principales utilisées. L'aide IA a servi de support de planification et de relecture, puis les scripts ont été adaptés et intégrés dans Unity selon les besoins du prototype.

## Simulateur et boutons

Les contrôles suivants sont disponibles pour tester le prototype dans l'éditeur Unity.

- `START` : commence la partie.
- `BUILD` : active le mode construction.
- `RESTART` : recommence la scène.
- `Space` : start dans le simulateur.
- `B` : build dans le simulateur.
- `R` : restart dans le simulateur.
- clic gauche : remplace la gâchette pour tester sélection / déplacement / attaque.

Avec un casque VR, la gâchette droite sert normalement à pointer et cliquer. Le clic souris reste la façon la plus simple pour tester dans l'éditeur.
