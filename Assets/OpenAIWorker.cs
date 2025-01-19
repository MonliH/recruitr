using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public class OpenAIWorker : MonoBehaviour
{
    [SerializeField] private string apiKey;

    [Serializable]
    public class OpenAIResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    public void GenerateProfile(Profile profile, Action<LinkedInProfile> onComplete)
    {
        StartCoroutine(MakeRequest(profile, onComplete));
    }

    private static Dictionary<string, string[]> CriteriaDescriptions() { return new()
    {
        ["Professionalism"] = new[]
        {
            "(Poor): Consistently displays unprofessional behavior, such as frequent spelling and grammar mistakes, poor presentation, and an underwhelming online presence. Lacks attention to detail and comes across as unreliable or immature",
            "(Fair): Shows some level of professionalism but often falls short in key areas. Presentation and written communication are inconsistent, and their online presence may lack polish. Appears somewhat reliable but could improve in demonstrating maturity",
            "(Good): Generally professional in demeanor, with adequate presentation skills and mostly clear, error-free written communication. Their online presence is decent, though there may be room for improvement. Comes across as reliable and mature in most interactions",
            "(Very Good): Demonstrates a high level of professionalism through polished presentation, well-written communication, and a solid online presence. Pays attention to detail and appears consistently reliable and mature",
            "(Excellent): Exemplifies professionalism in every aspect, with impeccable presentation, flawless written communication, and an impressive online presence. Highly reliable, detail-oriented, and mature, setting a strong example of professionalism"
        },
        ["Excellence"] = new[]
        {
            "(Poor): Minimal achievements or relevant experience. Lacks depth in technical expertise or educational background, with few examples of excellence in past roles",
            "(Fair): Some achievements and relevant experience, but they may be limited in scope or depth. Demonstrates basic technical expertise and education but hasn't consistently excelled in past roles",
            "(Good): A solid record of achievements and relevant experience, with a reasonable depth of technical expertise and education. Demonstrates capability but may not stand out significantly in terms of excellence",
            "(Very Good): A strong record of achievements, with substantial relevant experience and well-developed technical expertise. Educational background supports their experience, and they have demonstrated excellence in several past roles",
            "(Excellent): An outstanding track record of achievements, with extensive relevant experience, advanced technical expertise, and a strong educational background. Consistently demonstrates excellence and has clearly developed key skills to a high degree"
        },
        ["Relevance"] = new[]
        {
            "(Poor): Little to no alignment between the candidate's qualifications, work experience, and the job role for full-stack development at Google. Their background does not support the responsibilities or expectations of the position, making the transition challenging",
            "(Fair): Some relevant qualifications and experience, but there are significant gaps. The candidate might struggle to meet all the job's expectations and responsibilities of a full-stack engineer at Google due to a lack of direct alignment",
            "(Good): Generally relevant qualifications and experience, with most of the necessary skills to support the role. There may be minor gaps, but the candidate appears capable of transitioning into the role with some adjustment",
            $"(Very Good): Strongly aligned qualifications, work experience, and skills with the full-stack job role at Google (e.g., work experience at {NameGenerator.GenerateCompanies()}). The candidate's background supports the responsibilities and expectations, making them well-suited for a smooth transition",
            $"(Excellent): Exceptional alignment of qualifications, experience, and skills with the full-stack position at Google (e.g., work experience at {NameGenerator.GenerateCompanies()}). The candidate's background directly supports every aspect of the role, indicating a seamless and highly effective transition into the position"
        }
    };}

    private IEnumerator MakeRequest(Profile profile, Action<LinkedInProfile> onComplete)
    {
        string prompt = @"Help me write a Linkedin profile (in JSON) for a person with name " + NameGenerator.GenerateName() + @" who is a prospective applicant for a full-stack role at Google.\nCandidate LinkedIn Post Generation Instructions:\nProfessionalism: Assessing presentation, written communication, and overall maturity\n"
                        +CriteriaDescriptions()["Professionalism"][profile.prescreenStats.professionalism-1]
                        +@"\nExcellence: Evaluating achievements, experience, and education\n"
                        +CriteriaDescriptions()["Excellence"][profile.prescreenStats.excellence-1]
                        +@"\nRelevance to Position: Alignment of qualifications with job role\n"
                        +CriteriaDescriptions()["Relevance"][profile.prescreenStats.relevance-1]
                        +@"\nBased on the values from the stats and provided user personality and profile, generate a professional LinkedIn profile. Use REAL company and university names (e.g., "+NameGenerator.GenerateUniversity()+@"), DO NOT put names like XYZ or ABC. Tailor the tone, content, and highlights according to the specific details of each stat. Ensure the post reflects the candidate's experience, skills, achievements, and overall professional brand. These posts should embody typical 'cringe' content. Each post should feature elements like humblebrags, overly inspirational stories, excessive use of buzzwords, and contrived personal achievements. Include phrases like 'I'm humbled to announce,' 'grateful for this opportunity,' and 'after much reflection.' Incorporate hashtags such as #blessed, #grind, #success, and #leadership. Ensure the tone is overly formal, self-promotional, and designed to attract engagement through likes and comments. Also make sure to include plenty of emojis.";

        string jsonRequest = @"{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {
                    ""role"": ""user"",
                    ""content"": [
                        {
                            ""type"": ""text"",
                            ""text"": """ + prompt + @"""
                        }
                    ]
                }
            ],"+@"
""response_format"": {
    ""type"": ""json_schema"",
    ""json_schema"": {
      ""name"": ""linkedin_user"",
      ""strict"": true,
      ""schema"": {
        ""type"": ""object"",
        ""properties"": {
          ""description"": {
            ""type"": ""string"",
            ""description"": ""A brief description about the user.""
          },
          ""connections"": {
            ""type"": ""number"",
            ""description"": ""Number of connections the user has on LinkedIn.""
          },
          ""name"": {
            ""type"": ""string"",
            ""description"": ""The name of the user.""
          },
          ""maleGender"": {
            ""type"": ""boolean"",
            ""description"": ""Whether the candidate identifies as male gender or not.""
          },
          ""education"": {
            ""type"": ""string"",
            ""description"": ""The user's latest education (name of university and degree)""
          },
          ""experiences"": {
            ""type"": ""array"",
            ""description"": ""List of work experiences of the user."",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""date"": {
                  ""type"": ""string"",
                  ""description"": ""The date when the experience took place.""
                },
                ""company"": {
                  ""type"": ""string"",
                  ""description"": ""The name of the company where the user worked.""
                },
                ""description"": {
                  ""type"": ""string"",
                  ""description"": ""A brief description of the role or responsibilities at the company.""
                }
              },
              ""required"": [
                ""date"",
                ""company"",
                ""description""
              ],
              ""additionalProperties"": false
            }
          },
          ""linkedin_posts"": {
            ""type"": ""array"",
            ""description"": ""User's posts on LinkedIn."",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""text"": {
                  ""type"": ""string"",
                  ""description"": ""The content of the LinkedIn post.""
                },
                ""date"": {
                  ""type"": ""string"",
                  ""description"": ""The date when the post was created.""
                }
              },
              ""required"": [
                ""text"",
                ""date""
              ],
              ""additionalProperties"": false
            }
          },
          ""skills"": {
            ""type"": ""array"",
            ""description"": ""List of skills possessed by the user along with endorsements."",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""skill"": {
                  ""type"": ""string"",
                  ""description"": ""The name of the skill.""
                },
                ""endorsements"": {
                  ""type"": ""number"",
                  ""description"": ""The number of endorsements received for this skill.""
                }
              },
              ""required"": [
                ""skill"",
                ""endorsements""
              ],
              ""additionalProperties"": false
            }
          }
        },
        ""required"": [
          ""description"",
          ""name"",
          ""experiences"",
          ""linkedin_posts"",
          ""skills"",
          ""maleGender"",
          ""education"",
          ""connections""
        ],
        ""additionalProperties"": false
      }
    }
  }
}";

        using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(request.downloadHandler.text);
                if (response.choices != null && response.choices.Length > 0)
                {
                    string profileJson = response.choices[0].message.content;
                    LinkedInProfile generatedLinkedin = JsonUtility.FromJson<LinkedInProfile>(profileJson);
                    onComplete?.Invoke(generatedLinkedin);
                }
            }
            else
            {
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                onComplete?.Invoke(null);
            }
        }
    }
}