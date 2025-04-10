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

        bool isPlaying = false;

        void Start()
        {
            BoardGameManager.Instance.OnGameStatusChanged += UpdateIsPlaying;

            button.onClick.AddListener(() =>
            {
                if (isPlaying)
                {
                    BoardGameManager.Instance.StopGameRpc();
                }
                else
                {
                    BoardGameManager.Instance.StartGameRpc();
                }
            });
        }

        void OnDestroy()
        {
            if (BoardGameManager.Instance != null)
            {
                BoardGameManager.Instance.OnGameStatusChanged -= UpdateIsPlaying;
            }
        }

        void UpdateIsPlaying(BoardGameManager.GameStatus status)
        {
            isPlaying = status == BoardGameManager.GameStatus.Started;
            if (isPlaying)
            {
                text.text = "\n\nStop";
                image.sprite = stopSprite;
            }
            else
            {
                text.text = "\n\nStart";
                image.sprite = startSprite;
            }
        }
    }
}