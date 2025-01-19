using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class LinkedInProfile
{
    public string description;
    public string name;
    public string education;
    public bool maleGender;
    public int connections;
    public Sprite profileImage;

    public Experience[] experiences;
    public Post[] linkedin_posts;
    public Skill[] skills;

    [Serializable]
    public class Experience
    {
        public string date;
        public string company;
        public string description;
    }
    [Serializable]
    public class Post
    {
        public string text;
        public string date;
    }

    [Serializable]
    public class Skill
    {
        public string skill;
        public int endorsements;
    }

    public string ToNarrativeString()
    {
        var sb = new System.Text.StringBuilder();

        // Basic Info and Education
        sb.Append($"You are {name}. {description} ");
        
        if (!string.IsNullOrEmpty(education))
        {
            sb.Append($"You studied at {education}. ");
        }

        // Experience
        if (experiences != null && experiences.Length > 0)
        {
            if (experiences.Length == 1)
            {
                var exp = experiences[0];
                sb.Append($"You work at {exp.company} since {exp.date}, where {exp.description.ToLower()}. ");
            }
            else
            {
                sb.Append("Your work experience includes ");
                for (int i = 0; i < experiences.Length; i++)
                {
                    var exp = experiences[i];
                    if (i == experiences.Length - 1)
                    {
                        sb.Append($"and {exp.company} ({exp.date}) where {exp.description.ToLower()}. ");
                    }
                    else if (i == experiences.Length - 2)
                    {
                        sb.Append($"{exp.company} ({exp.date}) where {exp.description.ToLower()} ");
                    }
                    else
                    {
                        sb.Append($"{exp.company} ({exp.date}) where {exp.description.ToLower()}, ");
                    }
                }
            }
        }

        // Skills - simple comma-separated list
        if (skills != null && skills.Length > 0)
        {
            var skillsList = string.Join(", ", skills.Select(s => s.skill));
            sb.Append($"Your skills include {skillsList}.");
        }

        return sb.ToString();
    }
}

public class Profile
{
    public LinkedInProfile linkedInProfile;
    public PrescreenStats prescreenStats;
    public CoffeeChatStats coffeeChatStats;
    public VoiceName voiceName;
    public GameObject prefab;
    public GameObject instance;
    public int totalScore;
}

[Serializable]
public class PrescreenStats
{
    public int professionalism;
    public int excellence;
    public int relevance;
}

[Serializable]
public class CoffeeChatStats
{
    public int rizzAndFluency;
    public int problemSolving;
    public int interviewability;
}