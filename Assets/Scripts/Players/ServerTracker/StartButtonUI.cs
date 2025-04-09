using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Main
{
    class StartButtonUI : MonoBehaviour
    {
        [SerializeField]
        Button button;
        [SerializeField]
        TMP_Text text;
        [SerializeField]
        Image image;
        [SerializeField]
        Sprite startSprite;
        [SerializeField]
        Sprite stopSprite;

        void Start()
        {
            button.onClick.AddListener(() =>
            {
                if (BoardGameManager.Instance.IsPlaying)
                {
                    BoardGameManager.Instance.StopGame();
                    text.text = "\n\nStart";
                    image.sprite = startSprite;
                }
                else
                {
                    BoardGameManager.Instance.StartGame();
                    text.text = "\n\nStop";
                    image.sprite = stopSprite;
                }
            });
        }
    }
}