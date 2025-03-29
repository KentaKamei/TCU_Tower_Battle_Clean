using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class TowerAgent : Agent
{
    public PieceController currentPiece;
    public StageGenerator stageGenerator;
    public GameManager gameManager;
    public Rigidbody2D currentPieceRigidbody;
    public Transform currentPieceTransform;

    public override void OnEpisodeBegin()
    {
        gameManager.ResetGame();
        currentPiece = gameManager.currentPiece;
        currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>();
        currentPieceTransform = currentPiece.transform;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (currentPiece == null) return;
        sensor.AddObservation(currentPiece.transform.position);
        sensor.AddObservation(currentPiece.transform.rotation);
        sensor.AddObservation((int)currentPiece.pieceType);
        float[] surfaceShape = CalculateSurfaceShape();
        foreach (float height in surfaceShape)
            sensor.AddObservation(height);

        sensor.AddObservation(stageGenerator.minX);
        sensor.AddObservation(stageGenerator.maxX);
        sensor.AddObservation(stageGenerator.baseY);
        sensor.AddObservation(gameManager.allPieces.Count);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0] * 5f;
        float rotationZ = actions.ContinuousActions[1] * 180f;

        // 難易度に応じてノイズを加える
        string difficulty = gameManager.trainingDifficulty;
        float noiseFactor = 0f;

        if (difficulty == "easy")
            noiseFactor = 1.0f;
        else if (difficulty == "normal")
            noiseFactor = 0.3f;
        else if (difficulty == "hard")
            noiseFactor = 0f;

        moveX += Random.Range(-noiseFactor, noiseFactor);
        rotationZ += Random.Range(-noiseFactor * 20f, noiseFactor * 20f); // 回転は少し荒く

        MovePiece(moveX);
        RotatePiece(rotationZ);
        currentPiece.DropPiece();
    }

    protected void MovePiece(float moveX)
    {
        Vector3 newPosition = currentPiece.transform.position + new Vector3(moveX, 0, 0);
        float leftBoundary = stageGenerator.minX + 0.2f;
        float rightBoundary = stageGenerator.maxX - 0.2f;
        newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
        currentPiece.transform.position = newPosition;
    }

    protected void RotatePiece(float rotationZ)
    {
        currentPieceTransform.Rotate(new Vector3(0, 0, rotationZ));
    }

    protected float[] CalculateSurfaceShape()
    {
        int segments = 15;
        float[] shape = new float[segments];
        float totalWidth = stageGenerator.totalWidth;

        foreach (var piece in gameManager.allPieces)
        {
            int index = Mathf.FloorToInt((piece.transform.position.x + totalWidth / 2) / totalWidth * segments);
            if (index >= 0 && index < segments)
                shape[index] = Mathf.Max(shape[index], piece.transform.position.y);
        }
        return shape;
    }

    public void HandlePieceStable(float prevHeight, float newHeight, int pieceCount)
    {
        // 基本報酬：ピースを安定して積めた
        AddReward(2.0f);

        // 高さが前より伸びたらご褒美（高く積む誘導）
        if (newHeight > prevHeight + 0.5f)
            AddReward(2.0f);
    }
    

    public void HandleGameOver(int pieceCount)
    {
        // 単純に「どれだけ積めたか」に応じた報酬（積んだピース数）
        float reward = pieceCount * 1.5f;

        AddReward(reward);

        // 例：めっちゃ少ないと軽くマイナス（ミスっぽさ）
        if (pieceCount < 3)
            AddReward(-10.0f);

        EndEpisode();
    }
}
