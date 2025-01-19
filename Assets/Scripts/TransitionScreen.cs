using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class TransitionScreen : MonoBehaviour
{
    [SerializeField] private RectTransform screen;
    [SerializeField] private int size = 450;
    public void ScreenDown()
    {
        screen.DOLocalMoveY(0, 1).SetEase(Ease.OutBounce);
    }
    public void ChangeText(string text)
    {
        screen.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = text;
    }
    public void ScreenUp()
    {
        screen.DOLocalMoveY(size, 1).SetEase(Ease.InExpo);
    }
}
