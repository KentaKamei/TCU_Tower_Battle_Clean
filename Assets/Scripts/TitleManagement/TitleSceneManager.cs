using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreenManager : MonoBehaviour
{
    public float delayBeforeSceneLoad = 3.0f; // シーン切り替えの遅延時間

    public void StartGame()
    {
        StartCoroutine(LoadSceneWithDelay());
    }

    private IEnumerator LoadSceneWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneLoad); // 指定した秒数待機
        SceneManager.LoadScene("GameScene");
    }
}
