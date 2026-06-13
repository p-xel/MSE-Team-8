using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Sound Clips")]
    [SerializeField] private AudioClip cardSwapSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip cardDrawSound;
    [SerializeField] private AudioClip knockSound;
    [SerializeField] private AudioClip shootSound;

    private AudioSource audioSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        AudioListener.volume = Session.Volume;
    }

    public static void PlayCardSwap()
    {
        var inst = Instance;
        if (inst != null && inst.cardSwapSound != null)
        {
            inst.PlayClip(inst.cardSwapSound);
        }
    }

    public static void PlayButtonClick()
    {
        var inst = Instance;
        if (inst != null && inst.buttonClickSound != null)
        {
            inst.PlayClip(inst.buttonClickSound);
        }
    }

    public static void PlayCardDraw()
    {
        var inst = Instance;
        if (inst != null && inst.cardDrawSound != null)
        {
            inst.PlayClip(inst.cardDrawSound);
        }
    }

    public static void PlayKnock()
    {
        var inst = Instance;
        if (inst != null && inst.knockSound != null)
        {
            inst.PlayClip(inst.knockSound);
        }
    }

    public static void PlayShoot()
    {
        var inst = Instance;
        if (inst != null && inst.shootSound != null)
        {
            inst.PlayClip(inst.shootSound);
        }
    }

    public static void Play(AudioClip clip)
    {
        if (Instance != null && clip != null)
        {
            Instance.PlayClip(clip);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
