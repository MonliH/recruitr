using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PersonResult : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statsText;
    public Image profileImage;
    public Image bg;
    public Profile profile;
    public bool hired;
    public bool interviewed;

    public void Start()
    {
        nameText.text = profile.linkedInProfile.name;
        profileImage.sprite = profile.linkedInProfile.profileImage;
        scoreText.text = profile.totalScore.ToString() + "/24";
        int totalPrescreen = profile.prescreenStats.professionalism + profile.prescreenStats.excellence + profile.prescreenStats.relevance;
        statsText.text = $"Background: {totalPrescreen}/15 | Rizz: {profile.coffeeChatStats.rizzAndFluency}/3 | Smarts: {profile.coffeeChatStats.problemSolving}/3";

        if (hired)
        {
            bg.color = new Color(106, 255, 0, 255);
        }
        else if (interviewed)
        {
            bg.color = new Color(0, 202, 255, 255);
        }
    }
}
