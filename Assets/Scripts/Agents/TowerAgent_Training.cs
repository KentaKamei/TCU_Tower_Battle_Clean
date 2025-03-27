public class TowerAgent_Training : TowerAgent
{
    public override void HandlePieceStable(float prevHeight, float newHeight, int pieceCount)
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

    public override void HandleGameOver(int pieceCount)
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
