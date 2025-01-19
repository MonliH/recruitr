using DG.Tweening;
using TMPro;
using UnityEngine;

public class LoadingTextAnim : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingText;

    public float updateInterval = 1f; // Time between updates
    public float pauseDuration = 1.5f; // Pause duration at max dots
    public int maxDots = 3; // Maximum number of dots

    private string baseText = "Loading";
    private int currentDotCount = 0;

    void Start()
    {
        if (loadingText == null)
        {
            Debug.LogError("LoadingTextDOTween: No TMP_Text component assigned!");
            return;
        }

        // Start the DOTween animation loop
        AnimateDots();
    }

    private void AnimateDots()
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 1; i <= maxDots; i++)
        {
            int dots = i; // Capture the current value for the closure
            sequence.AppendCallback(() =>
            {
                loadingText.text = baseText + new string('.', dots);
            });
            sequence.AppendInterval(updateInterval);
        }

        // Add a pause when the max number of dots is reached
        sequence.AppendInterval(pauseDuration);

        // Loop the sequence infinitely
        sequence.SetLoops(-1, LoopType.Restart);
    }
}
