using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoosePerson : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image profileImage;
    public Profile profile;

    public void Start()
    {
        nameText.text = profile.linkedInProfile.name;
        profileImage.sprite = profile.linkedInProfile.profileImage;
    }

    public void SelectPerson()
    {
        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>().HireProfile(profile);
    }
}
