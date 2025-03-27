using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public abstract class TowerAgent : Agent
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
    public abstract void HandlePieceStable(float prevHeight, float newHeight, int pieceCount);
    public abstract void HandleGameOver(int pieceCount);
}
