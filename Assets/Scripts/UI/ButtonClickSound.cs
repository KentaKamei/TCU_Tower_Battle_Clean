using UnityEngine;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    public AudioSource audioSource; // AudioSourceをアタッチ
    public AudioClip clickSound;    // 再生する音

    void Start()
    {
        // ボタンコンポーネントを取得
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // PlayClickSoundをOnClickイベントに登録
            button.onClick.AddListener(PlayClickSound);
        }
        else
        {
            //Debug.Log("Buttonコンポーネントが見つかりません");
        }
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        else
        {
            Debug.LogWarning("AudioSourceまたはAudioClipが設定されていません");
        }
    }
}
