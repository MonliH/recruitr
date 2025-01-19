using UnityEngine;
using System.Reflection;
using System.ComponentModel;
using Unity.WebRTC;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public enum VoiceName
{
    // alloy, ash, ballad, coral, echo sage, shimmer and verse
    [Description("alloy")]
    Alloy = 0, // female
    [Description("ash")]
    Ash = 1,  // male
    [Description("ballad")]
    Ballad = 2, // female
    [Description("coral")]
    Coral = 3, // female
    [Description("echo")]
    Echo = 4, // male
    [Description("sage")]
    Sage = 5, // female
    [Description("shimmer")]
    Shimmer = 6, // female
    [Description("verse")]
    Verse = 7 // male
}

public static class VoiceNameExtensions
{
    public static VoiceName[] maleVoices = new VoiceName[] { VoiceName.Ash, VoiceName.Echo, VoiceName.Verse };
    public static VoiceName[] femaleVoices = new VoiceName[] { VoiceName.Alloy, VoiceName.Ballad, VoiceName.Coral, VoiceName.Sage, VoiceName.Shimmer };

    public static string ToVoiceString(this VoiceName voice)
    {
        return voice.ToString().ToLower();
    }
}

public class OpenAIVoiceChat : MonoBehaviour
{
    [SerializeField] public string apiKey = "api";
    public AudioSource outputSource;
    public Profile profile;

    private RTCPeerConnection peerConnection;
    private RTCDataChannel dataChannel;
    public AudioStreamTrack microphoneTrack;
    private RTCRtpSender audioSender;
    private string ephemeralKey;

    private const string OPENAI_SESSION_URL = "https://api.openai.com/v1/realtime/sessions";
    private const string OPENAI_BASE_URL = "https://api.openai.com/v1/realtime";
    private const string MODEL_ID = "gpt-4o-mini-realtime-preview-2024-12-17";

    public AudioSource microphoneSource;
    private MediaStream sendStream;

    const int FREQUENCY = 44100;
    AudioClip mic;

    private GameObject bro;

    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private GameObject broHead;
    private GameObject broNeck;

    [SerializeField] private float updateInterval = 0.1f; // How often to update the intensity
    [SerializeField] private int sampleDataLength = 1024; // Length of audio sample data to analyze

    private float[] sampleData;


    public void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "api")
        {
            Debug.LogError("Please set your OpenAI API key in the inspector!");
            return;
        }

        Debug.Log($"Microphone.devices: {string.Join(", ", Microphone.devices)}");
        Debug.Log($"Microphone position: {Microphone.GetPosition(null)}");

        if (DataManager.ActiveProfile != null) {
            profile = DataManager.ActiveProfile;
        }

        bro = profile.instance;
        sampleData = new float[sampleDataLength];
        broHead = bro.transform.Find("Armature/Hips/Spine/Spine1/Spine2/Neck/Head").gameObject;
        broNeck = bro.transform.Find("Armature/Hips/Spine/Spine1/Spine2/Neck").gameObject;
        originalPosition = broNeck.transform.position;
        originalRotation = broHead.transform.eulerAngles;

        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return InitializeSession();
    }

    private IEnumerator InitializeSession()
    {
        var request = new UnityWebRequest(OPENAI_SESSION_URL, "POST");
        var sessionRequest = new SessionRequest
        {
            model = MODEL_ID,
            voice = "shimmer"
        };
        string jsonBody = JsonUtility.ToJson(sessionRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to get session token: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
        ephemeralKey = response.client_secret.value;

        yield return SetupWebRTC();
        Debug.Log("WebRTC setup complete");

        yield return CreateAndSendOffer();
    }

    void Update()
    {
        // read from output source and transform to bro
        if (outputSource != null && bro != null)
        {
            outputSource.GetSpectrumData(sampleData, 0, FFTWindow.BlackmanHarris);

            float sum = 0f;

            // Calculate the sum of all frequency intensities
            for (int i = 0; i < sampleDataLength; i++)
            {
                sum += sampleData[i] * sampleData[i]; // Square to get power
            }

            // Calculate RMS (Root Mean Square) for average intensity
            float intensity = Mathf.Sqrt(sum / sampleDataLength);
            broNeck.transform.position = originalPosition + new Vector3(0, intensity * 10f, 0);
            broHead.transform.eulerAngles = new Vector3(intensity * 100f * 70f, 0, 0) + originalRotation;
            // use lerp to smooth out the movement
            // broHead.transform.eulerAngles = Vector3.Lerp(broHead.transform.eulerAngles, new Vector3(intensity * 100f * 45f, 0, 0) + originalRotation, Time.deltaTime * 5f);
        }
    }

    private IEnumerator SetupWebRTC()
    {
        RTCConfiguration config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        peerConnection = new RTCPeerConnection(ref config);

        // Create data channel
        dataChannel = peerConnection.CreateDataChannel("oai-events", new RTCDataChannelInit { ordered = true });
        dataChannel.OnOpen = () =>
        {
            Debug.Log("Data channel opened");
            SendInitialPrompt();
        };
        dataChannel.OnMessage = bytes =>
        {
            var message = Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message from OpenAI: {message}");
            HandleServerMessage(message);
        };

        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogError("Microphone access not granted!");
            yield break;
        }

        // Get the default microphone
        string micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micDevice == null)
        {
            Debug.LogError("No microphone found!");
            yield break;
        }

        Debug.Log("Using microphone: " + micDevice);
        mic = Microphone.Start(micDevice, true, 1, FREQUENCY);

        Debug.Log(mic);

        microphoneSource.clip = mic;
        microphoneSource.loop = true;
        microphoneSource.Play();

        microphoneTrack = new AudioStreamTrack(microphoneSource);

        // Add track to peer connection
        sendStream = new MediaStream();
        audioSender = peerConnection.AddTrack(microphoneTrack, sendStream);

        // Handle incoming audio
        peerConnection.OnTrack = e =>
        {
            Debug.Log("Received audio track from OpenAI??");
            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log("Received audio track from OpenAI");
                outputSource.SetTrack(audioTrack);
                outputSource.loop = true;
                outputSource.Play();
            }
        };

        yield return null;
    }

    private IEnumerator CreateAndSendOffer()
    {
        Debug.Log("Creating WebRTC offer...");

        // Create offer
        RTCSessionDescriptionAsyncOperation op = peerConnection.CreateOffer();
        yield return op;

        if (op.IsError)
        {
            Debug.LogError($"Failed to create offer: {op.Error.message}");
            yield break;
        }

        Debug.Log($"Created offer SDP: {op.Desc.sdp}");

        // Set local description
        var offerDesc = new RTCSessionDescription { type = op.Desc.type, sdp = op.Desc.sdp };
        var setLocalOp = peerConnection.SetLocalDescription(ref offerDesc);
        yield return setLocalOp;

        // Send offer to OpenAI
        var request = UnityWebRequest.PostWwwForm($"{OPENAI_BASE_URL}?model={MODEL_ID}", "");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(op.Desc.sdp));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/sdp");
        request.SetRequestHeader("Authorization", $"Bearer {ephemeralKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to get answer: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        // Set remote description
        var answer = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = request.downloadHandler.text };
        var setRemoteOp = peerConnection.SetRemoteDescription(ref answer);
        yield return setRemoteOp;
    }


    private static readonly Dictionary<string, string[]> CriteriaDescriptions = new()
    {
        ["RizzAndFluency"] = new[]
        {
            "Howerver, you stammer a lot, make awkward pauses, and are generally unconvincing. You may come across as nervous or unsure of yourself, and use a lot of 'um' and 'uhh'. Your pacing is weird and you often repeat yourself or speak too quickly.",
            "You show some confidence, but you often hesitate or lack smoothness in communication. You don't sound natural and occasionally use 'um' and 'uhh'. Sometimes, not too often, you speak too quickly or awkwardly laugh.",
            "You are generally confident and smooth in communication, with minimal hesitation or awkward pauses. You come across as charming and engaging, with a natural flow to your speech. You sound confident.",
        },
        ["Interviewability"] = new[]
        {
            "However, you never answer the question asked of you by the interviewer. Whenever you are asked something, you intentionally misinterpret it and provide a few sentences before going on a complete tangent and speaking about something completely different.",
            "Once every 3 questions, misinterpret it and provide a completely random answer and lose focus after speaking a few sentences, going off topic.",
            "",
        },
        ["ProblemSolving"] = new[]
        {
            "You act like someone who hasn't learned to code and say you don't know for almost all the technical questions that are asked of you. Sound like someone who is not technical and do not say any semblance of the correct answer. For example, if someone asks you about a class in Python say a class is a group of people learning Python. As another example, if asked about a linked list, say that you would open your reminders app and drag the reminders in a different order.",
            "You give clearly wrong answers to technical interview problems. You act like someone who is new to coding, and don't have deep technical knowledge.",
            "You effectively identify and analyze problems, proposing reasonable solutions.",
        }
    };

    private void SendInitialPrompt()
    {
        if (dataChannel?.ReadyState != RTCDataChannelState.Open) return;

        var prompt = new SessionCreate
        {
            type = "session.update",
            session = new SessionData
            {
                instructions = "Act like an interview candidate for a tech position. The job you are applying to is a fullstack engineer at Google working on distributed systems. You are not a perfect candidate. "
+ CriteriaDescriptions["ProblemSolving"][profile.coffeeChatStats.problemSolving - 1] + " "
+ CriteriaDescriptions["RizzAndFluency"][profile.coffeeChatStats.rizzAndFluency - 1] + " "
+ CriteriaDescriptions["Interviewability"][profile.coffeeChatStats.interviewability - 1] + " "
+ profile.linkedInProfile.ToNarrativeString()
+ " I will give you interview questions, and you must answer in this persona. Be brief in your responses, since this is a speed interview.",
                voice = VoiceNameExtensions.ToVoiceString(profile.voiceName)
            }
        };
        Debug.Log(prompt.session.instructions);
        dataChannel.Send(Encoding.UTF8.GetBytes(JsonUtility.ToJson(prompt)));
    }

    private void HandleServerMessage(string message)
    {
        try
        {
            ServerEvent baseEvent = JsonUtility.FromJson<ServerEvent>(message);
            Debug.Log($"Received event: {baseEvent.type}");

            switch (baseEvent.type)
            {
                case "response.audio_transcript.delta":
                    var delta = JsonUtility.FromJson<TranscriptDelta>(message);
                    Debug.Log($"Transcript delta: {delta.delta}");
                    break;

                case "response.audio_transcript.done":
                    var done = JsonUtility.FromJson<TranscriptDone>(message);
                    Debug.Log($"Final transcript: {done.transcript}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing message: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(null)) {
            Microphone.End(Microphone.devices[0]);
        }

        if (dataChannel != null)
        {
            dataChannel.Close();
            dataChannel = null;
        }

        if (peerConnection != null)
        {
            if (audioSender != null)
            {
                peerConnection.RemoveTrack(audioSender);
                audioSender = null;
            }
            peerConnection.Close();
            peerConnection = null;
        }

        if (microphoneTrack != null)
        {
            microphoneTrack.Stop();
            microphoneTrack = null;
        }

        if (sendStream != null)
        {
            sendStream.Dispose();
            sendStream = null;
        }
    }

    // Helper classes for JSON serialization
    [Serializable]
    private class SessionRequest
    {
        public string model;
        public string voice;
    }

    [Serializable]
    private class AudioBufferAppend
    {
        public string type;
        public string audio;
    }

    [Serializable]
    private class SessionResponse
    {
        public ClientSecret client_secret;
    }

    [Serializable]
    private class ClientSecret
    {
        public string value;
    }

    [Serializable]
    private class ServerEvent
    {
        public string type;
    }

    [Serializable]
    private class ResponseCreate
    {
        public string type;
        public ResponseData response;
    }

    [Serializable]
    private class SessionCreate
    {
        public string type;
        public SessionData session;
    }

    [Serializable]
    private class SessionData
    {
        public string instructions;
        public string voice;
    }

    [Serializable]
    private class ResponseData
    {
        public string instructions;
        public string voice;
    }

    [Serializable]
    private class TranscriptDelta
    {
        public string delta;
    }

    [Serializable]
    private class TranscriptDone
    {
        public string transcript;
    }
}
