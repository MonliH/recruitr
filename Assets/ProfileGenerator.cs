using UnityEngine;
using System.Linq;

static class RandomExtensions
{
    public static void Shuffle<T>(this System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}

public class ProfileGenerator : MonoBehaviour
{
    public Profile[] profiles;
    public int totalProfiles;
    public OpenAIWorker openAiWorker;
    public GameObject[] malePrefabs;
    public GameObject[] femalePrefabs;

    private bool generated = false;
    [SerializeField] GameManager gameManager;

    void Start()
    {
        profiles = new Profile[totalProfiles];
        for (int i = 0; i < totalProfiles; i++)
        {
            GenerateProfile(i);
        }
    }

    void GenerateProfile(int i)
    {
        Profile profile = new Profile();

        PrescreenStats prescreenStats = new PrescreenStats
        {
            professionalism = Random.Range(1, 6),
            excellence = Random.Range(1, 6),
            relevance = Random.Range(1, 6)
        };
        CoffeeChatStats coffeeChatStats = new CoffeeChatStats
        {
            rizzAndFluency = Random.Range(1, 4),
            problemSolving = Random.Range(1, 4),
            interviewability = Random.Range(1, 4)
        };

        profile.prescreenStats = prescreenStats;
        profile.coffeeChatStats = coffeeChatStats;
        profile.totalScore = prescreenStats.professionalism + prescreenStats.excellence + prescreenStats.relevance + coffeeChatStats.rizzAndFluency + coffeeChatStats.problemSolving + coffeeChatStats.interviewability;

        openAiWorker.GenerateProfile(profile, linkedInProfile =>
        {
            profile.linkedInProfile = linkedInProfile;
            profiles[i] = profile;

            if (profiles.All(p => p != null && p.linkedInProfile != null))
            {
                AllProfilesGenerated();
            }
        });
    }

    void AllProfilesGenerated()
    {
        if (generated) return;

        generated = true;
        Debug.Log("All profiles generated!");

        // shuffle profiles
        var rng = new System.Random();
        rng.Shuffle(malePrefabs);
        rng.Shuffle(femalePrefabs);

        rng.Shuffle(VoiceNameExtensions.femaleVoices);
        rng.Shuffle(VoiceNameExtensions.maleVoices);

        for (int i = 0; i < totalProfiles; i++)
        {
            Profile profile = profiles[i];
            GameObject prefab = profile.linkedInProfile.maleGender ? malePrefabs[i % malePrefabs.Length] : femalePrefabs[i % femalePrefabs.Length];
            var voice = profile.linkedInProfile.maleGender ? VoiceNameExtensions.maleVoices[i % VoiceNameExtensions.maleVoices.Length] : VoiceNameExtensions.femaleVoices[i % VoiceNameExtensions.femaleVoices.Length];
            profile.prefab = prefab;
            profile.voiceName = voice;
            profile.linkedInProfile.profileImage = prefab.GetComponent<Headshots>().headshot;
        }

        gameManager.FinishProfileGen();
    }
}
