using System;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    public GameObject profileHeader;
    public GameObject aboutMe;
    public GameObject experience;
    public GameObject posts;
    public GameObject skills;

    public GameObject experiencePrefab;
    public GameObject postPrefab;
    public GameObject skillPrefab;

    public Image profileImage;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI connectionsText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI educationHeaderText;
    public TextMeshProUGUI experienceHeaderText;

    public Profile fullProfile;
    private LinkedInProfile profile;

    private GameManager gameManager;

    private bool setExperienceHeader = false;

    public void GenerateUI()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        profile = fullProfile.linkedInProfile;
        profileImage.sprite = profile.profileImage;
        nameText.text = profile.name;
        connectionsText.text = (profile.connections >= 500) ? "500+ connections" : profile.connections.ToString() + " connections";
        descriptionText.text = profile.description;
        educationHeaderText.text = profile.education;
        if (profile.experiences != null && profile.experiences.Length > 0)
        {
            experienceHeaderText.text = "Experience";
            foreach (LinkedInProfile.Experience exp in profile.experiences)
            {
                if (!setExperienceHeader)
                {
                    experienceHeaderText.text = exp.company;
                    setExperienceHeader = true;
                }
                var expObj = Instantiate(experiencePrefab, experience.transform);
                
                var textBoxes = expObj.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var textBox in textBoxes)
                {
                    if (textBox.tag == "ListElementHeader")
                    {
                        textBox.text = exp.company;
                    }
                    else if (textBox.tag == "ListElementDate")
                    {
                        textBox.text = exp.date;
                    }
                    else if (textBox.tag == "ListElementText")
                    {
                        textBox.text = exp.description;
                    }
                }
            }
        }
        if (profile.linkedin_posts != null && profile.linkedin_posts.Length > 0)
        {
            foreach (var post in profile.linkedin_posts)
            {
                var postObj = Instantiate(postPrefab, posts.transform);
                
                var textBoxes = postObj.GetComponentsInChildren<TextMeshProUGUI>();

                foreach (var textBox in textBoxes)
                {
                    if (textBox.tag == "ListElementDate")
                    {
                        textBox.text = post.date;
                    }
                    else if (textBox.tag == "ListElementText")
                    {
                        textBox.text = post.text;
                    }
                }
            }
        }
        if (profile.skills != null && profile.skills.Length > 0)
        {
            foreach (var skill in profile.skills)
            {
                var skillObj = Instantiate(skillPrefab, skills.transform);
                
                var textBoxes = skillObj.GetComponentsInChildren<TextMeshProUGUI>();

                foreach (var textBox in textBoxes)
                {
                    if (textBox.tag == "ListElementHeader")
                    {
                        textBox.text = skill.skill;
                    }
                    else if (textBox.tag == "ListElementText")
                    {
                        textBox.text = skill.endorsements.ToString() + " endorsments";
                    }
                }
            }
        }
    }
    public void AcceptProfile()
    {
        gameManager.AcceptProfile(fullProfile);
    }
    public void RejectProfile()
    {
        gameManager.RejectProfile();
    }
}
