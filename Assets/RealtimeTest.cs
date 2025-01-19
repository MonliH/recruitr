using UnityEngine;
using Unity.WebRTC;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class OpenAIAudioDebug : MonoBehaviour
{
    [SerializeField] private string apiKey = "your-api-key";
    [SerializeField] private AudioSource outputSource;
    
    private RTCPeerConnection peerConnection;
    private string ephemeralKey;
    private const string OPENAI_SESSION_URL = "https://api.openai.com/v1/realtime/sessions";
    private const string OPENAI_BASE_URL = "https://api.openai.com/v1/realtime";
    private const string MODEL_ID = "gpt-4o-mini-realtime-preview-2024-12-17";

    void Start()
    {
        Debug.Log("Starting OpenAI Audio Debug...");
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // Get session token
        var request = new UnityWebRequest(OPENAI_SESSION_URL, "POST");
        var sessionRequest = new { model = MODEL_ID, voice = "shimmer" };
        string jsonBody = JsonUtility.ToJson(sessionRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log("Requesting session token...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Session token error: {request.error}\nResponse: {request.downloadHandler.text}");
            yield break;
        }

        Debug.Log($"Session response: {request.downloadHandler.text}");
        var response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
        ephemeralKey = response.client_secret.value;
        
        yield return SetupWebRTC();
    }

    private IEnumerator SetupWebRTC()
    {
        // Create peer connection
        var config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };
        peerConnection = new RTCPeerConnection(ref config);

        // Add debug logs for connection state
        peerConnection.OnConnectionStateChange = state => 
            Debug.Log($"Connection state changed to: {state}");
        
        // peerConnection.OnIceConnectionStateChange = state => 
        //     Debug.Log($"ICE state changed to: {state}");
        
        peerConnection.OnIceCandidate = candidate => 
            Debug.Log($"New ICE candidate: {candidate}");

        // Set up audio output handling
        peerConnection.OnTrack = e =>
        {
            Debug.Log($"Got track! Type: {e.Track.GetType()}, Kind: {e.Track.Kind}");
            
            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log("Got audio track! Setting up output...");
                outputSource.SetTrack(audioTrack);
                outputSource.loop = true;
                outputSource.Play();
                Debug.Log($"Audio setup complete. Playing: {outputSource.isPlaying}, Volume: {outputSource.volume}");
            }
        };

        yield return CreateAndSendOffer();
    }

    private IEnumerator CreateAndSendOffer()
    {
        Debug.Log("Creating offer...");
        var op = peerConnection.CreateOffer();
        yield return op;

        if (op.IsError)
        {
            Debug.LogError($"Offer creation failed: {op.Error.message}");
            yield break;
        }

        Debug.Log("Setting local description...");
        var offerDesc = op.Desc;
        var setLocalOp = peerConnection.SetLocalDescription(ref offerDesc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            Debug.LogError($"Set local description failed: {setLocalOp.Error.message}");
            yield break;
        }

        // Send offer to OpenAI
        Debug.Log("Sending offer to OpenAI...");
        var request = UnityWebRequest.PostWwwForm($"{OPENAI_BASE_URL}?model={MODEL_ID}", "");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(offerDesc.sdp));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/sdp");
        request.SetRequestHeader("Authorization", $"Bearer {ephemeralKey}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to get answer: {request.error}\nResponse: {request.downloadHandler.text}");
            yield break;
        }

        Debug.Log($"Got answer! Setting remote description...");
        var answer = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = request.downloadHandler.text };
        var setRemoteOp = peerConnection.SetRemoteDescription(ref answer);
        yield return setRemoteOp;

        if (setRemoteOp.IsError)
        {
            Debug.LogError($"Set remote description failed: {setRemoteOp.Error.message}");
            yield break;
        }

        Debug.Log("WebRTC setup complete!");
    }

    void Update()
    {
        // Monitor audio output state
        if (outputSource != null)
        {
            if (outputSource.isPlaying)
            {
                Debug.Log($"Audio playing - Time: {outputSource.time}, Volume: {outputSource.volume}");
            }
        }
    }

    private void OnDestroy()
    {
        if (peerConnection != null)
        {
            peerConnection.Close();
            peerConnection.Dispose();
        }
    }
}

[System.Serializable]
public class SessionResponse
{
    public ClientSecret client_secret;
}

[System.Serializable]
public class ClientSecret
{
    public string value;
}