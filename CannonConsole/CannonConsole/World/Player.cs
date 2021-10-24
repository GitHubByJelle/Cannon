using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using World;
using TT;
using System.Text.RegularExpressions;

/// Not used in main code, slowed down the code (still kept in)
public struct features
{
    public int MaterialPiece { get; set; }
    public int MaterialCannon { get; set; }
    public int MaterialSoldier { get; set; }
    public int MaterialTown { get; set; }
    public int InControl { get; set; }
    public int InDangerPiece { get; set; }
    public int InDangerCannon { get; set; }
    public int InDangerSoldier { get; set; }
    public int InDangerTown { get; set; }
    public int Mobility { get; set; }
    public int Random { get; set; }

    public int multiplyArr(int[] weights)
    {

        return this.MaterialPiece * weights[0] + this.MaterialSoldier * weights[1] + this.MaterialCannon * weights[2] + this.MaterialTown * weights[3] +
            this.InControl * weights[4] + this.InDangerPiece * weights[5] + this.InDangerSoldier * weights[6] + this.InDangerCannon * weights[7] + this.InDangerTown * weights[8] +
            this.Mobility * weights[9] + this.Random * weights[10];
    }
}

// Default Player options
public class Player
{
    public int playerId;
    bool LegalMoves = true;
    static Random rnd = new Random();
    List<Move> orderedMoves = new List<Move>();
    features fts = new features();
    public bool adjustTime = false;
    public int numberOfEstimations = 0;
    public float epsilon;

    public virtual void makeMove(Board B, bool print, Player playerOne, Player playerTwo) { }
    public virtual int makeMove(Board B, bool print, bool analyses) { return 0; }
    public virtual void placeTown(Board B, bool print, Player playerOne, Player playerTwo) { }

    public virtual void resetTT() { }

    public virtual void adjustMaxTime(int expectedPly) { }

    public virtual void setWeights(int[] wghts) { }
    public virtual int[] getWeights() { return new int[2]; }

    public int getPlayerId()
    {
        return this.playerId;
    }

    public bool getAdjustTime()
    {
        return this.adjustTime;
    }

    public bool LegalMoveLeft()
    {
        return this.LegalMoves;
    }

    public void NoLegalMoves()
    {
        this.LegalMoves = false;
    }

    // Evaluate
    public int Evaluate(Board B, int[] wght)
    {
        //return rnd.Next(-10, 10);

        return getFeatures(B).multiplyArr(wght);
        //return 2 * fts[0] + fts[1] + 3 * fts[2] + 100 * fts[3] + 2 * fts[4] + rnd.Next(-10, 10);
    }

    public int Evaluate2(Board B, int[] wght)
    {
        //return rnd.Next(-10, 10);

        return getFeatures2(B).multiplyArr(wght);
        //return 2 * fts[0] + fts[1] + 3 * fts[2] + 100 * fts[3] + 2 * fts[4] + rnd.Next(-10, 10);
    }

    public int Evaluate3(Board B, int[] wght)
    {
        //return rnd.Next(-10, 10);
        getFeatures3(B);

        return this.fts.multiplyArr(wght);
        //return 2 * fts[0] + fts[1] + 3 * fts[2] + 100 * fts[3] + 2 * fts[4] + rnd.Next(-10, 10);
    }

    public int getBoundsEval(int[] wght)
    {
        //return 10; 
        return Math.Abs(wght[0] * 15) + Math.Abs(wght[1] * 15) + Math.Abs(wght[2] * 15) + Math.Abs(wght[3] * 1) + Math.Abs(wght[4] * 100) +
            Math.Abs(wght[5] * 15) + Math.Abs(wght[6] * 15) + Math.Abs(wght[7] * 15) + Math.Abs(wght[8] * 1) + Math.Abs(wght[9] * 70);
        return 2 * 15 + 15 + 3 * 15 + 100 * 1 + 2 * 100 - 2 * 15 - 15 - 15 - 10 * 1 + 2 * 70;
    }

    public int getBoundsEval2(int[] wght)
    {
        return wght[0] * 15 + wght[1] * 15 + wght[2] * 15 + wght[3] * 15 + wght[4] * 15 + wght[5] * 15 + wght[6] * 1 + wght[7] * 100 + wght[8] * 15 + wght[9] * 15 + wght[10] * 15 + wght[11] * 1 + wght[12] * 80 + wght[13] * 5 + wght[14] * 70 + wght[15] * 9 + wght[16] * 10;
    }

    // Order moves
    public List<Move> orderMoves(List<Move> oldMoves, Move bestTTMove)
    {
        // If there is a TT move
        if (bestTTMove != default(Move))
        {
            int numMoves = oldMoves.Count();
            int[] orderArray = new int[numMoves];

            // Give all moves a value of importance
            for (int i = 0; i < numMoves; i++)
            {
                if (oldMoves[i] == bestTTMove)
                {
                    orderArray[i] += 10;
                }

                // Add values based on Knowledge
                orderArray[i] += (int)oldMoves[i].type;
            }

            // Order moves
            int[] indices = Enumerable.Range(0, numMoves).ToArray();
            Array.Sort(orderArray, indices);

            List<Move> newMoves = new List<Move>();
            for (int j = numMoves - 1; j >= 0; j--)
            {
                newMoves.Add(oldMoves[indices[j]]);
            }

            // Return orders list
            return newMoves;
        }
        else
            return oldMoves.orderByMoveType();
    }

    public List<Move> orderMoves(List<Move> oldMoves, Move bestTTMove, int[] scores)
    {
        // If there is a TT move
        if (bestTTMove != default(Move))
        {
            int numMoves = oldMoves.Count();
            int[] orderArray = new int[numMoves];

            // Give all moves a value of importance
            for (int i = 0; i < numMoves; i++)
            {
                // Add values based on Knowledge (multiply with 1 for decreasing)
                orderArray[i] += scores[i] * -1;

                // If it is the bestTT move, place it in front (by multiplying with 100)
                if (oldMoves[i] == bestTTMove)
                {
                    orderArray[i] *= 100;
                }
            }

            // Order moves (reverse for descending order)
            int[] indices = Enumerable.Range(0, numMoves).ToArray();
            Array.Sort(orderArray, indices);

            List<Move> newMoves = new List<Move>();
            for (int j = 0; j < numMoves; j++)
            {
                newMoves.Add(oldMoves[indices[j]]);
            }

            // Return orders list
            return newMoves;
        }
        else
        {
            int numMoves = oldMoves.Count();

            // Order moves based on scores only
            int[] indices = Enumerable.Range(0, numMoves).ToArray();
            scores = scores.Select(x => x * -1).ToArray(); // For decreasing order
            Array.Sort(scores, indices); // Array.Sort((int[])scores.Clone(), indices); If you dont want score to change

            // Sort the moves correctly
            List<Move> newMoves = new List<Move>();
            for (int j = 0; j < numMoves; j++)
            {
                newMoves.Add(oldMoves[indices[j]]);
            }

            // Return orders list
            return newMoves;
        }
    }

    public List<Coord> orderPlacements(List<Coord> placements, int[] scores)
    {
        int numMoves = placements.Count();

        // Order moves based on scores only
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        scores = scores.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(scores, indices); // Array.Sort((int[])scores.Clone(), indices); If you dont want score to change

        // Sort the moves correctly
        List<Coord> newMoves = new List<Coord>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(placements[indices[j]]);
        }

        // Return orders list
        return newMoves;
    }

    public List<Move> orderMoves(List<Move> oldMoves, Move bestTTMove, Move[] killerMoves, int depth)
    {
        int numMoves = oldMoves.Count();
        int[] orderArray = new int[numMoves];
        bool bestTT = bestTTMove != default(Move);
        bool km = killerMoves[depth - 1] != default(Move);

        // Give all moves a value of importance
        for (int i = 0; i < numMoves; i++)
        {
            if (bestTT && oldMoves[i] == bestTTMove)
            {
                orderArray[i] += 20;
            }

            else if (km && oldMoves[i] == killerMoves[depth - 1])
            {
                orderArray[i] += 10;
            }

            else
            {
                // Add values based on Knowledge
                orderArray[i] += (int)oldMoves[i].type;
            }
        }

        // Order moves
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        orderArray = orderArray.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(orderArray, indices);

        List<Move> newMoves = new List<Move>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(oldMoves[indices[j]]);
        }

        // Return orders list
        return newMoves;
    }

    public List<Move> orderMoves(List<Move> oldMoves, Move bestTTMove, Move[] killerMoves, int depth, int[,,,] historyHeuristic)
    {
        int numMoves = oldMoves.Count();
        int[] orderArray = new int[numMoves];
        bool bestTT = bestTTMove != default(Move);
        bool km = killerMoves[depth - 1] != default(Move);
        int maxHH = historyHeuristic.getMax(oldMoves);

        // Give all moves a value of importance
        for (int i = 0; i < numMoves; i++)
        {
            if (bestTT && oldMoves[i] == bestTTMove)
            {
                // Add value based on TT move
                orderArray[i] += (maxHH + 20);
            }

            else if (km && oldMoves[i] == killerMoves[depth - 1])
            {
                // Add value based on killermove
                orderArray[i] += (maxHH + 10);
            }

            else if (oldMoves[i].type == Move.moveType.shoot || oldMoves[i].type == Move.moveType.soldierCapture)
            {
                // Add values based on Knowledge
                orderArray[i] += (maxHH + (int)oldMoves[i].type);
            }

            else
            {
                // Add values based on HH
                orderArray[i] += historyHeuristic[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y];
            }
        }

        // Order moves
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        orderArray = orderArray.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(orderArray, indices);

        List<Move> newMoves = new List<Move>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(oldMoves[indices[j]]);
        }

        // Return orders list
        return newMoves;
    }

    public List<Move> orderMovesVD(List<Move> oldMoves, Move bestTTMove, Move[] killerMoves, float depth, int[,,,] historyHeuristic)
    {
        int numMoves = oldMoves.Count();
        int[] orderArray = new int[numMoves];
        bool bestTT = bestTTMove != default(Move);
        bool km = killerMoves[(int)(2*depth - 1)] != default(Move);
        int maxHH = historyHeuristic.getMax(oldMoves);

        // Give all moves a value of importance
        for (int i = 0; i < numMoves; i++)
        {
            if (bestTT && oldMoves[i] == bestTTMove)
            {
                // Add value based on TT move
                orderArray[i] += (maxHH + 20);
            }

            else if (km && oldMoves[i] == killerMoves[(int)(2 * depth - 1)])
            {
                // Add value based on killermove
                orderArray[i] += (maxHH + 10);
            }

            else if (oldMoves[i].type == Move.moveType.shoot || oldMoves[i].type == Move.moveType.soldierCapture)
            {
                // Add values based on Knowledge
                orderArray[i] += (maxHH + (int)oldMoves[i].type);
            }

            else
            {
                // Add values based on HH
                orderArray[i] += historyHeuristic[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y];
            }
        }

        // Order moves
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        orderArray = orderArray.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(orderArray, indices);

        List<Move> newMoves = new List<Move>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(oldMoves[indices[j]]);
        }

        // Return orders list
        return newMoves;


        //this.orderedMoves.Clear();
        //for (int j = 0; j < numMoves; j++)
        //{
        //    this.orderedMoves.Add(oldMoves[indices[j]]);
        //}

        //// Return orders list
        //return this.orderedMoves;
    }

    public List<Move> orderMovesVDPlus(List<Move> oldMoves, Move bestTTMove, Move[] killerMoves, float depth, int[,,,] historyHeuristic, int[,,,] butterflyTable)
    {
        int numMoves = oldMoves.Count();
        int[] orderArray = new int[numMoves];
        bool bestTT = bestTTMove != default(Move);
        bool km = killerMoves[(int)(2 * depth - 1)] != default(Move);
        int maxHH = historyHeuristic.getMax(oldMoves);

        // Give all moves a value of importance
        for (int i = 0; i < numMoves; i++)
        {
            if (bestTT && oldMoves[i] == bestTTMove)
            {
                // Add value based on TT move
                orderArray[i] += (maxHH + 20);
            }

            else if (km && oldMoves[i] == killerMoves[(int)(2 * depth - 1)])
            {
                // Add value based on killermove
                orderArray[i] += (maxHH + 10);
            }

            else if (oldMoves[i].type == Move.moveType.shoot || oldMoves[i].type == Move.moveType.soldierCapture)
            {
                // Add values based on Knowledge
                orderArray[i] += (maxHH + (int)oldMoves[i].type);
            }

            else
            {
                // Add values based on HH
                if (butterflyTable[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y] > 0)
                {
                    orderArray[i] += historyHeuristic[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y] /
                    butterflyTable[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y];
                }
                else
                {
                    orderArray[i] += historyHeuristic[oldMoves[i].From.x, oldMoves[i].From.y, oldMoves[i].To.x, oldMoves[i].To.y];
                }
                
            }
        }

        // Order moves
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        orderArray = orderArray.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(orderArray, indices);

        List<Move> newMoves = new List<Move>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(oldMoves[indices[j]]);
        }

        // Return orders list
        return newMoves;


        //this.orderedMoves.Clear();
        //for (int j = 0; j < numMoves; j++)
        //{
        //    this.orderedMoves.Add(oldMoves[indices[j]]);
        //}

        //// Return orders list
        //return this.orderedMoves;
    }

    public List<Move> orderMovesScore(List<Move> oldMoves, int[] scores)
    {
        int numMoves = oldMoves.Count();

        // Order moves based on scores only
        int[] indices = Enumerable.Range(0, numMoves).ToArray();
        scores = scores.Select(x => x * -1).ToArray(); // For decreasing order
        Array.Sort(scores, indices); // Array.Sort((int[])scores.Clone(), indices); If you dont want score to change

        List<Move> newMoves = new List<Move>();
        for (int j = 0; j < numMoves; j++)
        {
            newMoves.Add(oldMoves[indices[j]]);
        }

        // Return orders list
        return newMoves;
    }

    /// Features
    public int[] getFeatures(Board B)
    {
        int[] features = new int[11];

        // Get all measures
        int[] cntPieces = CountPieces(B);
        int[] cntTown = CountTown(B);
        int[] cntDangerControl = getNumberPiecesInControlAndDanger(B);
        int[] cntPossibleMoves = getNumberOfPossibleMoves(B);

        // Determine Features
        // Feature 1 - Material (Pieces) (Current - Enemy) (MAX)
        features[0] = cntPieces[0] - cntPieces[3];

        // Feature 2 - Material (Soldier) (Current - Enemy) (MAX)
        features[1] = cntPieces[1] - cntPieces[4];

        // Feature 3 - Material (Cannon) (Current - Enemy) (MAX)
        features[2] = cntPieces[2] - cntPieces[5];

        // Feature 4 - Material (Town) (Current - Enemy) (MAX) (IMPORTANT -> WIN / LOSE)
        features[3] = cntTown[0] - cntTown[1];

        //// Feature 5 - Control (Current - Enemy) (MAX)
        features[4] = cntDangerControl[0] - cntDangerControl[5];

        //// Feature 6 = Danger (Pieces) (Current - Enemy) (MIN)
        features[5] = cntDangerControl[1] - cntDangerControl[6];

        //// Feature 7 = Danger (Soldiers) (Current - Enemy) (MIN)
        features[6] = cntDangerControl[2] - cntDangerControl[7];

        //// Feature 8 = Danger (Cannons) (Current - Enemy) (MIN)
        features[7] = cntDangerControl[3] - cntDangerControl[8];

        //// Feature 9 = Danger (Towns) (Current - Enemy) (MIN) (IMPORTANT -> Potential WIN / LOSE)
        features[8] = cntDangerControl[4] - cntDangerControl[9];

        // Feature 10 = Mobility (Possible moves) (Current - Enemy) (MAX) -> Potential win
        features[9] = cntPossibleMoves[0] - cntPossibleMoves[1];

        // Feature 11 = random factor
        //features[10] = 0;
        features[10] = rnd.Next(-2, 2);

        return features;
    }

    public int[] getFeatures2(Board B)
    {
        int[] features = new int[17];
        // Get all measures
        int[] cntPieces = CountPieces2(B);
        int[] cntTown = CountTown(B);
        int[] cntDangerControl = getNumberPiecesInControlAndDanger2(B);
        int[] cntPossibleMoves = getNumberOfPossibleMoves(B);
        int[] minDistance = getMinimumDistanceToTown(B);

        // Determine Features
        // Feature 1 - Material (Pieces) (Current - Enemy) (MAX)
        features[0] = cntPieces[0] - cntPieces[6];
        
        // Feature 2 - Material (Soldier) (Current - Enemy) (MAX)
        features[1] = cntPieces[1] - cntPieces[7];

        // Feature 3 - Material (Cannon) (Current - Enemy) (MAX)
        features[2] = cntPieces[2] - cntPieces[8];

        // Feature 4 - Material Town Reachable (Pieces) (Current - Enemy) (MAX)
        features[3] = cntPieces[3] - cntPieces[9];

        // Feature 5 - Material Town Reachable (Soldier) (Current - Enemy) (MAX)
        features[4] = cntPieces[4] - cntPieces[10];

        // Feature 6 - Material Town Reachable (Cannon) (Current - Enemy) (MAX)
        features[5] = cntPieces[5] - cntPieces[11];
        
        // Feature 7 - Material (Town) (Current - Enemy) (MAX) (IMPORTANT -> WIN / LOSE)
        features[6] = cntTown[0] - cntTown[1];

        //// Feature 8 - Control (Current - Enemy) (MAX)
        features[7] = cntDangerControl[0] - cntDangerControl[7];
        
        //// Feature 9 - Danger (Pieces) (Current - Enemy) (MIN)
        features[8] = cntDangerControl[1] - cntDangerControl[8];

        //// Feature 10 - Danger (Soldiers) (Current - Enemy) (MIN)
        features[9] = cntDangerControl[2] - cntDangerControl[9];

        //// Feature 11 - Danger (Cannons) (Current - Enemy) (MIN)
        features[10] = cntDangerControl[3] - cntDangerControl[10];
        
        //// Feature 12 - Danger (Towns) (Current - Enemy) (MIN) (IMPORTANT -> Potential WIN / LOSE)
        features[11] = cntDangerControl[4] - cntDangerControl[11];

        //// Feature 13 - Control Town Reachable (Current - Enemy) (MAX)
        features[12] = cntDangerControl[5] - cntDangerControl[12];

        //// Feature 14 - Control Around Town (Current - Enemy) (MAX)
        features[13] = cntDangerControl[6] - cntDangerControl[13];

        //// Feature 15 - Mobility (Possible moves) (Current - Enemy) (MAX) -> Potential win
        features[14] = cntPossibleMoves[0] - cntPossibleMoves[1];

        //// Feature 16 - Minimum distance to Town (Current - Enemy) (MIN)
        features[15] = minDistance[0] - minDistance[1];

        //// Feature 17 - Random factor
        features[16] = rnd.Next(-10, 10);

        return features;
    }

    public void getFeatures3(Board B)
    {
        // Get all measures
        CountPieces3(B);
        CountTown3(B);
        getNumberPiecesInControlAndDanger3(B);
        getNumberOfPossibleMoves3(B);

        // Feature 11 = random factor
        this.fts.Random = rnd.Next(-10, 10);
    }

    public void printFeatures(Board B)
    {
        int[] fts = getFeatures(B);

        Console.WriteLine($"###########################################################\n" +
            $"Feature 1 - Material (Pieces) (Current - Enemy) (MAX) {fts[0]}\n" +
            $"Feature 2 - Material (Soldier) (Current - Enemy) (MAX) {fts[1]}\n" +
            $"Feature 3 - Material (Cannon) (Current - Enemy) (MAX) {fts[2]}\n" +
            $"Feature 4 - Material (Town) (Current - Enemy) (MAX) (IMPORTANT -> WIN / LOSE) {fts[3]}\n" +
            $"Feature 5 - Control (Current - Enemy) (MAX) {fts[4]}\n" +
            $"Feature 6 = Danger (Pieces) (Current - Enemy) (MIN) {fts[5]}\n" +
            $"Feature 7 = Danger (Soldiers) (Current - Enemy) (MIN) {fts[6]}\n" +
            $"Feature 8 = Danger (Cannons) (Current - Enemy) (MIN) {fts[7]}\n" +
            $"Feature 9 = Danger (Towns) (Current - Enemy) (MIN) (IMPORTANT -> Potential WIN / LOSE) {fts[8]}\n" +
            $"Feature 10 = Mobility (Possible moves) (Current - Enemy) (MAX) {fts[9]}\n" +
            $"Feature 11 = random factor {fts[10]}\n" +
            $"###########################################################\n");
    }

    public void printFeatures2(Board B)
    {
        int[] fts = getFeatures2(B);

        Console.WriteLine($"###########################################################\n" +
            $"Feature 1 - Material (Pieces) (Current - Enemy) (MAX) {fts[0]}\n" +
            $"Feature 2 - Material (Soldier) (Current - Enemy) (MAX) {fts[1]}\n" +
            $"Feature 3 - Material (Cannon) (Current - Enemy) (MAX) {fts[2]}\n" +
            $"Feature 7 - Material (Town) (Current - Enemy) (MAX) (IMPORTANT -> WIN / LOSE) {fts[6]}\n" +
            $"Feature 8 - Control (Current - Enemy) (MAX) {fts[7]}\n" +
            $"Feature 9 = Danger (Pieces) (Current - Enemy) (MIN) {fts[8]}\n" +
            $"Feature 10 = Danger (Soldiers) (Current - Enemy) (MIN) {fts[9]}\n" +
            $"Feature 11 = Danger (Cannons) (Current - Enemy) (MIN) {fts[10]}\n" +
            $"Feature 12 = Danger (Towns) (Current - Enemy) (MIN) (IMPORTANT -> Potential WIN / LOSE) {fts[11]}\n" +
            $"Feature 15 = Mobility (Possible moves) (Current - Enemy) (MAX) {fts[14]}\n" +
            $"Feature 16 = random factor {fts[15]}\n" +
            $"\n" +
            $"Feature 4 - Material Town Reachable (Pieces) (Current - Enemy) (MAX) {fts[3]}\n" +
            $"Feature 5 - Material Town Reachable (Soldier) (Current - Enemy) (MAX) {fts[4]}\n" +
            $"Feature 6 - Material Town Reachable (Cannon) (Current - Enemy) (MAX) {fts[5]}\n" +
            $"Feature 13 - Control Town Reachable (Current - Enemy) (MAX) {fts[12]}\n" +
            $"Feature 14 - Control Around Town (Current - Enemy) (MAX) {fts[13]}\n" +
            $"Feature 16 - Minimum distance to Town (Current - Enemy) {fts[15]}\n" +
            $"" +
            $"###########################################################\n");

    }

    public void printFeatures3(Board B)
    {
        getFeatures3(B);

        Console.WriteLine($"###########################################################\n" +
            $"Feature 1 - Material (Pieces) (Current - Enemy) (MAX) {this.fts.MaterialPiece}\n" +
            $"Feature 2 - Material (Soldier) (Current - Enemy) (MAX) {this.fts.MaterialSoldier}\n" +
            $"Feature 3 - Material (Cannon) (Current - Enemy) (MAX) {this.fts.MaterialCannon}\n" +
            $"Feature 4 - Material (Town) (Current - Enemy) (MAX) (IMPORTANT -> WIN / LOSE) {this.fts.MaterialTown}\n" +
            $"Feature 5 - Control (Current - Enemy) (MAX) {this.fts.InControl}\n" +
            $"Feature 6 = Danger (Pieces) (Current - Enemy) (MIN) {this.fts.InDangerPiece}\n" +
            $"Feature 7 = Danger (Soldiers) (Current - Enemy) (MIN) {this.fts.InDangerSoldier}\n" +
            $"Feature 8 = Danger (Cannons) (Current - Enemy) (MIN) {this.fts.InDangerCannon}\n" +
            $"Feature 9 = Danger (Towns) (Current - Enemy) (MIN) (IMPORTANT -> Potential WIN / LOSE) {this.fts.InDangerTown}\n" +
            $"Feature 10 = Mobility (Possible moves) (Current - Enemy) (MAX) {this.fts.Mobility}\n" +
            $"Feature 11 = random factor {this.fts.Random}\n" +
            $"###########################################################\n");
    }

    // Count Pieces
    int[] CountPieces(Board B)
    {
        // Initialise Parameters
        int offSet = 3;
        int[] count = new int[offSet * 2]; // [# of pieces player, # of soldiers player, # of cannons player,
                                           // # of pieces opponent, # of soldiers opponent, # of cannons opponent

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() == this.playerId)
            {
                // Count number of Pieces
                count[0]++;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    count[1]++;
                }
                else
                {
                    count[2]++;
                }
            }

            // Else opponent
            else
            {
                // Count number of Pieces
                count[0+offSet]++;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    count[1+offSet]++;
                }
                else
                {
                    count[2+offSet]++;
                }
            }
        }

        return count;
    }

    int[] CountPieces2(Board B)
    {
        // Initialise Parameters
        int offSet = 6;
        int[] count = new int[offSet * 2]; // [# of pieces player, # of soldiers player, # of cannons player,
                                           // # of pieces opponent, # of soldiers opponent, # of cannons opponent

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() == this.playerId)
            {
                // Count number of Pieces
                count[0]++;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    count[1]++;
                }
                else
                {
                    count[2]++;
                }

                // Count number of pieces that can reach the town
                if (!B.getUnreachableCoords().getPlayer(this.playerId).Contains(B.getPiecesCoords()[i]))
                {
                    count[3]++;

                    // Count number of soldiers and cannons
                    if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                    {
                        count[4]++;
                    }
                    else
                    {
                        count[5]++;
                    }
                }
            }

            // Else opponent
            else
            {
                // Count number of Pieces
                count[0 + offSet]++;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    count[1 + offSet]++;
                }
                else
                {
                    count[2 + offSet]++;
                }

                // Count number of pieces that can reach the town
                if (!B.getUnreachableCoords().getPlayer(this.playerId == 1 ? 2 : 1).Contains(B.getPiecesCoords()[i]))
                {
                    count[3 + offSet]++;

                    // Count number of soldiers and cannons
                    if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                    {
                        count[4 + offSet]++;
                    }
                    else
                    {
                        count[5 + offSet]++;
                    }
                }
            }
        }

        return count;

        //if (this.playerId == 1)
        //    return count;
        //else
        //    return new int[6] { count[offSet], count[offSet + 1], count[offSet + 2], count[0], count[1], count[2] };
    }

    void CountPieces3(Board B)
    {
        this.fts.MaterialPiece = 0; this.fts.MaterialCannon = 0; this.fts.MaterialSoldier = 0;
        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() == this.playerId)
            {
                // Count number of Pieces
                this.fts.MaterialPiece++;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    this.fts.MaterialSoldier++;
                }
                else
                {
                    this.fts.MaterialCannon++;
                }
            }

            // Else opponent
            else
            {
                // Count number of Pieces
                this.fts.MaterialPiece--;

                // Count number of soldiers and cannons
                if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceType() == Piece.epieceType.soldier)
                {
                    this.fts.MaterialSoldier--;
                }
                else
                {
                    this.fts.MaterialCannon--;
                }
            }
        }
    }

    int[] CountTown(Board B)
    {
        int[] count = new int[2];

        if (B.getTownCoords()[0] != default(Coord))
            count[0]++;
        if (B.getTownCoords()[1] != default(Coord))
            count[1]++;

        if (this.playerId == 1)
            return count;
        else
            return new int[2] { count[1], count[0] };
    }

    void CountTown3(Board B)
    {
        this.fts.MaterialTown = 0;
        if (B.getTownCoords()[0] != default(Coord))
            if (this.playerId == 1)
                this.fts.MaterialTown++;
            else
                this.fts.MaterialTown--;

        if (B.getTownCoords()[1] != default(Coord))
            if (this.playerId == 2)
                this.fts.MaterialTown++;
            else
                this.fts.MaterialTown--;
    }

    int[] getNumberOfPossibleMoves(Board B)
    {
        int[] count = new int[2];

        // Look for both players how many moves are available
        count[0] = B.getPossibleMoves(this.playerId).Count();
        count[1] = B.getPossibleMoves(this.playerId == 1 ? 2 : 1).Count();

        return count;
    }

    void getNumberOfPossibleMoves3(Board B)
    {
        // Look for both players how many moves are available
        this.fts.Mobility = B.getPossibleMoves(this.playerId).Count() - B.getPossibleMoves(this.playerId == 1 ? 2 : 1).Count();
    }

    int[] getNumberPiecesInControlAndDanger(Board B)
    {
        return B.countControlAndDanger(this.playerId);
    }

    int[] getNumberPiecesInControlAndDanger2(Board B)
    {
        return B.countControlAndDanger2(this.playerId);
    }

    void getNumberPiecesInControlAndDanger3(Board B)
    {
        this.fts = B.countControlAndDanger3(this.playerId, this.fts);
    }

    int[] getMinimumDistanceToTown(Board B)
    {
        int[] result = new int[2];
        result.setAll(10);

        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            if (B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() == 1)
            {
                if (!B.getUnreachableCoords().getPlayer(1).Contains(B.getPiecesCoords()[i]))
                    result[this.playerId == 1 ? 0 : 1] = Math.Min(result[this.playerId == 1 ? 0 : 1], Board.n - 1 - B.getPiecesCoords()[i].y);
            }

            // Else opponent
            else
            {
                if (!B.getUnreachableCoords().getPlayer(2).Contains(B.getPiecesCoords()[i]))
                    result[this.playerId == 2 ? 0 : 1] = Math.Min(result[this.playerId == 2 ? 0 : 1], B.getPiecesCoords()[i].y);
            }
        }

        return result;
    }
}

///  Players
internal class Human : Player
{
    Regex moveFormat = new Regex("([A-Z][0-9]{1,2}[x|-][A-Z][0-9]{1,2})|([x][A-Z][0-9]{1,2})");
    Regex placementFormat = new Regex("([A-Z][0-9]{1,2})");

    public Human(int id)
    {
        this.playerId = id;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Move> moves = B.getPossibleMoves(this.playerId);

        // If legal moves continue game
        if (moves.Count() > 0)
        {
            // Get player move
            Move move = getPlayerMove(moves);

            // Check if the move is a possible move
            bool cntinue = true;
            while (cntinue)
            {
                // If it is, make the move
                if (moves.Contains(move))
                {
                    // Move piece
                    B.movePiece(move, print, false, true, false);
                    cntinue = false;
                }
                else if (move.From.x == 99)
                {
                    B.undoMoveList(playerOne, playerTwo);
                    B.switchPlayer(playerOne, playerTwo);
                    cntinue = false;
                }

                // Otherwise choose a different move
                else
                {
                    // Update user
                    Console.WriteLine("Your move hasn't been found, try again!");

                    // Get new move
                    move = getPlayerMove(moves);
                }
            }
        }
        // Else set no legal moves
        else
            this.NoLegalMoves();

    }

    Move getPlayerMove(List<Move> possibleMoves)
    {
        // Let the user enter his move
        Console.Write("Enter move: ");
        string chessNotation = Console.ReadLine();

        // Convert to move
        Move move = convertToMove(chessNotation, possibleMoves);

        return move;
    }

    Move convertToMove(string chessNotation, List<Move> possibleMoves)
    {
        // Check if valid format, else return empty move {0, 0, 0, 0}
        if (this.moveFormat.IsMatch(chessNotation))
        {
            // Split notation in from and to
            char[] delimiters = { '-', 'x' };
            string[] splitNotation = chessNotation.Split(delimiters);

            // Shoot -> First element is empty && contains x
            if (splitNotation[0].Equals(""))
            {
                // Determine the indices on the "to part"
                Coord toIndex = nameToCoords(splitNotation[1]);
                Console.WriteLine($"Creates coords: {toIndex}");
                List<Move> moveList = possibleMoves.Where(x => x.type == Move.moveType.shoot &&
                                                                        x.To.x == toIndex.x &&
                                                                        x.To.y == toIndex.y).ToList();

                // Return move that satisfies condition (if found)
                if (moveList.Count() > 0)
                    return moveList[0];
                else
                    return default(Move);
            }

            else if (chessNotation.Contains('x'))
            {
                // Determine the indices on the "from" and "to" part
                Coord fromIndex = nameToCoords(splitNotation[0]);
                Coord toIndex = nameToCoords(splitNotation[1]);

                // Return move that satisfies condition
                Move newMove = new Move(fromIndex.x, fromIndex.y, toIndex.x, toIndex.y, Move.moveType.soldierCapture);
                if (possibleMoves.Contains(newMove))
                    return newMove;
                else
                    return default(Move);
            }

            // Else (when there is no x) -> Two elements, where both of them contain one Letter and One or Two numbers
            else
            {
                // Determine the indices on the "from" and "to" part
                Coord fromIndex = nameToCoords(splitNotation[0]);
                Coord toIndex = nameToCoords(splitNotation[1]);
                List<Move> moveList = possibleMoves.Where(x => x.From.x == fromIndex.x &&
                                                                x.From.y == fromIndex.y &&
                                                                x.To.x == toIndex.x &&
                                                                x.To.y == toIndex.y &&
                                                                (x.type == Move.moveType.step ||
                                                                x.type == Move.moveType.retreat ||
                                                                x.type == Move.moveType.slide)).ToList();

                // Return move that satisfies condition
                if (moveList.Count() == 1)
                    return moveList[0];
                else
                    return default(Move);
            }
        }
        else if (chessNotation == "undo")
            return new Move(99, 99, 99, 99, Move.moveType.step);
        else
            return default(Move);
    }

    Coord nameToCoords(string spaceName)
    {
        return new Coord( Convert.ToInt32(spaceName[0] - 65),
            Int32.Parse(new String(spaceName.Where(Char.IsDigit).ToArray())) - 1);
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());
        
        // Get player move
        Coord move = getPlayerPlacement(placements);

        // Check if the move is a possible move
        bool cntinue = true;
        while (cntinue)
        {
            // If it is, make the move
            if (placements.Contains(move))
            {
                // Move piece
                B.placeTown(move, print);
                cntinue = false;
            }
            // Otherwise choose a different move
            else
            {
                // Update user
                Console.WriteLine("Your placement hasn't been found, try again!\n");

                // Get new move
                move = getPlayerPlacement(placements);
            }
        }   
    }

    Coord getPlayerPlacement(List<Coord> possiblePlacements)
    {
        // Let the user enter his move
        Console.Write("Enter move: ");
        string chessNotation = Console.ReadLine();

        // Convert to move
        Coord move = convertToPlacement(chessNotation, possiblePlacements);

        return move;
    }

    Coord convertToPlacement(string chessNotation, List<Coord> possiblePlacements)
    {
        // Check if valid format, else return empty placement {0, 0} (not a legal placement)
        if (this.placementFormat.IsMatch(chessNotation))
            return nameToCoords(chessNotation);
        else
            return default(Coord);
    }
}

/// Bots
internal class RandomBot : Player
{
    static Random rnd = new Random();

    public RandomBot(int id)
    {
        this.playerId = id;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Move> moves = B.getPossibleMoves(this.playerId);

        // If legal moves continue game
        if (moves.Count() > 0)
        {
            Move move = moves[rnd.Next(moves.Count())];

            B.movePiece(move, print, false, true, false);
        }
        // Else set no legal moves
        else
            this.NoLegalMoves();

    }

    public override int makeMove(Board B, bool print, bool analyses)
    {
        List<Move> moves = B.getPossibleMoves(this.playerId);

        // If legal moves continue game
        if (moves.Count() > 0)
        {
            Move move = moves[rnd.Next(moves.Count())];

            B.movePiece(move, print, false, true, false);
        }
        // Else set no legal moves
        else
            this.NoLegalMoves();

        return moves.Count();

    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        Coord placement = placements[rnd.Next(placements.Count())];

        B.placeTown(placement, print);
    }
}

internal class HeuristicBot : Player
{
    static Random rnd = new Random();
    float epsilon;

    public HeuristicBot(int id, float epsilon)
    {
        this.playerId = id;
        this.epsilon = epsilon;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Move> moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        Move move;

        // If legal moves continue game
        if (moves.Count() > 0)
        {
            // Epsilon greedy
            if (rnd.NextDouble() < this.epsilon)
                move = moves[rnd.Next(moves.Count())];
            else
                move = moves[0];

            B.movePiece(move, print, false, true, false);
        }
        // Else set no legal moves
        else
            this.NoLegalMoves();

    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        Coord placement = placements[rnd.Next(placements.Count())];

        B.placeTown(placement, print);
    }
}

/// Attempt 4 (of creating bots)
// Only use optimal improvements (Iterative Deepening, TT, Knowledge, Fractional Plies, Null Move) 
internal class OptimalBot : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    // {4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5}
    // {4, 7, 13, -3, 5, 4, -16, -12, 3, 12, -6} // weights GA
    // {4, 6, 15, -2, 6, 2, -11, -10, 4, 10, -5} // Weights GA <- 14/15 won
    // {1, 5, -2, 2, 5, 6, -16, -10, 4, 7, -8} // Weights GA -< 13/15 won

    // 9 Generations - 5 games (scale up to 15 to seperate good ones) - Population 16 

    // Move ordering
    TranspositionTable TT;

    // Dynamic depth
    int R;
    bool endGame = false;

    public OptimalBot(int id, int searchTimeMs, int lengthHashKey, int R, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Null Move and Multi-Cut
        this.R = R;

        //if (this.playerId == 1)
        //{
        //    this.weights = new int[] { 4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5 };
        //}
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true, false);

            // Check if it is the end game, if so add it (for forward prunings)
            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Forward pruning
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                // Null Move
                if (!lastNullMove)
                {
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);

                    if (score > Beta)
                    {
                        return score;
                    }
                }
            }

            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, float depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Determine fractional ply
        float fracply = move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture ? .5f : 1f;

        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        // Min numer of pieces
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Aspirations search with all optimizations (Iterative Deepening, TT, KM, Knowledge, HH, Fractional Plies, Null Move, MultiCut)
// Adjusts time needed on time left and expected time
internal class OptimizedASAdjust : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch playTime = new Stopwatch();
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    //int[] weights = new int[] { 5, 1, 2, 100, 2, -2, -1, -1, -10, 2, 1 };
    //int[] weights = new int[] { 4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5 };
    // {4, 7, 13, -3, 5, 4, -16, -12, 3, 12, -6} // weights GA


    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    // Window
    int delta;

    // Dynamic depth
    int R, C, M;
    bool endGame = false;

    // Monte Carlo Evaluation for plies left (determine time per move)
    int maxTimeGame;
    int maxTime;
    int maxTimeLimit;

    public OptimizedASAdjust(int id, int MaxSearchTimeMs, int SearchTimeLimitMs, int numberOfEstimations, float epsilon, int lengthHashKey, int delta, int R, int C, int M, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTimeGame = MaxSearchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);
        this.adjustTime = true;
        this.maxTimeLimit = SearchTimeLimitMs;
        this.maxTime = Math.Min(MaxSearchTimeMs / 65, SearchTimeLimitMs);
        this.numberOfEstimations = numberOfEstimations;
        this.epsilon = epsilon;

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Search Windows
        this.delta = delta;

        // Null Move and Multi-Cut
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        this.playTime.Start();
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];
            int guess = 0;

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta (AS)
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                // Fail high - research
                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                // Fail low - research
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                // Change guess
                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();
            this.playTime.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);

            // Check if it is the end game, if so add it (for forward prunings)
            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);




        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Forward pruning
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                // Null Move
                if (!lastNullMove)
                {
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);

                    if (score > Beta)
                    {
                        return score;
                    }
                }


                // Multicut (if null move fails)
                int c = 0, m = 0;
                while (m < possibleMoves.Count() && m < this.M)
                {
                    value = -simulateMove(B, possibleMoves[m], depth - 1 - this.R, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color * -1, false);

                    if (value >= Beta)
                    {
                        c++;
                        if (c > this.C)
                        {
                            return Beta;
                        }
                    }

                    m++;
                }
            }

            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];
        int guess = 0;

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // Fail high - research
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            // Fail low - research
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

            // Change guess
            guess = scores.Max();

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, float depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Determine fractional ply
        float fracply = move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture ? .5f : 1f;

        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        // Min numer of pieces
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }

    public override void adjustMaxTime(int expectedPly)
    {
        this.maxTime = Math.Min((int)(this.maxTimeGame - this.playTime.ElapsedMilliseconds) / expectedPly, this.maxTimeLimit);
        Console.WriteLine($"Updated time per move to {this.maxTime} [ms].\n");
    }
}

// Aspirations search with all optimizations (Variable depth, replaced HH with Relative HH)
internal class OptimizedASBH : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    //int[] weights = new int[] { 4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5 };
// {4, 7, 13, -3, 5, 4, -16, -12, 3, 12, -6} // weights GA
// {4, 6, 15, -2, 6, 2, -11, -10, 4, 10, -5} // Weights GA <- 14/15 won
// {1, 5, -2, 2, 5, 6, -16, -10, 4, 7, -8} // Weights GA

// 9 Generations - 5 games (scale up to 15 to seperate good ones) - Population 16 

// Move ordering
TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int[,,,] butterflyTable = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    // Window
    int delta;

    // Dynamic depth
    int R, C, M;
    bool endGame = false;

    public OptimizedASBH(int id, int searchTimeMs, int lengthHashKey, int delta, int R, int C, int M, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Search Windows
        this.delta = delta;

        // Null Move and Multi-Cut
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];
            int guess = 0;

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta (AS)
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                // Fail high - research
                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                // Fail low - research
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                // Change guess
                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            this.butterflyTable.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);

            // Check if it is the end game, if so add it (for forward prunings)
            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVDPlus(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic, this.butterflyTable);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;
        bool addedToHH = false;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Forward pruning
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                // Null Move
                if (!lastNullMove)
                {
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);

                    if (score > Beta)
                    {
                        return score;
                    }
                }


                // Multicut (if null move fails)
                int c = 0, m = 0;
                while (m < possibleMoves.Count() && m < this.M)
                {
                    value = -simulateMove(B, possibleMoves[m], depth - 1 - this.R, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color * -1, false);

                    if (value >= Beta)
                    {
                        c++;
                        if (c > this.C)
                        {
                            return Beta;
                        }
                    }

                    m++;
                }
            }

            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                            {
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                addedToHH = true;
                            }
                                
                            break;
                        }
                    }
                }

                if (!addedToHH)
                    this.butterflyTable[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;

                addedToHH = false;

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];
        int guess = 0;

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // Fail high - research
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            // Fail low - research
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

            // Change guess
            guess = scores.Max();

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, float depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Determine fractional ply
        float fracply = move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture ? .5f : 1f;

        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        // Min numer of pieces
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Aspirations search with all optimizations (Iterative Deepening, TT, KM, Knowledge, HH, Fractional Plies, Null Move, MultiCut) 
internal class OptimizedAS: Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    // {4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5}
    // {4, 7, 13, -3, 5, 4, -16, -12, 3, 12, -6} // weights GA
    // {4, 6, 15, -2, 6, 2, -11, -10, 4, 10, -5} // Weights GA <- 14/15 won
    // {1, 5, -2, 2, 5, 6, -16, -10, 4, 7, -8} // Weights GA -< 13/15 won

    // 9 Generations - 5 games (scale up to 15 to seperate good ones) - Population 16 

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    
    // Window
    int delta;

    // Dynamic depth
    int R, C, M;
    bool endGame = false;

    public OptimizedAS(int id, int searchTimeMs, int lengthHashKey, int delta, int R, int C, int M, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Search Windows
        this.delta = delta;

        // Null Move and Multi-Cut
        this.R = R;
        this.C = C;
        this.M = M;

        //if (this.playerId == 1)
        //{
        //    this.weights = new int[] { 4, 6, 15, -2, 6, -2, -11, -10, -4, 10, -5 };
        //}
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); 
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];
            int guess = 0;

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta (AS)
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                // Fail high - research
                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                // Fail low - research
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                // Change guess
                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);

            // Check if it is the end game, if so add it (for forward prunings)
            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++; 

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Forward pruning
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                // Null Move
                if (!lastNullMove)
                {
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);

                    if (score > Beta)
                    {
                        return score;
                    }
                }
                

                // Multicut (if null move fails)
                int c = 0, m = 0;
                while (m < possibleMoves.Count() && m < this.M)
                {
                    value = -simulateMove(B, possibleMoves[m], depth - 1 - this.R, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color * -1, false);

                    if (value >= Beta)
                    {
                        c++;
                        if (c > this.C)
                        {
                            return Beta;
                        }
                    }

                    m++;
                }
            }

            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];
        int guess = 0;

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // Fail high - research
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            // Fail low - research
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

            // Change guess
            guess = scores.Max();

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, float depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Determine fractional ply
        float fracply = move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture ? .5f : 1f;

        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        // Min numer of pieces
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Aspirations search with variable depth (Iterative Deepening, TT, KM, Knowledge, HH, Fractional Plies, Null Move) 
internal class AS_FP_NL : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    // Window
    int delta;

    // Dynamic depth
    int R;
    bool endGame = false;

    public AS_FP_NL(int id, int searchTimeMs, int lengthHashKey, int delta, int R, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Search Windows
        this.delta = delta;

        // Null Move
        this.R = R;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];
            int guess = 0;

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta (AS)
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                // Fail high - research
                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                // Fail low - research
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                // Change guess
                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);

            // Check if it is the end game, if so add it (for forward prunings)
            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Forward pruning
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                // Null Move
                if (!lastNullMove)
                {
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);

                    if (score > Beta)
                    {
                        return score;
                    }
                }
            }

            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];
        int guess = 0;

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // Fail high - research
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            // Fail low - research
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

            // Change guess
            guess = scores.Max();

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, float depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Determine fractional ply
        float fracply = move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture ? .5f : 1f;

        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        // Min numer of pieces
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Aspirations search with fixed depth (Iterative Deepening, TT, KM, Knowledge, HH) 
internal class OrderedAS : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    // Window
    int delta;

    public OrderedAS(int id, int searchTimeMs, int lengthHashKey, int delta, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);

        // Search Windows
        this.delta = delta;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];
            int guess = 0;

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine Alpha and Beta (AS)
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores (NegaMax)
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                // Fail high - research
                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                // Fail low - research
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                // Change guess
                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];
        int guess = 0;

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // Fail high - research
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            // Fail low - research
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

            // Change guess
            guess = scores.Max();

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Principal Variation Search / NegaScout with move ordering (Iterative Deepening, TT, KM, Knowledge, HH) 
internal class OrderedPVS : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    public OrderedPVS(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValueIndex = 0;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // NegaScout
            int bestValue = simulateMove(B, possibleMoves[0], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);
            if (bestValue < Beta)
            {
                // Look at all moves
                for (int i = 1; i < possibleMoves.Count(); i++)
                {
                    // Determine small window (by looking at best value
                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

                    // Determine value
                    value = simulateMove(B, possibleMoves[i], depth, lbound, ubound, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // Check if research is fail high
                    if (value >= ubound && value < Beta)
                        value = simulateMove(B, possibleMoves[i], depth, ubound, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // If it is at our search depth, and isn't during placement, add score to list
                    if (!placeTown && depth == this.searchDepth)
                    {
                        this.scores[i] = value;
                    }

                    // Check if value is higher (better move)
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestValueIndex = i;

                        // Check if it is higher than Alpha
                        if (bestValue > Alpha)
                        {
                            Alpha = bestValue;

                            // Check if Alpha >= Beta, such that we can prune
                            if (Alpha >= Beta)
                            {
                                // Save to killermove and history heuristic
                                this.killerMoves[(int)(depth - 1)] = possibleMoves[i];
                                if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                    this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                break;
                            }
                        }
                    }

                    // If no time less, break
                    if (this.sw.ElapsedMilliseconds > this.maxTime)
                        break;
                }
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Iterative Deepening with move ordering (Iterative Deepening, TT, KM, Knowledge, HH)
internal class OrderedID : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    public OrderedID(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move, update HH
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove and history heuristic
                            this.killerMoves[(int)(depth - 1)] = possibleMoves[i];
                            if (possibleMoves[i].type != Move.moveType.shoot || possibleMoves[i].type != Move.moveType.soldierCapture)
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Iterative Deepening with move ordering (Iterative Deepening, TT, KM, Knowledge)
internal class ID_TT_KM : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;
    Move[] killerMoves; // depth, move

    public ID_TT_KM(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            // Save to killermove
                            this.killerMoves[(int)(depth - 1)] = possibleMoves[i];
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);
            this.killerMoves = new Move[2 * searchDepth];

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Iterative Deepening with move ordering (Iterative Deepening, TT, Knowledge)
internal class ID_TT : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    // Move ordering
    TranspositionTable TT;

    public ID_TT(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        // TT
        this.TT = new TranspositionTable(lengthHashKey);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(B.getCurrentHashKey(), B.getCurrentHashValue());
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth && depth >= 0)
        {
            if (entry.type == TTEntry.flag.exact)
                return entry.value;
            else if (entry.type == TTEntry.flag.lowerBound)
                Alpha = Math.Max(Alpha, entry.value);
            else if (entry.type == TTEntry.flag.upperBound)
                Beta = Math.Min(Beta, entry.value);
            if (Alpha >= Beta)
                return entry.value;
        }

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove);

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;
                    bestValueIndex = i;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(B.getCurrentHashKey(), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, B.getCurrentHashValue());

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
        this.TT.reset();
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Iterative Deepening with move ordering (Iterative Deepening, Knowledge)
internal class IterativeDeepening : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    public IterativeDeepening(int id, int searchTimeMs, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = B.getPossibleMoves(currentPlayerId).orderByMoveType();

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Iterative Deepening with move ordering (Iterative Deepening)
internal class IterativeDeepeningNoOrdering : Player
{
    // Initiliaze everything needed
    // General (Iterative Deepening)
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };

    public IterativeDeepeningNoOrdering(int id, int searchTimeMs, bool printIterations)
    {
        // Initiliase
        // General (Iterative Deepening)
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start Stopwatch
        sw.Restart(); sw.Start();

        // Determine moves and order on knowledge
        this.moves = B.getPossibleMoves(this.playerId).cloneList();
        this.scores = new int[moves.Count()];

        // Reset node counter
        this.seenNodes = 0;
        // If there are moves left
        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            // While time left
            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves on scores of previous search
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);

                // Determine scores (NegaMax)
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherwise score isn't complete -> needs complicated look at)
                if (this.sw.ElapsedMilliseconds < this.maxTime)
                {
                    bestMove = this.moves[this.scores.argMax()];
                    actualDepth = this.searchDepth;
                    nrOfNodes = this.seenNodes;

                    // Print time per iteration
                    if (this.printIterations)
                    {
                        // Print performance
                        Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
                    }
                }
                else
                {
                    // If it isn't entireyly searched
                    // Check the value we would get for choosing the first one
                    int bestVal = this.scores[0];
                    int i = 1;

                    // If this one is searched entirely (next one is not equal to -this.evalBound)
                    // Check if this is better than the move we choose to make (at the previous depth)
                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
                        // If it is better, save it
                        if (this.scores[i] > bestVal)
                        {
                            bestMove = this.moves[i];
                            actualDepth = this.searchDepth;
                            nrOfNodes = this.seenNodes;
                        }

                        i++;
                    }
                }

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            if (printIterations)
                Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true, false);
        }
        // No legal moves is loss.
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Keep track of number of nodes seen
        this.seenNodes++;

        // If town is removes -> win (terminal)
        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        // If threefold -> draw (terminal)
        if (B.getMaxFolds() == 3) { return (-this.evalBound + 1) * color; }
        // If leafnode , return evaluate
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        // Get currentId
        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = B.getPossibleMoves(currentPlayerId).cloneList();

        // Initialise for search
        int value = -this.evalBound - 1;
        int bestValue = value;

        // If moves left
        if (possibleMoves.Count() > 0)
        {
            // Look at all moves
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher (better move)
                if (value > bestValue)
                {
                    bestValue = value;

                    // Check if it is higher than Alpha
                    if (bestValue > Alpha)
                    {
                        Alpha = bestValue;

                        // Check if Alpha >= Beta, such that we can prune
                        if (Alpha >= Beta)
                        {
                            break;
                        }
                    }
                }

                // If no time less, break
                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Return value
            return bestValue;
        }
        else
        {
            // No move left is loss
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Initialise
        int nrOfNodes = 0;
        int actualDepth = 0;
        this.seenNodes = 0;
        this.searchDepth = 1;
        int[] scores = new int[placements.Count()];
        Coord bestPlacement = placements[0];

        while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
        {
            // Order placements
            placements = orderPlacements(placements, scores);

            // Reset scores and killermoves
            scores.setAll(-this.evalBound);

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

            // Print time per iteration
            if (this.printIterations)
            {
                // Print performance
                Console.WriteLine($"Depth: {this.searchDepth}. Nodes seen: {this.seenNodes}. Time: {this.sw.ElapsedMilliseconds} [ms].");
            }

            // Get best placement (if enough time is left, otherewise score isn't complete)
            if (this.sw.ElapsedMilliseconds < this.maxTime)
            {
                bestPlacement = placements[scores.argMax()];
                actualDepth = searchDepth;
                nrOfNodes = this.seenNodes;
            }

            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
            this.searchDepth++;
            B.updateCannons();
        }

        // Stop stopwatch
        this.sw.Stop();

        // Print nodes evaluated
        if (printIterations)
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Check if to position is town (when captured or shoot)
        bool isTown = false;
        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
            isTown = true;

        // Make Move (and update hash)
        B.movePiece(move, false, placeTown, false, isTown);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move        
        B.UndoMove(move, placeTown, isTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            // Place town
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Determine scores
            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            // Switch player
            B.switchPlayer(playerOne, playerTwo);

            // Remove town
            B.removeTown(placements[i]);
        }

        // Return scores of placements
        return scores;
    }

    // Reset TT
    public override void resetTT()
    {
    }

    // Set weights (for GA)
    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}