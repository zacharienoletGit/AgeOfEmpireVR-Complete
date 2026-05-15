using UnityEngine;

// Sources consultees:
// - AudioSource: https://docs.unity3d.com/ScriptReference/AudioSource.html
// - AudioClip: https://docs.unity3d.com/ScriptReference/AudioClip.html
// Aide utilisee: Codex a donne les grandes lignes pour centraliser les sons dans un script simple.
// GestionAudio centralise les sons du prototype.
// Les AudioClip peuvent etre assignes dans l'inspecteur si on ajoute des sons au projet plus tard.
public class GestionAudio : MonoBehaviour
{
    public static GestionAudio Instance { get; private set; }

    // Clips optionnels. Si un clip est vide, le jeu continue sans erreur.
    public AudioClip buttonClip;
    public AudioClip buildClip;
    public AudioClip hitClip;
    public AudioClip deathClip;

    // Source audio utilisee pour jouer les sons en OneShot.
    AudioSource source;

    void Awake()
    {
        // Singleton simple pour appeler GestionAudio.Instance depuis n'importe quel script.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        source = GetComponent<AudioSource>();

        // Si le GameObject n'a pas encore d'AudioSource, on en ajoute une automatiquement.
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
    }

    public void PlayButton()
    {
        // Son quand le joueur clique sur un bouton UI.
        Play(buttonClip);
    }

    public void PlayBuild()
    {
        // Son quand une tour est construite.
        Play(buildClip);
    }

    public void PlayHit()
    {
        // Son quand une unite attaque ou touche une cible.
        Play(hitClip);
    }

    public void PlayDeath()
    {
        // Son quand un ennemi meurt.
        Play(deathClip);
    }

    void Play(AudioClip clip)
    {
        // Si aucun son gratuit n'est encore importe, on ne fait rien.
        // Ca evite les erreurs NullReferenceException pendant le developpement.
        if (clip == null || source == null)
            return;

        source.PlayOneShot(clip);
    }
}
