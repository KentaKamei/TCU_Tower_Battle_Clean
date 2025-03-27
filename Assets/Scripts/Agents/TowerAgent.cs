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
        float moveX = actions.ContinuousActions[0] * 5;
        float rotationZ = actions.ContinuousActions[1] * 180;

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

    // 難易度ごとにオーバーライド
    public void HandlePieceStable(float prevHeight, float newHeight, int pieceCount)
    {
        string difficulty = gameManager.trainingDifficulty;

        if (difficulty == "easy")
        {
            AddReward(3.0f);
            if (pieceCount >= 7)
            {
                AddReward(-10.0f);
            }
        }
        else if (difficulty == "normal")
        {
            AddReward(5.0f);
            if (pieceCount >= 10)
            {
                AddReward(-6.0f);
            }
        }
        else if (difficulty == "hard")
        {
            AddReward(5.0f);

            if (pieceCount == 7)
                AddReward(5.0f);
            else if (pieceCount == 10)
                AddReward(7.0f);
            else if (pieceCount == 14)
                AddReward(10.0f);

            if (prevHeight - currentPieceTransform.position.y >= 0)
            {
                AddReward(2.0f); // 安定性の報酬
            }
            else if (newHeight >= 6.0f)
            {
                AddReward(3.0f);
            }
            else if (newHeight >= 12.0f)
            {
                AddReward(5.0f);
            }
            else if (newHeight >= 18.0f)
            {
                AddReward(10.0f);
            }
        }
    }

    public void HandleGameOver(int pieceCount)
    {
        string difficulty = gameManager.trainingDifficulty;

        if (difficulty == "easy")
        {
            if (pieceCount >= 4 && pieceCount < 7)
            {
                AddReward(30.0f);
            }
            else
            {
                AddReward(-30.0f);
            }
        }
        else if (difficulty == "normal")
        {
            if (pieceCount >= 7 && pieceCount < 10)
            {
                AddReward(30.0f);
            }
            else
            {
                AddReward(-20.0f);
            }
        }
        else if (difficulty == "hard")
        {
            float rewardForStackedPieces = pieceCount * 2.0f;
            AddReward(rewardForStackedPieces);
            AddReward(-30.0f); // ベースペナルティ

            if (pieceCount <= 6)
            {
                AddReward(-30.0f);
            }
            else if (pieceCount <= 10)
            {
                AddReward(-15.0f);
            }
            else
            {
                AddReward(20.0f);
            }
        }

        EndEpisode();
    }
}
