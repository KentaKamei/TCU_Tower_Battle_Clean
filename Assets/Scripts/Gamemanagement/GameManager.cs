using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.MLAgents; 

public enum PieceType { Tcu1, Tcu2, Tcu3, Tcu4, Tcu5 }

public class GameManager : MonoBehaviour
{
    // ===============================
    // ピース関連
    // ===============================
    public List<GameObject> TCUPrefabs;
    public PieceController currentPiece;
    public List<PieceController> allPieces;

    // ===============================
    // ステージ・カメラ
    // ===============================
    public StageGenerator stageGenerator;
    public float yOffset = 3.5f;
    public Camera mainCamera;
    private float cameraScrollSpeed = 5.0f;
    private float minCameraY = 0.0f;
    private float maxCameraY = 20.0f;

    // ===============================
    // 入力・UI
    // ===============================
    public Button retry;
    public Button title;
    public Button rotateButton;
    public Button rotateButton2;
    public float rotationAngle = 10f;
    public TextMeshProUGUI win;
    public TextMeshProUGUI lose;
    public TextMeshProUGUI MyTurn;
    public TextMeshProUGUI AITurn;
    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    // ===============================
    // サウンド
    // ===============================
    public AudioSource audioSource;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip retryClip;

    // ===============================
    // AI関連
    // ===============================
    public TowerAgent towerAgent;
    public bool isTrainingMode = false;
    public bool hasRequestedAction = false;
    private float previousHighestPoint;
    private float currentHighestPoint;
    public static string selectedDifficulty = "training";

    // ===============================
    // プレイヤー操作関連
    // ===============================
    public bool isPlayerTurn = true;
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private float pressDuration = 0f;
    private float dragThresholdTime = 0.3f;
    private float clickedTimeThreshold = 15.0f;
    private float clickedTimeCounter = 0.0f;



    void Start()
    {
        if (allPieces == null)
        {
            allPieces = new List<PieceController>(); // リストがnullのときのみ初期化
        }
        stageGenerator = FindObjectOfType<StageGenerator>();
        towerAgent = FindObjectOfType<TowerAgent>();
        isPlayerTurn = true;

        // ML-Agentsがトレーニング中かどうかを確認
        isTrainingMode = Academy.Instance.IsCommunicatorOn;

        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("Piece prefabs are not set.");
        }

        Debug.Log("selectedDifficulty:" + selectedDifficulty);

        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        lose.gameObject.SetActive(false);
        MyTurn.gameObject.SetActive(false);
        AITurn.gameObject.SetActive(false);
        
        // 回転ボタンのクリックイベントを設定
        rotateButton.onClick.AddListener(RotatePiece);
        rotateButton2.onClick.AddListener(RotatePiece2);

        // 必要なコンポーネントを取得
        raycaster = FindObjectOfType<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();
    }

    void Update()
    {
        // トレーニング時は両方AIが行動
        if (isTrainingMode)
        {
            HandleAITurn();  // 両方のターンでAIが行動する
        }
        else
        {
            if (isPlayerTurn)
            {
                HandlePlayerTurn();  // プレイヤーのターン処理
            }
            else
            {
                HandleAITurn();  // AIのターン処理
            }
        }
        
        if (allPieces.Count > 0 && AreAllPiecesStationary())
        {
            SpawnPiece();
        }


    }
    // ===============================
    // ゲームの基本制御
    // ===============================
    public void Retry()
    {
        PlaySound(retryClip);
        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        lose.gameObject.SetActive(false);

        ClearAllPieces();

        // yOffsetを元の値に戻す
        yOffset = 3.5f;

        // カメラの位置を初期位置に戻す
        mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, yOffset - 3.5f, mainCamera.transform.position.z);

        // ステージを再生成
        if (stageGenerator != null)
        {
           stageGenerator.GenerateStage();
        }
        else
        {
            Debug.LogError("StageGenerator is null!");
        }

        // 新しいピースを生成する
        SpawnPiece();
        isPlayerTurn = true;
        hasRequestedAction = false;

    }
    public void GameOver()
    {
        // ゲームオーバーUIを表示
        retry.gameObject.SetActive(true);
        title.gameObject.SetActive(true);

        if(isPlayerTurn)
        {
            lose.gameObject.SetActive(true);
            PlaySound(loseClip); // 敗北音声を再生
        }
        else
        {
            win.gameObject.SetActive(true);
            PlaySound(winClip); // 勝利音声を再生
        }

        if (isTrainingMode && towerAgent != null)
        {
            
            //easy
            if(allPieces.Count >= 4 && allPieces.Count < 7)
            {
                towerAgent.AddReward(30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(-30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            //easy

            /*//nomal
            if(allPieces.Count >= 7 && allPieces.Count < 10)
            {
                towerAgent.AddReward(30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(-20.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            *///nomal

            /*
            //hard
            float rewardForStackedPieces = allPieces.Count * 2.0f;
            towerAgent.AddReward(rewardForStackedPieces);
            Debug.Log("ピース数報酬: " + rewardForStackedPieces);
            towerAgent.AddReward(-30.0f); // ペナルティ
            Debug.Log("ペナルティ報酬");
            
            if(allPieces.Count <= 6)//hard
            {
                towerAgent.AddReward(-30.0f);//easy
                towerAgent.EndEpisode(); // エピソード終了
            }
            else if(allPieces.Count <= 10)
            {
                towerAgent.AddReward(-15.0f);//hard
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(20.0f);//hard
                towerAgent.EndEpisode(); // エピソード終了
            }
            */
            //hard

        }
        
        // ゲームの状態を停止またはリセットする処理を追加
        foreach (var piece in allPieces)
        {
            piece.enabled = false;
        }

        // 回転ボタンを無効化
        rotateButton.interactable = false;
        rotateButton2.interactable = false;

        // トレーニング中なら自動でリトライ
        if (isTrainingMode)
        {
            Invoke("Retry", 1.0f); // 1秒後にリトライ（少し待つことで安定）
        }
    }
    public void BackToTitle()
    {        
        ClearAllPieces();

        // ゲームの状態をリセット
        towerAgent.currentPiece = null;
        towerAgent.currentPieceRigidbody = null;
        towerAgent.currentPieceTransform = null;

        // 初期位置やタイマーのリセット
        currentPiece = null;
        yOffset = 3.5f;
        isPlayerTurn = true;
        hasRequestedAction = false;

        // カメラ位置を初期位置に戻す
        mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, yOffset - 3.5f, mainCamera.transform.position.z);


        // タイトル画面へ遷移
        SceneManager.LoadScene("TitleScene");
    }
    private void ClearAllPieces()
    {
        foreach (PieceController piece in allPieces)
        {
            Destroy(piece.gameObject);
        }
        allPieces.Clear();
    }

    // ===============================
    // AIターン処理
    // ===============================
    private void HandleAITurn()
    {
        MyTurn.gameObject.SetActive(false);
        AITurn.gameObject.SetActive(true);


        // 現在のピースが存在し、まだ落下していない場合のみAIに行動をリクエスト
        if (towerAgent != null && currentPiece != null && !currentPiece.IsClicked)
        {
            if (!hasRequestedAction)
            {
                previousHighestPoint = CalculateTowerHeight();//ピースドロップ前の高さ
                towerAgent.RequestDecision();
                hasRequestedAction = true;
            }
        }

        // トレーニング中でピースがクリックされた状態をチェック
        if (isTrainingMode && currentPiece != null && currentPiece.isClicked)
        {
            // ピースが固定されている場合（通常のトレーニング処理）
            if (currentPiece.IsStationary())
            {
                
                //easy
                
                clickedTimeCounter = 0.0f;
                towerAgent.AddReward(3.0f);
                hasRequestedAction = false;
                Debug.Log("積み上げ成功");
                currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

                if(allPieces.Count >= 7)
                {
                    towerAgent.AddReward(-10.0f);
                    Debug.Log("ピース数10");
                }

                
                //easy

                /*//nomal
                clickedTimeCounter = 0.0f;
                towerAgent.AddReward(5.0f);
                hasRequestedAction = false;
                Debug.Log("積み上げ成功");
                currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

                if(allPieces.Count >= 10)
                {
                    towerAgent.AddReward(-6.0f);
                    Debug.Log("ピース数10");
                }
                //nomal
                */

                //hard
                /*
                clickedTimeCounter = 0.0f;
                towerAgent.AddReward(5.0f);//hard
                hasRequestedAction = false;
                Debug.Log("積み上げ成功");
                currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

                if(allPieces.Count == 7)
                {
                    towerAgent.AddReward(5.0f);
                    Debug.Log("ピース数7");
                }
                if(allPieces.Count == 10)
                {
                    towerAgent.AddReward(7.0f);
                    Debug.Log("ピース数10");
                }
                if(allPieces.Count == 14)
                {
                    towerAgent.AddReward(10.0f);
                    Debug.Log("ピース数14");
                }

                
                if (previousHighestPoint - currentPiece.transform.position.y >= 0)
                {
                    towerAgent.AddReward(2.0f); // 安定性の報酬
                    Debug.Log("安定性の報酬を追加: 2.0f");
                }
                else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold)
                {
                    towerAgent.AddReward(3.0f); // 高さ更新の報酬
                    Debug.Log("高さ更新の報酬を追加: 5.0f");
                }
                else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 2)
                {
                    towerAgent.AddReward(5.0f); // 高さ更新の報酬(2)
                    Debug.Log("高さ更新の報酬を追加(2): 10.0f");
                }
                else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 3)
                {
                    towerAgent.AddReward(10.0f); // 高さ更新の報酬(3)
                    Debug.Log("高さ更新の報酬を追加(3): 15.0f");
                }
                */
                //hard
            }
            else
            {
                // クリックされ続けている時間をカウント
                clickedTimeCounter += Time.deltaTime;

                // 許容時間を超えた場合にペナルティを与え、次のステップに進む
                if (clickedTimeCounter > clickedTimeThreshold)
                {
                    Debug.LogWarning("ピース停止");
                    clickedTimeCounter = 0.0f; // タイマーをリセット

                    if (currentPiece.rb != null)
                    {
                        // 動きを完全に停止
                        currentPiece.rb.velocity = Vector2.zero; // 速度をリセット
                        currentPiece.rb.angularVelocity = 0.0f;  // 回転速度をリセット

                        // 必要に応じて位置固定を有効化
                        currentPiece.rb.constraints = RigidbodyConstraints2D.FreezeAll; // 動きと回転を固定
                        clickedTimeCounter = 0.0f; // タイマーをリセット
                    }
                    
                }
            }
        }
    }

    // ===============================
    // プレイヤーターン処理
    // ===============================
    private void HandlePlayerTurn()
    {
        MyTurn.gameObject.SetActive(true);
        AITurn.gameObject.SetActive(false);

        // ----------------------------------------
        // 回転ボタンの有効化
        // ----------------------------------------
        if (currentPiece != null && !currentPiece.IsClicked)
        {
            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }
            if (rotateButton2 != null)
            {
                rotateButton2.interactable = true;
            }
        }

        // ----------------------------------------
        // マウスホイールでカメラスクロール
        // ----------------------------------------
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.y += scroll * cameraScrollSpeed;
            cameraPosition.y = Mathf.Clamp(cameraPosition.y, minCameraY, maxCameraY);
            mainCamera.transform.position = cameraPosition;
        }

        // ----------------------------------------
        // マウスクリック開始時の処理
        // ----------------------------------------
        if (Input.GetMouseButtonDown(0) && currentPiece != null)
        {
            if (!IsPointerOverUIElement(rotateButton.gameObject) && !IsPointerOverUIElement(rotateButton2.gameObject))
            {
                if (IsPointerOverPiece(currentPiece))
                {
                    isDragging = false;
                    pressDuration = 0f;
                    dragStartPosition = Input.mousePosition;
                }
                else
                {
                    currentPiece.DropPiece();
                    rotateButton.interactable = false;
                    rotateButton2.interactable = false;
                }
            }
        }

        // ----------------------------------------
        // マウスドラッグによるピース移動
        // ----------------------------------------
        if (Input.GetMouseButton(0))
        {
            HandleMouseDrag(); 
        }

        // ----------------------------------------
        // マウスクリック終了時の処理
        // ----------------------------------------
        if (Input.GetMouseButtonUp(0))
        {
            if (IsPointerOverUIElement(rotateButton.gameObject) || IsPointerOverUIElement(rotateButton2.gameObject))
            {
                isDragging = false;
                pressDuration = 0f;
                return;
            }

            if (currentPiece != null && !isDragging && pressDuration <= dragThresholdTime)
            {
                currentPiece.DropPiece();
                rotateButton.interactable = false;
                rotateButton2.interactable = false;
            }

            isDragging = false;
            pressDuration = 0f;
        }

        // ----------------------------------------
        // キーボードでの左右移動
        // ----------------------------------------
        if (currentPiece != null && !currentPiece.IsClicked && !isDragging)
        {
            float move = Input.GetAxis("Horizontal") * Time.deltaTime * 7.5f;
            Vector3 newPosition = currentPiece.transform.position + new Vector3(move, 0, 0);
            float leftBoundary = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).x;
            float rightBoundary = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane)).x;
            newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
            currentPiece.transform.position = newPosition;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                currentPiece.DropPiece();
                rotateButton.interactable = false;
                rotateButton2.interactable = false;
            }
        }
    }
    private void HandleMouseDrag()
    {
        if (currentPiece == null) return;

        // 回転ボタン上ならドラッグ処理を行わない
        if (IsPointerOverUIElement(rotateButton.gameObject) || IsPointerOverUIElement(rotateButton2.gameObject))
        {
            isDragging = false;
            return;
        }

        pressDuration += Time.deltaTime; // 押下時間をカウント

        // ドラッグかどうかの判断
        if (!isDragging && pressDuration > dragThresholdTime)
        {
            isDragging = true; // ドラッグ開始
        }

        if (isDragging && !currentPiece.IsClicked)
        {
            Vector3 dragCurrentPosition = Input.mousePosition;
            float dragDeltaX = dragCurrentPosition.x - dragStartPosition.x;
            float move = dragDeltaX * 0.0225f;

            Vector3 newPosition = currentPiece.transform.position + new Vector3(move, 0, 0);

            float leftBoundary = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).x;
            float rightBoundary = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane)).x;

            newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
            currentPiece.transform.position = newPosition;

            dragStartPosition = dragCurrentPosition;
        }
    }

    // ===============================
    // ピース生成・状態確認
    // ===============================
    public void SpawnPiece()
    {
        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("No piece prefabs available to spawn.");
            return;
        }

        // ランダムにプレハブを選択
        int randomIndex = Random.Range(0, TCUPrefabs.Count);
        GameObject selectedPrefab = TCUPrefabs[randomIndex];

        // タワーの高さを取得し、オフセットを追加
        float towerHeight = CalculateTowerHeight();
        if (yOffset - towerHeight < 4.0f)
        {
            yOffset += 2.0f;
            // カメラの位置も更新
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y + 2.0f, mainCamera.transform.position.z);
        }

        Vector3 spawnPosition = new Vector3(0, yOffset, 0);

        Debug.Log("spawn");

        // ピースを生成
        GameObject piece = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        currentPiece = piece.GetComponent<PieceController>();

        // プレハブのインデックスからピースの種類を設定
        if (randomIndex == 0)
        {
            currentPiece.pieceType = PieceType.Tcu1;
        }
        else if (randomIndex == 1)
        {
            currentPiece.pieceType = PieceType.Tcu2;
        }
        else if (randomIndex == 2)
        {
            currentPiece.pieceType = PieceType.Tcu3;
        }
        else if (randomIndex == 3)
        {
            currentPiece.pieceType = PieceType.Tcu4;
        }
        else if (randomIndex == 4)
        {
            currentPiece.pieceType = PieceType.Tcu5;
        }

        // 新しいピースをリストに追加
        allPieces.Add(currentPiece);

         if (towerAgent != null)
        {
            towerAgent.currentPiece = currentPiece;
            towerAgent.currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>();
            towerAgent.currentPieceTransform = currentPiece.transform;
            //Debug.Log("TowerAgent's currentPiece updated to: " + currentPiece.name);
        }


        isPlayerTurn = !isPlayerTurn;
        if(isPlayerTurn)
        {
            // 回転ボタンを有効化
            rotateButton.interactable = true;
            rotateButton2.interactable = true;
        }
        else
        {
            hasRequestedAction = false;
        }
        
    }
    public bool AreAllPiecesStationary()
    {
        foreach (var piece in allPieces)
        {
            if (!piece.IsStationary())
            {
                return false;
            }
        }
        return true;
    }
    public float CalculateTowerHeight()
    {
        float maxHeight = stageGenerator.baseY;
        foreach (var piece in allPieces)
        {
            if (piece.transform.position.y > maxHeight)
            {
                maxHeight = piece.transform.position.y;
            }
        }
        return maxHeight;
    }

    // ===============================
    // ピース回転・入力
    // ===============================
    public void RotatePiece()
    {
        if (currentPiece != null && currentPiece.enabled)
        {
            currentPiece.transform.Rotate(0, 0, rotationAngle);
        }
    }
    public void RotatePiece2()
    {
        if (currentPiece != null && currentPiece.enabled)
        {
            currentPiece.transform.Rotate(0, 0, -rotationAngle);
        }
    }
    private bool IsPointerOverUIElement(GameObject uiElement)
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == uiElement)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsPointerOverPiece(PieceController piece)
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D pieceCollider = piece.GetComponent<Collider2D>();
        return pieceCollider != null && pieceCollider.OverlapPoint(mousePosition);
    }
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip); // 指定された音声を再生
            Debug.Log($"再生中の音声: {clip.name}");
        }
        else
        {
            Debug.LogWarning("AudioSourceまたはAudioClipが設定されていません");
        }
    }

}
