using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using System;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private ProfileGenerator profileGenerator;
    [SerializeField] private GameObject profileUIPrefab;
    [SerializeField] private Transform profileUIParent;
    [SerializeField] private TransitionScreen transitionScreen;
    [SerializeField] private TransitionScreen hireScreen;
    [SerializeField] private Transform hireUIParent;
    [SerializeField] private GameObject hirePrefab;
    [SerializeField] private GameObject endPrefab;
    [SerializeField] private Transform endUIParent;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TransitionScreen endScreen;

    private Queue<GameObject> profileUIs = new Queue<GameObject>();

    public ScrollRect scrollView;

    public List<Profile> acceptedProfiles = new List<Profile>();
    public int maxAcceptedProfiles = 3;

    private bool createdProfile = false;

    public string openaiApiKey;
    public float totalTime = 10f;
    public TextMeshProUGUI timerText;
    public int activeProfileIndex = 0;
    private float timeLeft;
    private GameObject personPanel;

    public AudioSource timerTick;
    private bool playedTimerTick;

    public void FinishProfileGen()
    {
        foreach (Profile profile in profileGenerator.profiles)
        {
            GameObject profileUIobj = Instantiate(profileUIPrefab, profileUIParent);
            ProfileUI profileUI = profileUIobj.GetComponent<ProfileUI>();
            profileUI.fullProfile = profile;
            profileUI.GenerateUI();
            if (createdProfile)
            {
                profileUIobj.SetActive(false);
            }
            else
            {
                createdProfile = true;
                scrollView.content = profileUIobj.GetComponent<RectTransform>();
            }
            profileUIs.Enqueue(profileUIobj);
        }
        loadingScreen.SetActive(false);

        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        timerText.gameObject.SetActive(true);
        timeLeft = 45f;
        playedTimerTick = false;
        while (timeLeft > 0 && acceptedProfiles.Count < maxAcceptedProfiles)
        {
            timeLeft -= Time.deltaTime;
            timerText.text = Math.Max(timeLeft, 0f).ToString("F2");
            if (timeLeft < 10 && !playedTimerTick)
            {
                timerTick.Play();
                playedTimerTick = true;
            }
            yield return null;
        }
    }

    public void AcceptProfile(Profile profile)
    {
        acceptedProfiles.Add(profile);
        if (acceptedProfiles.Count >= maxAcceptedProfiles)
        {
            transitionScreen.ChangeText("Coffee Chat #1");
            transitionScreen.ScreenDown();
            profileUIs.Peek().SetActive(false);
            StartCoroutine(StartInterview());
        }
        else
        {
            NextProfile();
        }
    }

    public void RejectProfile()
    {
        NextProfile();
    }

    private void NextProfile()
    {
        profileUIs.Peek().SetActive(false);
        profileUIs.Dequeue();
        if (profileUIs.Count == 0)
        {
            FinishProfileGen();
        }
        else
        {
            profileUIs.Peek().SetActive(true);
            scrollView.content = profileUIs.Peek().GetComponent<RectTransform>();
        }
    }
    public void StartHiring()
    {
        foreach (Profile profile in acceptedProfiles)
        {
            GameObject hireUIobj = Instantiate(hirePrefab, hireUIParent);
            hireUIobj.GetComponent<ChoosePerson>().profile = profile;
        }
        hireScreen.ScreenDown();
    }

    public void HireProfile(Profile hiredProfile)
    {
        hireScreen.ScreenUp();
        foreach (Profile profile in profileGenerator.profiles.OrderBy(p => p.totalScore, Comparer<int>.Create((a, b) => b.CompareTo(a))))
        {
            GameObject endUIobj = Instantiate(endPrefab, endUIParent);
            PersonResult personResult = endUIobj.GetComponent<PersonResult>();
            personResult.profile = profile;

            if (profile == hiredProfile)
            {
                personResult.hired = true;
            }
            else if (acceptedProfiles.Select(i => i.linkedInProfile.name).Contains(profile.linkedInProfile.name))
            {
                personResult.interviewed = true;
            }
        }
        if (hiredProfile.totalScore >= 20)
        {
            gradeText.text = "S";
        }
        else if (hiredProfile.totalScore >= 16)
        {
            gradeText.text = "A";
        }
        else if (hiredProfile.totalScore >= 12)
        {
            gradeText.text = "B";
        }
        else if (hiredProfile.totalScore >= 8)
        {
            gradeText.text = "C";
        }
        else
        {
            gradeText.text = "D";
        }
        endScreen.ScreenDown();
    }

    IEnumerator StartInterview()
    {
        profileUIs.Peek().SetActive(false);
        var currentProfile = acceptedProfiles[activeProfileIndex];
        GameObject profileUIobj = Instantiate(profileUIPrefab, profileUIParent);
        ProfileUI profileUI = profileUIobj.GetComponent<ProfileUI>();
        profileUI.fullProfile = currentProfile;
        profileUI.GenerateUI();
        profileUIobj.SetActive(true);
        GameObject[] buttons = GameObject.FindGameObjectsWithTag("Check Buttons");
        buttons[0].SetActive(false);
        buttons[1].SetActive(false);
        scrollView.content = profileUIobj.GetComponent<RectTransform>();

        GameObject candidateCharacter = Instantiate(currentProfile.prefab, new Vector3(-10.592f, -0.41f, -1.696f), Quaternion.identity);
        currentProfile.instance = candidateCharacter;
        DataManager.ActiveProfile = acceptedProfiles[activeProfileIndex];
        yield return SceneManager.LoadSceneAsync("Chat", LoadSceneMode.Additive);
        yield return new WaitForSeconds(3f);
        transitionScreen.ScreenUp();

        // update timer text
        timerText.gameObject.SetActive(true);
        timeLeft = totalTime;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            timerText.text = Math.Max(timeLeft, 0f).ToString("F2");
            yield return null;
        }

        activeProfileIndex++;
        transitionScreen.ChangeText("Coffee Chat #" + (activeProfileIndex + 1));
        if (activeProfileIndex < acceptedProfiles.Count)
        {
            transitionScreen.ScreenDown();
        }
        profileUIobj.SetActive(false);
        yield return new WaitForSeconds(1f);
        Destroy(candidateCharacter);
        yield return SceneManager.UnloadSceneAsync("Chat");
        if (activeProfileIndex < acceptedProfiles.Count)
        {
            yield return StartInterview();
        }
        else
        {
            transitionScreen.ScreenUp();
            StartHiring();
        }
    }
}
