using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using World;
using TT;
using System.Text.RegularExpressions;

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


public class Player
{
    public int playerId;
    bool LegalMoves = true;
    static Random rnd = new Random();
    List<Move> orderedMoves = new List<Move>();
    features fts = new features();

    public virtual void makeMove(Board B, bool print, Player playerOne, Player playerTwo) { }
    public virtual int makeMove(Board B, bool print, bool analyses) { return 0; }
    public virtual void placeTown(Board B, bool print, Player playerOne, Player playerTwo) { }

    public virtual void resetTT() { }

    public virtual void setWeights(int[] wghts) { }
    public virtual int[] getWeights() { return new int[2]; }

    public int getPlayerId()
    {
        return this.playerId;
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
        return Math.Abs(wght[0] * 15 + wght[1] * 15 + wght[2] * 15 + wght[3] * 1 + wght[4] * 100 + wght[5] * 15 + wght[6] * 15 + wght[7] * 15 + wght[8] * 1 + wght[9] * 70);
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
                    B.movePiece(move, print, false, true);
                    cntinue = false;
                }
                else if (move.From.x == 99)
                {
                    B.undoMoveList();
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
                List<Move> moveList = possibleMoves.Where(x => x.type == Move.moveType.shoot &&
                                                                        x.To.x == toIndex.x &&
                                                                        x.To.y == toIndex.y).ToList();

                // Return move that satisfies condition
                if (moveList.Count() == 1)
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

            B.movePiece(move, print, false, true);
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

            B.movePiece(move, print, false, true);
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


/// Attempt 1
//internal class NegaMax : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;

//    // Depth = 1, Nodes evaluated: 42. In 7 [ms].
//    // Depth = 2, Nodes evaluated: 1782. In 19 [ms].
//    // Depth = 3, Nodes evaluated: 69737. In 726 [ms].
//    // Depth = 4, Nodes evaluated: 2885028. In 26914 [ms].

//    // Basic NegaMax no pruning
//    public NegaMax(int id, int searchDepth)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.evalBound = this.getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get all possible moves
//        List<Move> moves = new List<Move>();

//        // Reset node counter
//        this.seenNodes = 0;

//        // If legal moves continue game
//        if (moves.Count() > 0)
//        {
//            // Determine best move
//            int[] scores = new int[moves.Count()];
//            for (int i = 0; i < moves.Count(); i++)
//            {
//                // Move the piece
//                B.movePiece(moves[i], false, false);
                
//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get score
//                scores[i] = -NegaMaxSearch(B, this.searchDepth-1, playerOne, playerTwo, -1, false);

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(moves[i]);

//                // No update needed, will do it before determining the score -> Saves time

//            }

//            // Stop Stopwatch
//            this.sw.Stop();

//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[scores.argMax()], print, false);
//        }
//        // Else set no legal moves
//        else
//            this.NoLegalMoves();

//    }

//    int NegaMaxSearch(Board B, int depth, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
//        int value = -this.evalBound;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Make Move
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                value = Math.Max(value, -NegaMaxSearch(B, depth - 1, playerOne, playerTwo, color * -1, placeTown));

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(possibleMoves[i]);
//            }

//            return value;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player
//            B.switchPlayer(playerOne, playerTwo);

//            scores[i] = -NegaMaxSearch(B, this.searchDepth-1, playerOne, playerTwo, -1, true);

//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }
//}

//internal class NegaMaxAlphaBeta : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;

//    // Basic NegaMax no pruning
//    public NegaMaxAlphaBeta(int id, int searchDepth)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.evalBound = getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get all possible moves
//        List<Move> moves = B.getPossibleMoves(this.playerId).orderByMoveType();

//        // Reset node counter
//        this.seenNodes = 0;

//        // If legal moves continue game
//        if (moves.Count() > 0)
//        {
//            // Determine best move
//            int[] scores = new int[moves.Count()];
//            for (int i = 0; i < moves.Count(); i++)
//            {
//                // Move the piece
//                B.movePiece(moves[i], false, false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get score
//                scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth-1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, false);

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(moves[i], false);

//                // No update needed, will do it before determining the score -> Saves time

//            }

//            // Stop Stopwatch
//            this.sw.Stop();

//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[scores.argMax()], print);
//        }
//        // Else set no legal moves
//        else
//            this.NoLegalMoves();

//    }

//    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId()).orderByMoveType();
//        int value = -this.evalBound;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Make Move
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                value = Math.Max(value, -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown));

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(possibleMoves[i]);

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                if (Alpha >= Beta)
//                {
//                    break;
//                }
//            }

//            return value;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player 
//            B.switchPlayer(playerOne, playerTwo);

//            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth-1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }
//}

//internal class NegaMaxAlphaBetaTT : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;

//    // Basic NegaMax no pruning
//    public NegaMaxAlphaBetaTT(int id, int searchDepth, int lengthHashKey)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get entry, to order the moves
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));

//        // Get all possible moves and order
//        List<Move> moves = orderMoves(B.getPossibleMoves(this.playerId), entry.bestMove);

//        // Reset node counter
//        this.seenNodes = 0;

//        // If legal moves continue game
//        if (moves.Count() > 0)
//        {
//            // Determine best move
//            int[] scores = new int[moves.Count()];
//            for (int i = 0; i < moves.Count(); i++)
//            {
//                // Check if to position is town
//                bool isTown = false;
//                if ((moves[i].type == Move.moveType.shoot || moves[i].type == Move.moveType.soldierCapture) &&
//                    B.getSpaces()[moves[i].To.x, moves[i].To.y].getPieceType() == Piece.epieceType.town)
//                    isTown = true;

//                // Move the piece (and update hash)
//                this.currentHash = this.zH.makeMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//                B.movePiece(moves[i], false, false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Get score
//                scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, false);

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move (and update hash)
//                this.currentHash = this.zH.undoMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//                B.UndoMove(moves[i]);

//                // No update needed on cannons, will do it before determining the score -> Saves time

//            }

//            // Stop Stopwatch
//            this.sw.Stop();
//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[scores.argMax()], print);
//        }
//        // Else set no legal moves
//        else
//            this.NoLegalMoves();

//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;
//        }

//        // Alpha beta - With addition of keeping track of hashvalue
//        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound*color : -this.evalBound*color; } // Terminal state
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove);
//        int bestValue = -this.evalBound*10;
//        int bestValueIndex = -1;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Check if to position is town (when captured or shoot) (for changing hash value)
//                bool isTown = false;
//                if ((possibleMoves[i].type == Move.moveType.shoot || possibleMoves[i].type == Move.moveType.soldierCapture) &&
//                    B.getSpaces()[possibleMoves[i].To.x, possibleMoves[i].To.y].getPieceType() == Piece.epieceType.town)
//                    isTown = true;

//                // Make Move (and update hash)
//                this.currentHash = this.zH.makeMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color*-1, placeTown);

//                // Compare with best value
//                if (value > bestValue)
//                {
//                    bestValue = value;
//                    bestValueIndex = i;
//                }

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move (and update hash)
//                this.currentHash = this.zH.undoMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.UndoMove(possibleMoves[i]);

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                // Prune when possible
//                if (Alpha >= Beta)
//                {
//                    break;
//                }
//            }

//            // Store value in transposition table
//            TTEntry.flag flagType;
//            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//            else { flagType = TTEntry.flag.exact; }

//            // Set entry
//            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
//                depth, this.zH.getHashValue(this.currentHash));

//            // Return value
//            return bestValue;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Create Initial Hash
//        this.currentHash = this.zH.generateBoardHash(B);

//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // Switch player
//            B.switchPlayer(playerOne, playerTwo); 
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            // Switch back
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            // Remove cannon
//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class IterativeDeepening : Player
//{
//    int maxTime;
//    int seenNodes = 0;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    bool printIterations;

//    // Basic NegaMax no pruning
//    public IterativeDeepening(int id, int maxTimeMilliSec, int lengthHashKey, bool printIterations)
//    {
//        this.playerId = id;
//        this.maxTime = maxTimeMilliSec;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//        this.printIterations = printIterations;
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Reset node counter
//        this.seenNodes = 0;

//        // Get entry, to order the moves
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));

//        // Get all possible moves and order
//        List<Move> moves = new List<Move>();

//        // If legal moves continue game
//        if (moves.Count() > 0)
//        {
//            int searchDepth = 0;
//            int[] scores = new int[moves.Count()];
//            int[] oldScores = new int[moves.Count()];

//            int actualDepth = 0;
//            int nrOfNodes = 0;
//            // Look at start of round
//            long startSw = this.sw.ElapsedMilliseconds;

//            // While we have enough time and we didn't find a win already
//            while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
//            {
//                // Get entry, to order the moves
//                entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));

//                // Get all possible moves and order
//                moves = orderMoves(moves, entry.bestMove, scores);

//                // Update scores
//                scores = searchOneIteration(B, moves, searchDepth, playerOne, playerTwo);

//                // Print time per iteration
//                if (this.printIterations)
//                {
//                    // Print performance
//                    Console.WriteLine("Depth " + (searchDepth + 1) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//                }

//                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//                if (this.sw.ElapsedMilliseconds < this.maxTime)
//                {
//                    oldScores = (int[])scores.Clone();
//                    actualDepth = searchDepth;
//                    nrOfNodes = this.seenNodes;
//                }

//                //Console.WriteLine("[{0}]", string.Join(", ", oldScores));

//                // Look at next depth
//                searchDepth++;
//            }

//            // Stop Stopwatch
//            this.sw.Stop();
//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: "+ (actualDepth+1) +
//                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move (based on last iteration)
//            B.movePiece(moves[oldScores.argMax()], print);
//        }
//        // Else set no legal moves
//        else
//            this.NoLegalMoves();

//    }

//    int[] searchOneIteration(Board B, List<Move> moves, int searchDepth, Player playerOne, Player playerTwo)
//    {
//        // Determine best move
//        int[] scores = new int[moves.Count()];
//        for (int i = 0; i < moves.Count(); i++)
//        {
//            // Check if to position is town
//            bool isTown = false;
//            if ((moves[i].type == Move.moveType.shoot || moves[i].type == Move.moveType.soldierCapture) &&
//                B.getSpaces()[moves[i].To.x, moves[i].To.y].getPieceType() == Piece.epieceType.town)
//                isTown = true;

//            // Move the piece (and update hash)
//            this.currentHash = this.zH.makeMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//            B.movePiece(moves[i], false, false);

//            // Update Cannons
//            B.updateCannons();

//            // Switch player (and update hash)
//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.switchPlayer(playerOne, playerTwo);

//            // Get score
//            scores[i] = -NegaMaxAlphaBetaTTSearch(B, searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, false);

//            // Switch player (and update hash)
//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.switchPlayer(playerOne, playerTwo);

//            // Undo Move (and update hash)
//            this.currentHash = this.zH.undoMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//            B.UndoMove(moves[i]);

//            // No update needed on cannons, will do it before determining the score -> Saves time

//            if (this.sw.ElapsedMilliseconds > this.maxTime)
//                break;
//        }

//        return scores;
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;
//        }

//        // Alpha beta - With addition of keeping track of hashvalue
//        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

//        if (!placeTown && !B.TownsInGame()) { 
//            this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; } // Terminal state
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove);
//        int bestValue = -this.evalBound * 10;
//        int bestValueIndex = -1;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Check if to position is town (when captured or shoot)
//                bool isTown = false;
//                if ((possibleMoves[i].type == Move.moveType.shoot || possibleMoves[i].type == Move.moveType.soldierCapture) &&
//                    B.getSpaces()[possibleMoves[i].To.x, possibleMoves[i].To.y].getPieceType() == Piece.epieceType.town)
//                    isTown = true;

//                // Make Move (and update hash)
//                this.currentHash = this.zH.makeMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//                // Compare with best value
//                if (value > bestValue)
//                {
//                    bestValue = value;
//                    bestValueIndex = i;
//                }

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move (and update hash)
//                this.currentHash = this.zH.undoMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.UndoMove(possibleMoves[i]);

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                if (Alpha >= Beta)
//                {
//                    break;
//                }

//                if (this.sw.ElapsedMilliseconds > this.maxTime)
//                    break;
//            }

//            // Store value in transposition table
//            TTEntry.flag flagType;
//            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//            else { flagType = TTEntry.flag.exact; }

//            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
//                depth, this.zH.getHashValue(this.currentHash));

//            // Return value
//            return bestValue;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Create Initial Hash
//        this.currentHash = this.zH.generateBoardHash(B);

//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        // Initialise scores
//        int[] scores = new int[placements.Count()];
//        int[] oldScores = new int[placements.Count()];

//        int searchDepth = 0;
//        while (this.sw.ElapsedMilliseconds < this.maxTime)
//        {
//            placements = orderPlacements(placements, scores);
//            scores = searchOneIterationTown(B, placements, searchDepth, playerOne, playerTwo);

//            //Console.WriteLine("[{0}]", string.Join(", ", scores));

//            // Print time per iteration
//            if (this.printIterations)
//            {
//                // Print performance
//                Console.WriteLine("Depth " + (searchDepth + 1) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//            }

//            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//            if (this.sw.ElapsedMilliseconds < this.maxTime)
//                oldScores = (int[])scores.Clone();

//            searchDepth++;
//        }

//        // Stop the stopwatch
//        this.sw.Stop();

//        // Place town
//        B.placeTown(placements[oldScores.argMax()], print);
//    }

//    int[] searchOneIterationTown(Board B, List<Coord> placements, int searchDepth, Player playerOne, Player playerTwo)
//    {
//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // Switch player
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            scores[i] = -NegaMaxAlphaBetaTTSearch(B, searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            // Switch back
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            // Remove cannon
//            B.removeTown(placements[i]);

//            if (this.sw.ElapsedMilliseconds > this.maxTime)
//                break;
//        }

//        return scores;
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class IterativeDeepeningPlus : Player
//{
//    int maxTime;
//    int seenNodes = 0;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    bool printIterations;
//    Move[] killerMoves; // depth, move

//    // Basic NegaMax no pruning
//    public IterativeDeepeningPlus(int id, int maxTimeMilliSec, int lengthHashKey, bool printIterations)
//    {
//        this.playerId = id;
//        this.maxTime = maxTimeMilliSec;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//        this.printIterations = printIterations;
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Reset node counter
//        this.seenNodes = 0;

//        // Get entry, to order the moves
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));

//        // Get all possible moves and order
//        List<Move> moves = new List<Move>();

//        // If legal moves continue game
//        if (moves.Count() > 0)
//        {
//            int searchDepth = 0;
//            int[] scores = new int[moves.Count()];
//            int[] oldScores = new int[moves.Count()];

//            int actualDepth = 0;
//            int nrOfNodes = 0;
//            // Look at start of round
//            long startSw = this.sw.ElapsedMilliseconds;

//            // While we have enough time and we didn't find a win already
//            while (this.sw.ElapsedMilliseconds < this.maxTime && scores.Max() < this.evalBound && scores.Max() > -this.evalBound)
//            {
//                // Get entry, to order the moves
//                entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));

//                // Get all possible moves and order
//                moves = orderMoves(moves, entry.bestMove, scores);

//                // Reset killer moves
//                this.killerMoves = new Move[searchDepth];

//                // Update scores
//                scores = searchOneIteration(B, moves, searchDepth, playerOne, playerTwo);

//                // Print time per iteration
//                if (this.printIterations)
//                {
//                    // Print performance
//                    Console.WriteLine("Depth " + (searchDepth + 1) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//                }

//                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//                if (this.sw.ElapsedMilliseconds < this.maxTime)
//                {
//                    oldScores = (int[])scores.Clone();
//                    actualDepth = searchDepth;
//                    nrOfNodes = this.seenNodes;
//                }

//                //Console.WriteLine("[{0}]", string.Join(", ", oldScores));

//                // Look at next depth
//                searchDepth++;
//            }

//            // Stop Stopwatch
//            this.sw.Stop();
//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth + 1) +
//                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move (based on last iteration)
//            B.movePiece(moves[oldScores.argMax()], print);
//        }
//        // Else set no legal moves
//        else
//            this.NoLegalMoves();

//    }

//    int[] searchOneIteration(Board B, List<Move> moves, int searchDepth, Player playerOne, Player playerTwo)
//    {
//        // Determine best move
//        int[] scores = new int[moves.Count()];
//        for (int i = 0; i < moves.Count(); i++)
//        {
//            // Check if to position is town
//            bool isTown = false;
//            if ((moves[i].type == Move.moveType.shoot || moves[i].type == Move.moveType.soldierCapture) &&
//                B.getSpaces()[moves[i].To.x, moves[i].To.y].getPieceType() == Piece.epieceType.town)
//                isTown = true;

//            // Move the piece (and update hash)
//            this.currentHash = this.zH.makeMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//            B.movePiece(moves[i], false, false)

//            // Update Cannons
//            B.updateCannons();

//            // Switch player (and update hash)
//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.switchPlayer(playerOne, playerTwo);

//            // Get score
//            scores[i] = -NegaMaxAlphaBetaTTKMSearch(B, searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, false);

//            // Switch player (and update hash)
//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.switchPlayer(playerOne, playerTwo);

//            // Undo Move (and update hash)
//            this.currentHash = this.zH.undoMoveHash(this.currentHash, moves[i], this.playerId, isTown);
//            B.UndoMove(moves[i]);

//            // No update needed on cannons, will do it before determining the score -> Saves time

//            if (this.sw.ElapsedMilliseconds > this.maxTime)
//                break;
//        }

//        return scores;
//    }

//    int NegaMaxAlphaBetaTTKMSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;
//        }

//        // Alpha beta - With addition of keeping track of hashvalue
//        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

//        if (!placeTown && !B.TownsInGame())
//        {
//            this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color;
//        } // Terminal state
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth);
//        int bestValue = -this.evalBound * 10;
//        int bestValueIndex = -1;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Check if to position is town (when captured or shoot)
//                bool isTown = false;
//                if ((possibleMoves[i].type == Move.moveType.shoot || possibleMoves[i].type == Move.moveType.soldierCapture) &&
//                    B.getSpaces()[possibleMoves[i].To.x, possibleMoves[i].To.y].getPieceType() == Piece.epieceType.town)
//                    isTown = true;

//                // Make Move (and update hash)
//                this.currentHash = this.zH.makeMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                int value = -NegaMaxAlphaBetaTTKMSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//                // Compare with best value
//                if (value > bestValue)
//                {
//                    bestValue = value;
//                    bestValueIndex = i;
//                }

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move (and update hash)
//                this.currentHash = this.zH.undoMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.UndoMove(possibleMoves[i]);

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                if (Alpha >= Beta)
//                {
//                    this.killerMoves[depth-1] = possibleMoves[i];
//                    break;
//                }

//                if (this.sw.ElapsedMilliseconds > this.maxTime)
//                    break;
//            }

//            // Store value in transposition table
//            TTEntry.flag flagType;
//            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//            else { flagType = TTEntry.flag.exact; }

//            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
//                depth, this.zH.getHashValue(this.currentHash));

//            // Return value
//            return bestValue;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Create Initial Hash
//        this.currentHash = this.zH.generateBoardHash(B);

//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        // Initialise scores
//        int[] scores = new int[placements.Count()];
//        int[] oldScores = new int[placements.Count()];

//        int searchDepth = 0;
//        while (this.sw.ElapsedMilliseconds < this.maxTime)
//        {
//            placements = orderPlacements(placements, scores);

//            // Reset killer moves
//            this.killerMoves = new Move[searchDepth];

//            scores = searchOneIterationTown(B, placements, searchDepth, playerOne, playerTwo);

//            //Console.WriteLine("[{0}]", string.Join(", ", scores));

//            // Print time per iteration
//            if (this.printIterations)
//            {
//                // Print performance
//                Console.WriteLine("Depth " + (searchDepth + 1) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//            }

//            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//            if (this.sw.ElapsedMilliseconds < this.maxTime)
//                oldScores = (int[])scores.Clone();

//            searchDepth++;
//        }

//        // Stop the stopwatch
//        this.sw.Stop();

//        // Place town
//        B.placeTown(placements[oldScores.argMax()], print);
//    }

//    int[] searchOneIterationTown(Board B, List<Coord> placements, int searchDepth, Player playerOne, Player playerTwo)
//    {
//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // Switch player
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            scores[i] = -NegaMaxAlphaBetaTTKMSearch(B, searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            // Switch back
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            // Remove cannon
//            B.removeTown(placements[i]);

//            if (this.sw.ElapsedMilliseconds > this.maxTime)
//                break;
//        }
        
//        return scores;
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}


/// Attempt 2
//internal class NegaMax2 : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;

//    // Depth = 1, Nodes evaluated: 42. In 7 [ms].
//    // Depth = 2, Nodes evaluated: 1782. In 19 [ms].
//    // Depth = 3, Nodes evaluated: 69737. In 726 [ms].
//    // Depth = 4, Nodes evaluated: 2885028. In 26914 [ms].

//    // Basic NegaMax no pruning
//    public NegaMax2(int id, int searchDepth)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.evalBound = this.getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        List<Move> moves = new List<Move>();
//        this.scores = new int[moves.Count()];

//        this.seenNodes = 0;

//        if (moves.Count() > 0)
//        {
//            // Determine scores
//            NegaMaxSearch(B, this.searchDepth, playerOne, playerTwo, 1, false);

//            // Stop stopwatch
//            sw.Stop();

//            //Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[this.scores.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxSearch(Board B, int depth, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
//        int value = -this.evalBound;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Make Move
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                value = Math.Max(value, -NegaMaxSearch(B, depth - 1, playerOne, playerTwo, color * -1, placeTown));

//                // If it is at our search depth, and isn't during placement, add score to list
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[i] = value;
//                }

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(possibleMoves[i]);
//            }

//            return value;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player
//            B.switchPlayer(playerOne, playerTwo);

//            scores[i] = -NegaMaxSearch(B, this.searchDepth - 1, playerOne, playerTwo, -1, true);

//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }
//}

//internal class NegaMaxAlphaBeta2 : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;

//    // Basic NegaMax with alpha beta pruning
//    public NegaMaxAlphaBeta2(int id, int searchDepth)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.evalBound = this.getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        List<Move> moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
//        this.scores = new int[moves.Count()];

//        this.seenNodes = 0;

//        if (moves.Count() > 0)
//        {
//            Console.WriteLine(this.scores.toPrint());
//            // Determine scores
//            NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//            // Stop stopwatch
//            sw.Stop();

//            //Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            Console.WriteLine(this.scores.toPrint());

//            // Make best move
//            B.movePiece(moves[this.scores.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        List<Move> possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId()).orderByMoveType();
//        int value = -this.evalBound;

//        if (possibleMoves.Count() > 0)
//        {
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Make Move
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                value = Math.Max(value, -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown));

//                // If it is at our search depth, and isn't during placement, add score to list
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[i] = value;
//                }

//                // Switch player
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move
//                B.UndoMove(possibleMoves[i]);

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                // Prune when possible
//                if (Alpha >= Beta)
//                    break;
//            }

//            return value;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player
//            B.switchPlayer(playerOne, playerTwo);

//            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }
//}

//internal class NegaMaxAlphaBetaTT2 : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;

//    // Basic NegaMax with alpha beta pruning and TT
//    public NegaMaxAlphaBetaTT2(int id, int searchDepth, int lengthHashKey)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        List<Move> moves = orderMoves(B.getPossibleMoves(this.playerId), entry.bestMove);
//        this.scores = new int[moves.Count()];

//        this.seenNodes = 0;

//        if (moves.Count() > 0)
//        {
//            // Determine scores
//            NegaMaxAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//            Console.WriteLine(this.scores.toPrint());

//            // Stop stopwatch
//            sw.Stop();

//            //Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[this.scores.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.upperBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;
//        }

//        // Normal Alpha Beta Search
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        // Get all possible moves
//        List<Move> possibleMoves = orderMoves(B.getPossibleMoves(B.getCurrentPlayer().getPlayerId()), entry.bestMove);

//        if (possibleMoves.Count() > 0)
//        {
//            // Initialise for search
//            int currentPlayerId = B.getCurrentPlayer().getPlayerId();
//            int bestValue = -this.evalBound - 1;
//            int bestValueIndex = -1;
//            for (int i = 0; i < possibleMoves.Count(); i++)
//            {
//                // Check if to position is town (when captured or shoot)
//                bool isTown = false;
//                if ((possibleMoves[i].type == Move.moveType.shoot || possibleMoves[i].type == Move.moveType.soldierCapture) &&
//                    B.getSpaces()[possibleMoves[i].To.x, possibleMoves[i].To.y].getPieceType() == Piece.epieceType.town)
//                    isTown = true;

//                // Make Move (and update hash)
//                this.currentHash = this.zH.makeMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.movePiece(possibleMoves[i], false);

//                // Update Cannons
//                B.updateCannons();

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Get value
//                int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//                // Compare with best value
//                if (value > bestValue)
//                {
//                    bestValue = value;
//                    bestValueIndex = i;
//                }

//                // Switch player (and update hash)
//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.switchPlayer(playerOne, playerTwo);

//                // Undo Move (and update hash)
//                this.currentHash = this.zH.undoMoveHash(this.currentHash, possibleMoves[i], currentPlayerId, isTown);
//                B.UndoMove(possibleMoves[i]);

//                // If it is at our search depth, and isn't during placement, add score to list
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[i] = value;
//                }

//                // Calculate new Alpha
//                Alpha = Math.Max(value, Alpha);

//                // Prune when possible
//                if (Alpha >= Beta)
//                    break;
//            }

//            // Store entry in TT
//            TTEntry.flag flagType;
//            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//            else { flagType = TTEntry.flag.exact; }

//            // Set entry
//            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
//                depth, this.zH.getHashValue(this.currentHash));

//            // Return value
//            return bestValue;
//        }
//        else
//        {
//            return -this.evalBound; // No color, looking from current perspective
//        }
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Create Initial Hash
//        this.currentHash = this.zH.generateBoardHash(B);

//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class NegaMaxAlphaBetaTT3 : Player
//{
//    int searchDepth;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;

//    // Basic NegaMax with alpha beta pruning and TT (skips all moves if TT resulted in Pruning)
//    public NegaMaxAlphaBetaTT3(int id, int searchDepth, int lengthHashKey)
//    {
//        this.playerId = id;
//        this.searchDepth = searchDepth;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        List<Move> moves = orderMoves(B.getPossibleMoves(this.playerId), entry.bestMove);
//        this.scores = new int[moves.Count()];

//        this.seenNodes = 0;

//        if (moves.Count() > 0)
//        {
//            // Determine scores
//            NegaMaxAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//            //Console.WriteLine(this.scores.toPrint());

//            // Stop stopwatch
//            sw.Stop();

//            //Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + this.seenNodes + ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(moves[this.scores.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Initialise
//        bool skipAllMoves = false;
//        int bestValue = -this.evalBound-1;
//        Move bestMove = default(Move);
//        int startLoop = 0;

//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            // Check if a value can be returned. (based on value of table)
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.upperBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;

//           // Else, make the TT move first (can it already been pruned) ? (iff depth != 0)
//           if (depth > 0)
//            {
//                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
//               playerOne, playerTwo, color, placeTown);

//                // We don't need to loop over this value anymore (within our moves)
//                startLoop++;

//                // Save value
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[0] = value;
//                }

//                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
//                Alpha = Math.Max(Alpha, value);
//                if (Alpha >= Beta)
//                {
//                    skipAllMoves = true;
//                    bestValue = value;
//                    bestMove = entry.bestMove;
//                }
//            }
//        }

//        // Terminal nodes?
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
//        if (!skipAllMoves)
//        {
//            // Get all possible moves
//            List<Move> possibleMoves = orderMoves(B.getPossibleMoves(B.getCurrentPlayer().getPlayerId()), entry.bestMove);

//            if (possibleMoves.Count() > 0)
//            {
//                // Initialise for search
//                int currentPlayerId = B.getCurrentPlayer().getPlayerId();
//                for (int i = startLoop; i < possibleMoves.Count(); i++)
//                {
//                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
//                    int value = simulateMove(B, depth, Alpha, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

//                    // Compare with best value
//                    if (value > bestValue)
//                    {
//                        bestValue = value;
//                        bestMove = possibleMoves[i];
//                    }

//                    // If it is at our search depth, and isn't during placement, add score to list
//                    if (!placeTown && depth == this.searchDepth)
//                    {
//                        this.scores[i] = value;
//                    }

//                    // Calculate new Alpha
//                    Alpha = Math.Max(value, Alpha);

//                    // Prune when possible
//                    if (Alpha >= Beta)
//                        break;
//                }
//            }
//            else
//            {
//                return -this.evalBound; // No color, looking from current perspective - terminal node
//            }
//        }

//        // Store entry in TT
//        TTEntry.flag flagType;
//        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//        else { flagType = TTEntry.flag.exact; }

//        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
//            depth, this.zH.getHashValue(this.currentHash));

//        // Return value
//        return bestValue;
//    }

//    int simulateMove(Board B, int depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Check if to position is town (when captured or shoot)
//        bool isTown = false;
//        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
//            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
//            isTown = true;

//        // Make Move (and update hash)
//        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.movePiece(move, false, placeTown);

//        // Update Cannons
//        B.updateCannons();

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Get value
//        int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Undo Move (and update hash)
//        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.UndoMove(move, placeTown);

//        return value;
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Create Initial Hash
//        this.currentHash = this.zH.generateBoardHash(B);

//        // Get placements
//        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        int[] scores = new int[placements.Count()];
//        // Determine placement
//        for (int i = 0; i < placements.Count(); i++)
//        {
//            B.placeTown(placements[i], false);

//            // Update cannons
//            B.updateCannons();

//            // switch player
//            B.switchPlayer(playerOne, playerTwo);
//            this.currentHash = this.zH.switchPlayer(this.currentHash);

//            scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//            this.currentHash = this.zH.switchPlayer(this.currentHash);
//            B.removeTown(placements[i]);
//        }

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class IterativeDeepeningSimple : Player
//{
//    int maxTime;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    int searchDepth;
//    bool printIterations;
//    List<Move> moves = new List<Move>();

//    // Iterative Deepening, without killermoves
//    public IterativeDeepeningSimple(int id, int maxSearchTimeMs, int lengthHashKey, bool printIterations)
//    {
//        this.playerId = id;
//        this.maxTime = maxSearchTimeMs;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//        this.printIterations = printIterations;
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        this.moves = B.getPossibleMoves(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        if (this.moves.Count() > 0)
//        {
//            // Initialise
//            int nrOfNodes = 0;
//            int actualDepth = 0;
//            this.seenNodes = 0;
//            this.searchDepth = 1;
//            this.scores = new int[this.moves.Count()];
//            int[] finishedScore = new int[this.moves.Count()];
//            List<Move> finishedMoves = new List<Move>();

//            while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//            {
//                // Entry will change automatically (reference type)

//                // Order moves (based on previous scores)
//                this.moves = orderMoves(this.moves, entry.bestMove, this.scores);

//                // Reset the scores
//                this.scores = new int[this.moves.Count()];

//                // Determine scores
//                NegaMaxAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//                // Print time per iteration
//                if (this.printIterations)
//                {
//                    // Print performance
//                    Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//                }

//                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//                if (this.sw.ElapsedMilliseconds < this.maxTime)
//                {
//                    finishedScore = (int[])this.scores.Clone();
//                    finishedMoves = this.moves;
//                    actualDepth = searchDepth;
//                    nrOfNodes = this.seenNodes;

//                    //Console.WriteLine("Best move: " + finishedMoves[finishedScore.argMax()].toPrint() + ", position : " +
//                    //    finishedScore.argMax());
//                    //Console.WriteLine();
//                    //Console.WriteLine(finishedScore.toPrint());
//                }

//                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//                this.searchDepth++;
//                B.updateCannons();
//            }
            

//            //Console.WriteLine(this.scores.toPrint());

//            // Stop stopwatch
//            sw.Stop();

//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            //if (finishedScore.Max() == this.evalBound)
//            //{
//            //    Console.WriteLine(finishedScore.toPrint() + " -> " + finishedScore.argMax());

//            //    for (int i = 0; i < finishedMoves.Count(); i++)
//            //    {
//            //        Console.Write(finishedMoves[i].toPrint() + ", ");
//            //    }
//            //    Console.WriteLine();
//            //    for (int i = 0; i < testMoves.Count(); i++)
//            //    {
//            //        Console.Write(testMoves[i].toPrint() + ", ");
//            //    }
//            //}

//            // Make best move
//            B.movePiece(finishedMoves[finishedScore.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Initialise
//        bool skipAllMoves = false;
//        int bestValue = -this.evalBound - 1;
//        Move bestMove = default(Move);
//        int startLoop = 0;

//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            // Check if a value can be returned. (based on value of table)
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.upperBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;

//            // Else, make the TT move first (can it already been pruned ?) (iff depth != 0)
//            if (depth > 0)
//            {
//                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
//                playerOne, playerTwo, color, placeTown);

//                // We don't need to loop over this value anymore (within our moves)
//                startLoop++;

//                // Save value
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[0] = value;
//                }

//                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
//                Alpha = Math.Max(Alpha, value);
//                if (Alpha >= Beta)
//                {
//                    skipAllMoves = true;
//                    bestValue = value;
//                    bestMove = entry.bestMove;
//                }
//            }
//        }

//        // Terminal nodes?
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
//        if (!skipAllMoves)
//        {
//            // Get all possible moves
//            List<Move> possibleMoves;
            
//            // If it is the first depth, take over the sorted moves (unless we are looking at the town placement)
//            if (!placeTown && depth == this.searchDepth)
//            {
//                possibleMoves = this.moves;
//            }
//            // Else sort them on TT and knowledge
//            else
//            {
//                possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
//                possibleMoves = orderMoves(possibleMoves, entry.bestMove);
//            }
                
//            if (possibleMoves.Count() > 0)
//            {
//                // Initialise for search
//                int currentPlayerId = B.getCurrentPlayer().getPlayerId();
//                for (int i = startLoop; i < possibleMoves.Count(); i++)
//                {
//                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
//                    int value = simulateMove(B, depth, Alpha, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

//                    // Compare with best value
//                    if (value > bestValue)
//                    {
//                        bestValue = value;
//                        bestMove = possibleMoves[i];
//                    }

//                    // If it is at our search depth, and isn't during placement, add score to list
//                    if (!placeTown && depth == this.searchDepth)
//                    {
//                        this.scores[i] = value;
//                    }

//                    // Calculate new Alpha
//                    Alpha = Math.Max(value, Alpha);

//                    // Prune when possible
//                    if (Alpha >= Beta)
//                        break;

//                    if (this.sw.ElapsedMilliseconds > this.maxTime)
//                        break;
//                }
//            }
//            else
//            {
//                return -this.evalBound; // No color, looking from current perspective - terminal node
//            }
//        }

//        // Store entry in TT
//        TTEntry.flag flagType;
//        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//        else { flagType = TTEntry.flag.exact; }

//        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
//            depth, this.zH.getHashValue(this.currentHash));

//        // Return value
//        return bestValue;
//    }

//    int simulateMove(Board B, int depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Check if to position is town (when captured or shoot)
//        bool isTown = false;
//        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
//            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
//            isTown = true;

//        // Make Move (and update hash)
//        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.movePiece(move, false, placeTown);

//        // Update Cannons
//        B.updateCannons();

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Get value
//        int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Undo Move (and update hash)
//        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.UndoMove(move, placeTown);

//        return value;
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        List<Coord> placements = B.getPossiblePlacements(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        // Initialise
//        int nrOfNodes = 0;
//        int actualDepth = 0;
//        this.seenNodes = 0;
//        this.searchDepth = 1;
//        int[] scores = new int[placements.Count()];
//        int[] finishedScore = new int[placements.Count()];
//        List<Coord> finishedMoves = new List<Coord>();

//        while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//        {
//            // Entry will change automatically (reference type)

//            // Order moves (based on previous scores)
//            placements = orderPlacements(placements, scores);

//            // Determine scores
//            scores = new int[placements.Count()];
//            // Determine placement
//            for (int i = 0; i < placements.Count(); i++)
//            {
//                B.placeTown(placements[i], false);

//                // Update cannons
//                B.updateCannons();

//                // switch player
//                B.switchPlayer(playerOne, playerTwo);
//                this.currentHash = this.zH.switchPlayer(this.currentHash);

//                scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.removeTown(placements[i]);
//            }

//            // Print time per iteration
//            if (this.printIterations)
//            {
//                // Print performance
//                Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//            }

//            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//            if (this.sw.ElapsedMilliseconds < this.maxTime)
//            {
//                finishedScore = (int[])scores.Clone();
//                finishedMoves = placements;
//                actualDepth = searchDepth;
//                nrOfNodes = this.seenNodes;

//                //Console.WriteLine("Best move: " + finishedMoves[finishedScore.argMax()].toPrint() + ", position : " +
//                //    finishedScore.argMax());
//                //Console.WriteLine();
//                //Console.WriteLine(finishedScore.toPrint());
//            }

//            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//            this.searchDepth++;
//            B.updateCannons();
//        }


//        //Console.WriteLine(this.scores.toPrint());

//        // Stop stopwatch
//        sw.Stop();

//        // Print nodes evaluated
//        Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//            ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);

//        //if (finishedScore.Max() == this.evalBound)
//        //{
//        //    Console.WriteLine(finishedScore.toPrint() + " -> " + finishedScore.argMax());

//        //    for (int i = 0; i < finishedMoves.Count(); i++)
//        //    {
//        //        Console.Write(finishedMoves[i].toPrint() + ", ");
//        //    }
//        //    Console.WriteLine();
//        //    for (int i = 0; i < testMoves.Count(); i++)
//        //    {
//        //        Console.Write(testMoves[i].toPrint() + ", ");
//        //    }
//        //}

//        // Old code
//        //// Make best move
//        //B.movePiece(finishedMoves[finishedScore.argMax()], print);

//        //// Create Initial Hash
//        //this.currentHash = this.zH.generateBoardHash(B);

//        //// Get placements
//        //List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

//        //int[] scores = new int[placements.Count()];
//        //// Determine placement
//        //for (int i = 0; i < placements.Count(); i++)
//        //{
//        //    B.placeTown(placements[i], false);

//        //    // Update cannons
//        //    B.updateCannons();

//        //    // switch player
//        //    B.switchPlayer(playerOne, playerTwo);
//        //    this.currentHash = this.zH.switchPlayer(this.currentHash);

//        //    scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//        //    this.currentHash = this.zH.switchPlayer(this.currentHash);
//        //    B.removeTown(placements[i]);
//        //}
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class IterativeDeepening2 : Player
//{
//    int maxTime;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    int searchDepth;
//    bool printIterations;
//    List<Move> moves = new List<Move>();
//    Move[] killerMoves; // depth, 

//    // Iterative Deepening with killer moves
//    public IterativeDeepening2(int id, int maxSearchTimeMs, int lengthHashKey, bool printIterations)
//    {
//        this.playerId = id;
//        this.maxTime = maxSearchTimeMs;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//        this.printIterations = printIterations;
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        this.moves = B.getPossibleMoves(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        if (this.moves.Count() > 0)
//        {
//            // Initialise
//            int nrOfNodes = 0;
//            int actualDepth = 0;
//            this.seenNodes = 0;
//            this.searchDepth = 1;
//            this.scores = new int[this.moves.Count()];
//            int[] finishedScore = new int[this.moves.Count()];
//            List<Move> finishedMoves = new List<Move>();

//            while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//            {
//                // Entry will change automatically (reference type)

//                // Order moves (based on previous scores)
//                this.moves = orderMoves(this.moves, entry.bestMove, this.scores);

//                // Reset the scores
//                this.scores = new int[this.moves.Count()];

//                // Reset killer moves
//                this.killerMoves = new Move[searchDepth];

//                // Determine scores
//                NegaMaxAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//                // Print time per iteration
//                if (this.printIterations)
//                {
//                    // Print performance
//                    Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//                }

//                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//                if (this.sw.ElapsedMilliseconds < this.maxTime)
//                {
//                    finishedScore = (int[])this.scores.Clone();
//                    finishedMoves = this.moves;
//                    actualDepth = searchDepth;
//                    nrOfNodes = this.seenNodes;
//                }

//                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//                this.searchDepth++;
//                B.updateCannons();
//            }


//            //Console.WriteLine(this.scores.toPrint());

//            // Stop stopwatch
//            sw.Stop();

//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            B.movePiece(finishedMoves[finishedScore.argMax()], print);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Initialise
//        bool skipAllMoves = false;
//        int bestValue = -this.evalBound - 1;
//        Move bestMove = default(Move);
//        int startLoop = 0;

//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            // Check if a value can be returned. (based on value of table)
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.upperBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;

//            // Else, make the TT move first (can it already been pruned ?) (iff depth != 0)
//            if (depth > 0)
//            {
//                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
//                playerOne, playerTwo, color, placeTown);

//                // We don't need to loop over this value anymore (within our moves)
//                startLoop++;

//                // Save value
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[0] = value;
//                }

//                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
//                Alpha = Math.Max(Alpha, value);
//                if (Alpha >= Beta)
//                {
//                    skipAllMoves = true;
//                    bestValue = value;
//                    bestMove = entry.bestMove;
//                }
//            }
//        }

//        // Terminal nodes?
//        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, new int[] { 0 }) * color; }

//        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
//        if (!skipAllMoves)
//        {
//            // Get all possible moves
//            List<Move> possibleMoves;

//            // If it is the first depth, take over the sorted moves (unless we are looking at the town placement)
//            if (!placeTown && depth == this.searchDepth)
//            {
//                possibleMoves = this.moves;
//            }
//            // Else sort them on TT and knowledge
//            else
//            {
//                possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
//                possibleMoves = orderMoves(possibleMoves, entry.bestMove, this.killerMoves, depth);
//            }

//            if (possibleMoves.Count() > 0)
//            {
//                // Initialise for search
//                int currentPlayerId = B.getCurrentPlayer().getPlayerId();
//                for (int i = startLoop; i < possibleMoves.Count(); i++)
//                {
//                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
//                    int value = simulateMove(B, depth, Alpha, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

//                    // Compare with best value
//                    if (value > bestValue)
//                    {
//                        bestValue = value;
//                        bestMove = possibleMoves[i];
//                    }

//                    // If it is at our search depth, and isn't during placement, add score to list
//                    if (!placeTown && depth == this.searchDepth)
//                    {
//                        this.scores[i] = value;
//                    }

//                    // Calculate new Alpha
//                    Alpha = Math.Max(value, Alpha);

//                    // Prune when possible
//                    if (Alpha >= Beta)
//                    {
//                        this.killerMoves[depth-1] = bestMove;
//                        break;
//                    }

//                    if (this.sw.ElapsedMilliseconds > this.maxTime)
//                        break;
//                }
//            }
//            else
//            {
//                return -this.evalBound; // No color, looking from current perspective - terminal node
//            }
//        }

//        // Store entry in TT
//        TTEntry.flag flagType;
//        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//        else { flagType = TTEntry.flag.exact; }

//        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
//            depth, this.zH.getHashValue(this.currentHash));

//        // Return value
//        return bestValue;
//    }

//    int simulateMove(Board B, int depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Check if to position is town (when captured or shoot)
//        bool isTown = false;
//        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
//            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
//            isTown = true;

//        // Make Move (and update hash)
//        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.movePiece(move, false, placeTown);

//        // Update Cannons
//        B.updateCannons();

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Get value
//        int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Undo Move (and update hash)
//        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.UndoMove(move, placeTown);

//        return value;
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        List<Coord> placements = B.getPossiblePlacements(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        // Initialise
//        int nrOfNodes = 0;
//        int actualDepth = 0;
//        this.seenNodes = 0;
//        this.searchDepth = 1;
//        int[] scores = new int[placements.Count()];
//        int[] finishedScore = new int[placements.Count()];
//        List<Coord> finishedMoves = new List<Coord>();

//        while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//        {
//            // Entry will change automatically (reference type)

//            // Order moves (based on previous scores)
//            placements = orderPlacements(placements, scores);

//            // Determine scores
//            scores = new int[placements.Count()];

//            // Reset killer moves
//            this.killerMoves = new Move[searchDepth-1];

//            // Determine placement
//            for (int i = 0; i < placements.Count(); i++)
//            {
//                B.placeTown(placements[i], false);

//                // Update cannons
//                B.updateCannons();

//                // switch player
//                B.switchPlayer(playerOne, playerTwo);
//                this.currentHash = this.zH.switchPlayer(this.currentHash);

//                scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.removeTown(placements[i]);
//            }

//            // Print time per iteration
//            if (this.printIterations)
//            {
//                // Print performance
//                Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//            }

//            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//            if (this.sw.ElapsedMilliseconds < this.maxTime)
//            {
//                finishedScore = (int[])scores.Clone();
//                finishedMoves = placements;
//                actualDepth = searchDepth;
//                nrOfNodes = this.seenNodes;
//            }

//            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//            this.searchDepth++;
//            B.updateCannons();
//        }


//        //Console.WriteLine(this.scores.toPrint());

//        // Stop stopwatch
//        sw.Stop();

//        // Print nodes evaluated
//        Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//            ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//        // Place town
//        B.placeTown(placements[scores.argMax()], print);
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

//internal class IterativeDeepeningHH : Player
//{
//    int maxTime;
//    int seenNodes = 0;
//    Stopwatch sw = new Stopwatch();
//    int evalBound;
//    int[] scores;
//    ZobristHashing zH;
//    TranspositionTable TT;
//    ulong currentHash;
//    int searchDepth;
//    bool printIterations;
//    List<Move> moves = new List<Move>();
//    Move[] killerMoves; // depth,
//    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

//    // Iterative Deepening with killermoves and history heuristics
//    public IterativeDeepeningHH(int id, int maxSearchTimeMs, int lengthHashKey, bool printIterations)
//    {
//        this.playerId = id;
//        this.maxTime = maxSearchTimeMs;
//        this.zH = new ZobristHashing(lengthHashKey);
//        this.TT = new TranspositionTable(lengthHashKey);
//        this.evalBound = getBoundsEval(new int[] { 0 });
//        this.printIterations = printIterations;
//    }

//    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        this.moves = B.getPossibleMoves(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        if (this.moves.Count() > 0)
//        {
//            // Initialise
//            int nrOfNodes = 0;
//            int actualDepth = 0;
//            this.seenNodes = 0;
//            this.searchDepth = 1;
//            this.scores = new int[this.moves.Count()];
//            int[] finishedScore = new int[this.moves.Count()];
//            List<Move> finishedMoves = new List<Move>();

//            while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//            {
//                // Entry will change automatically (reference type)

//                // Order moves (based on previous scores)
//                this.moves = orderMoves(this.moves, entry.bestMove, this.scores);

//                // Reset the scores
//                this.scores = new int[this.moves.Count()];

//                // Reset killer moves
//                this.killerMoves = new Move[searchDepth];

//                // Determine scores
//                NegaMaxAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

//                // Print time per iteration
//                if (this.printIterations)
//                {
//                    // Print performance
//                    Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//                }

//                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//                if (this.sw.ElapsedMilliseconds < this.maxTime)
//                {
//                    finishedScore = (int[])this.scores.Clone();
//                    finishedMoves = this.moves;
//                    actualDepth = searchDepth;
//                    nrOfNodes = this.seenNodes;
//                }

//                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//                this.searchDepth++;
//                B.updateCannons();
//            }


//            //Console.WriteLine(this.scores.toPrint());

//            // Stop stopwatch
//            sw.Stop();

//            // Print nodes evaluated
//            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//            // Make best move
//            Move bestMove = finishedMoves[finishedScore.argMax()];
//            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
//            this.historyHeuristic.multiplyDiscount();
//            B.movePiece(bestMove, print, false);
//        }
//        else
//        {
//            this.NoLegalMoves();
//        }
//    }

//    int NegaMaxAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        this.seenNodes++;
//        // Initialise
//        bool skipAllMoves = false;
//        int bestValue = -this.evalBound - 1;
//        Move bestMove = default(Move);
//        int startLoop = 0;

//        // Transposition Table look up
//        int olda = Alpha;
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        // If position is new, depth = -1 (initial value of TTEntry)
//        if (entry.depth >= depth)
//        {
//            // Check if a value can be returned. (based on value of table)
//            if (entry.type == TTEntry.flag.exact)
//                return entry.value;
//            else if (entry.type == TTEntry.flag.lowerBound)
//                Alpha = Math.Max(Alpha, entry.value);
//            else if (entry.type == TTEntry.flag.upperBound)
//                Beta = Math.Min(Beta, entry.value);
//            if (Alpha >= Beta)
//                return entry.value;

//            // Else, make the TT move first (can it already been pruned ?) (iff depth != 0)
//            if (depth > 0)
//            {
//                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
//                playerOne, playerTwo, color, placeTown);

//                // We don't need to loop over this value anymore (within our moves)
//                startLoop++;

//                // Save value
//                if (!placeTown && depth == this.searchDepth)
//                {
//                    this.scores[0] = value;
//                }

//                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
//                Alpha = Math.Max(Alpha, value);
//                if (Alpha >= Beta)
//                {
//                    skipAllMoves = true;
//                    bestValue = value;
//                    bestMove = entry.bestMove;
//                }
//            }
//        }

//        // Terminal nodes?
//        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
//        if (depth == 0) { return this.Evaluate(B, new int[] { 0 }) * color; }

//        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
//        if (!skipAllMoves)
//        {
//            // Get all possible moves
//            List<Move> possibleMoves;

//            // If it is the first depth, take over the sorted moves (unless we are looking at the town placement)
//            if (!placeTown && depth == this.searchDepth)
//            {
//                possibleMoves = this.moves;
//            }
//            // Else sort them on TT and knowledge
//            else
//            {
//                possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
//                possibleMoves = orderMoves(possibleMoves, entry.bestMove, this.killerMoves, depth, this.historyHeuristic);
//            }

//            if (possibleMoves.Count() > 0)
//            {
//                // Initialise for search
//                int currentPlayerId = B.getCurrentPlayer().getPlayerId();
//                for (int i = startLoop; i < possibleMoves.Count(); i++)
//                {
//                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
//                    int value = simulateMove(B, depth, Alpha, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

//                    // Compare with best value
//                    if (value > bestValue)
//                    {
//                        bestValue = value;
//                        bestMove = possibleMoves[i];
//                    }

//                    // If it is at our search depth, and isn't during placement, add score to list
//                    if (!placeTown && depth == this.searchDepth)
//                    {
//                        this.scores[i] = value;
//                    }

//                    // Calculate new Alpha
//                    Alpha = Math.Max(value, Alpha);

//                    // Prune when possible
//                    if (Alpha >= Beta)
//                    {
//                        this.killerMoves[depth - 1] = bestMove;
//                        this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
//                        break;
//                    }

//                    if (this.sw.ElapsedMilliseconds > this.maxTime)
//                        break;
//                }
//            }
//            else
//            {
//                return -this.evalBound; // No color, looking from current perspective - terminal node
//            }
//        }

//        // Store entry in TT
//        TTEntry.flag flagType;
//        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
//        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
//        else { flagType = TTEntry.flag.exact; }

//        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
//            depth, this.zH.getHashValue(this.currentHash));

//        // Return value
//        return bestValue;
//    }

//    int simulateMove(Board B, int depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
//    {
//        // Check if to position is town (when captured or shoot)
//        bool isTown = false;
//        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
//            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
//            isTown = true;

//        // Make Move (and update hash)
//        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.movePiece(move, false, placeTown);

//        // Update Cannons
//        B.updateCannons();

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Get value
//        int value = -NegaMaxAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

//        // Switch player (and update hash)
//        this.currentHash = this.zH.switchPlayer(this.currentHash);
//        B.switchPlayer(playerOne, playerTwo);

//        // Undo Move (and update hash)
//        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
//        B.UndoMove(move, placeTown);

//        return value;
//    }

//    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
//    {
//        // Start sw
//        sw.Restart(); sw.Start();

//        // Get moves and entry (entry = reference type, so changes automatically)
//        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
//        List<Coord> placements = B.getPossiblePlacements(this.playerId);

//        //List<int[]> testMoves = this.moves;

//        // Initialise
//        int nrOfNodes = 0;
//        int actualDepth = 0;
//        this.seenNodes = 0;
//        this.searchDepth = 1;
//        int[] scores = new int[placements.Count()];
//        int[] finishedScore = new int[placements.Count()];
//        List<Coord> finishedMoves = new List<Coord>();

//        while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
//        {
//            // Entry will change automatically (reference type)

//            // Order moves (based on previous scores)
//            placements = orderPlacements(placements, scores);

//            // Determine scores
//            scores = new int[placements.Count()];

//            // Reset killer moves
//            this.killerMoves = new Move[searchDepth - 1];

//            // Determine placement
//            for (int i = 0; i < placements.Count(); i++)
//            {
//                B.placeTown(placements[i], false);

//                // Update cannons
//                B.updateCannons();

//                // switch player
//                B.switchPlayer(playerOne, playerTwo);
//                this.currentHash = this.zH.switchPlayer(this.currentHash);

//                scores[i] = -NegaMaxAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

//                this.currentHash = this.zH.switchPlayer(this.currentHash);
//                B.removeTown(placements[i]);
//            }

//            // Print time per iteration
//            if (this.printIterations)
//            {
//                // Print performance
//                Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
//            }

//            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
//            if (this.sw.ElapsedMilliseconds < this.maxTime)
//            {
//                finishedScore = (int[])scores.Clone();
//                finishedMoves = placements;
//                actualDepth = searchDepth;
//                nrOfNodes = this.seenNodes;
//            }

//            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
//            this.searchDepth++;
//            B.updateCannons();
//        }


//        //Console.WriteLine(this.scores.toPrint());

//        // Stop stopwatch
//        sw.Stop();

//        // Print nodes evaluated
//        Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
//            ". In " + this.sw.ElapsedMilliseconds + " [ms].");

//        // Place town
//        B.placeTown(finishedMoves[scores.argMax()], print);
//    }

//    public override void resetTT()
//    {
//        this.TT.reset();
//    }
//}

////internal class IterativeDeepeningNegaScout : Player
////{
////    int maxTime;
////    int seenNodes = 0;
////    Stopwatch sw = new Stopwatch();
////    int evalBound;
////    int[] scores;
////    ZobristHashing zH;
////    TranspositionTable TT;
////    ulong currentHash;
////    int searchDepth;
////    bool printIterations;
////    List<Move> moves = new List<Move>();
////    Move[] killerMoves; // depth,
////    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

////    // Basic NegaMax with alpha beta pruning and TT (skips all moves if TT resulted in Pruning), and HH
////    public IterativeDeepeningNegaScout(int id, int maxSearchTimeMs, int lengthHashKey, bool printIterations)
////    {
////        this.playerId = id;
////        this.maxTime = maxSearchTimeMs;
////        this.zH = new ZobristHashing(lengthHashKey);
////        this.TT = new TranspositionTable(lengthHashKey);
////        this.evalBound = getBoundsEval(new int[] { 0 });
////        this.printIterations = printIterations;
////    }

////    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
////    {
////        // Start sw
////        sw.Restart(); sw.Start();

////        // Get moves and entry (entry = reference type, so changes automatically)
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        this.moves = B.getPossibleMoves(this.playerId);

////        //List<int[]> testMoves = this.moves;

////        if (this.moves.Count() > 0)
////        {
////            // Initialise
////            int nrOfNodes = 0;
////            int actualDepth = 0;
////            this.seenNodes = 0;
////            this.searchDepth = 1;
////            this.scores = new int[this.moves.Count()];
////            int[] finishedScore = new int[this.moves.Count()];
////            List<Move> finishedMoves = new List<Move>();

////            while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
////            {
////                // Entry will change automatically (reference type)

////                // Order moves (based on previous scores)
////                this.moves = orderMoves(this.moves, entry.bestMove, this.scores);

////                // Reset the scores
////                this.scores = new int[this.moves.Count()];

////                // Reset killer moves
////                this.killerMoves = new Move[searchDepth];

////                // Determine scores
////                NegaScoutAlphaBetaTTSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

////                // Print time per iteration
////                if (this.printIterations)
////                {
////                    // Print performance
////                    Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
////                }

////                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
////                if (this.sw.ElapsedMilliseconds < this.maxTime)
////                {
////                    finishedScore = (int[])this.scores.Clone();
////                    finishedMoves = this.moves;
////                    actualDepth = searchDepth;
////                    nrOfNodes = this.seenNodes;
////                }

////                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
////                this.searchDepth++;
////                B.updateCannons();
////            }


////            //Console.WriteLine(this.scores.toPrint());

////            // Stop stopwatch
////            sw.Stop();

////            // Print nodes evaluated
////            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
////                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

////            // Make best move
////            Move bestMove = finishedMoves[finishedScore.argMax()];
////            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
////            this.historyHeuristic.multiplyDiscount();
////            B.movePiece(bestMove, print, false);
////        }
////        else
////        {
////            this.NoLegalMoves();
////        }
////    }

////    int NegaScoutAlphaBetaTTSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
////    {
////        this.seenNodes++;
////        // Initialise
////        bool skipAllMoves = false;
////        int bestValue = -this.evalBound - 1;
////        Move bestMove = default(Move);
////        int startLoop = 0;

////        // Transposition Table look up
////        int olda = Alpha;
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        // If position is new, depth = -1 (initial value of TTEntry)
////        if (entry.depth >= depth)
////        {
////            // Check if a value can be returned. (based on value of table)
////            if (entry.type == TTEntry.flag.exact)
////                return entry.value;
////            else if (entry.type == TTEntry.flag.lowerBound)
////                Alpha = Math.Max(Alpha, entry.value);
////            else if (entry.type == TTEntry.flag.upperBound)
////                Beta = Math.Min(Beta, entry.value);
////            if (Alpha >= Beta)
////                return entry.value;

////            // Else, make the TT move first (can it already been pruned ?) (iff depth != 0)
////            if (depth > 0)
////            {
////                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
////                playerOne, playerTwo, color, placeTown);

////                // We don't need to loop over this value anymore (within our moves)
////                startLoop++;

////                // Save value
////                if (!placeTown && depth == this.searchDepth)
////                {
////                    this.scores[0] = value;
////                }

////                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
////                Alpha = Math.Max(Alpha, value);
////                if (Alpha >= Beta)
////                {
////                    skipAllMoves = true;
////                    bestValue = value;
////                    bestMove = entry.bestMove;
////                }
////            }
////        }

////        // Terminal nodes?
////        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
////        if (depth == 0) { return this.Evaluate(B, new int[] { 0 }) * color; }

////        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
////        if (!skipAllMoves)
////        {
////            // Get all possible moves
////            List<Move> possibleMoves;

////            // If it is the first depth, take over the sorted moves (unless we are looking at the town placement)
////            if (!placeTown && depth == this.searchDepth)
////            {
////                possibleMoves = this.moves;
////            }
////            // Else sort them on TT and knowledge
////            else
////            {
////                possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
////                possibleMoves = orderMoves(possibleMoves, entry.bestMove, this.killerMoves, depth, this.historyHeuristic);
////            }

////            if (possibleMoves.Count() > 0)
////            {
////                // Initialise for search
////                int currentPlayerId = B.getCurrentPlayer().getPlayerId();

////                // Determine bestValue
////                if (startLoop == 0)
////                {
////                    bestValue = simulateMove(B, depth, Alpha, Beta, possibleMoves[startLoop], currentPlayerId, playerOne, playerTwo, color, placeTown);
////                    bestMove = possibleMoves[0];
////                }

////                for (int i = 1; i < possibleMoves.Count(); i++)
////                {
////                    // NegaScout (update bound)
////                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

////                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
////                    int value = simulateMove(B, depth, lbound, ubound, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

////                    if (value >= ubound && value < Beta)
////                    {
////                        // Research
////                        value = simulateMove(B, depth, value, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);
////                    }

////                    // Compare with best value
////                    if (value > bestValue)
////                    {
////                        bestValue = value;
////                        bestMove = possibleMoves[i];
////                    }

////                    // If it is at our search depth, and isn't during placement, add score to list
////                    if (!placeTown && depth == this.searchDepth)
////                    {
////                        this.scores[i] = value;
////                    }

////                    // Prune when possible
////                    if (value >= Beta)
////                    {
////                        this.killerMoves[depth - 1] = bestMove;
////                        this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
////                        break;
////                    }

////                    if (this.sw.ElapsedMilliseconds > this.maxTime)
////                        break;
////                }
////            }
////            else
////            {
////                return -this.evalBound; // No color, looking from current perspective - terminal node
////            }
////        }

////        // Store entry in TT
////        TTEntry.flag flagType;
////        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
////        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
////        else { flagType = TTEntry.flag.exact; }

////        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
////            depth, this.zH.getHashValue(this.currentHash));

////        // Return value
////        return bestValue;
////    }

////    int simulateMove(Board B, int depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
////    {
////        // Check if to position is town (when captured or shoot)
////        bool isTown = false;
////        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
////            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
////            isTown = true;

////        // Make Move (and update hash)
////        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
////        B.movePiece(move, false, placeTown);

////        // Update Cannons
////        B.updateCannons();

////        // Switch player (and update hash)
////        this.currentHash = this.zH.switchPlayer(this.currentHash);
////        B.switchPlayer(playerOne, playerTwo);

////        // Get value
////        int value = -NegaScoutAlphaBetaTTSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

////        // Switch player (and update hash)
////        this.currentHash = this.zH.switchPlayer(this.currentHash);
////        B.switchPlayer(playerOne, playerTwo);

////        // Undo Move (and update hash)
////        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
////        B.UndoMove(move, placeTown);

////        return value;
////    }

////    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
////    {
////        // Start sw
////        sw.Restart(); sw.Start();

////        // Get moves and entry (entry = reference type, so changes automatically)
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        List<Coord> placements = B.getPossiblePlacements(this.playerId);

////        //List<int[]> testMoves = this.moves;

////        // Initialise
////        int nrOfNodes = 0;
////        int actualDepth = 0;
////        this.seenNodes = 0;
////        this.searchDepth = 1;
////        int[] scores = new int[placements.Count()];
////        int[] finishedScore = new int[placements.Count()];
////        List<Move> finishedMoves = new List<Move>();

////        while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
////        {
////            // Entry will change automatically (reference type)

////            // Order moves (based on previous scores)
////            placements = orderPlacements(placements, scores);

////            // Determine scores
////            scores = new int[placements.Count()];

////            // Reset killer moves
////            this.killerMoves = new Move[searchDepth - 1];

////            // Determine placement
////            for (int i = 0; i < placements.Count(); i++)
////            {
////                B.placeTown(placements[i], false);

////                // Update cannons
////                B.updateCannons();

////                // switch player
////                B.switchPlayer(playerOne, playerTwo);
////                this.currentHash = this.zH.switchPlayer(this.currentHash);

////                scores[i] = -NegaScoutAlphaBetaTTSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

////                this.currentHash = this.zH.switchPlayer(this.currentHash);
////                B.removeTown(placements[i]);
////            }

////            // Print time per iteration
////            if (this.printIterations)
////            {
////                // Print performance
////                Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
////            }

////            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
////            if (this.sw.ElapsedMilliseconds < this.maxTime)
////            {
////                finishedScore = (int[])scores.Clone();
////                finishedMoves = placements;
////                actualDepth = searchDepth;
////                nrOfNodes = this.seenNodes;
////            }

////            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
////            this.searchDepth++;
////            B.updateCannons();
////        }


////        //Console.WriteLine(this.scores.toPrint());

////        // Stop stopwatch
////        sw.Stop();

////        // Print nodes evaluated
////        Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
////            ". In " + this.sw.ElapsedMilliseconds + " [ms].");

////        // Place town
////        B.placeTown(finishedMoves[scores.argMax()], print);
////    }

////    public override void resetTT()
////    {
////        this.TT.reset();
////    }
////}

////internal class IterativeDeepeningNegaScoutVD : Player
////{
////    int maxTime;
////    int seenNodes = 0;
////    Stopwatch sw = new Stopwatch();
////    int evalBound;
////    int[] scores;
////    ZobristHashing zH;
////    TranspositionTable TT;
////    ulong currentHash;
////    int searchDepth;
////    bool printIterations;
////    List<Move> moves = new List<Move>();
////    Move[] killerMoves; // depth,
////    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
////    int R;

////    // Basic NegaMax with alpha beta pruning and TT (skips all moves if TT resulted in Pruning), and HH
////    public IterativeDeepeningNegaScoutVD(int id, int maxSearchTimeMs, int lengthHashKey, int R, bool printIterations)
////    {
////        this.playerId = id;
////        this.maxTime = maxSearchTimeMs;
////        this.zH = new ZobristHashing(lengthHashKey);
////        this.TT = new TranspositionTable(lengthHashKey);
////        this.evalBound = getBoundsEval(new int[] { 0 });
////        this.printIterations = printIterations;
////        this.R = R;
////    }

////    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
////    {
////        // Start sw
////        sw.Restart(); sw.Start();

////        // Get moves and entry (entry = reference type, so changes automatically)
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        this.moves = B.getPossibleMoves(this.playerId);

////        //List<int[]> testMoves = this.moves;

////        if (this.moves.Count() > 0)
////        {
////            // Initialise
////            int nrOfNodes = 0;
////            int actualDepth = 0;
////            this.seenNodes = 0;
////            this.searchDepth = 1;
////            this.scores = new int[this.moves.Count()];
////            this.scores.setAll(-this.evalBound);
////            int[] finishedScore = new int[this.moves.Count()];
////            List<Move> finishedMoves = new List<Move>();

////            while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
////            {
////                // Entry will change automatically (reference type)

////                // Order moves (based on previous scores)
////                this.moves = orderMoves(this.moves, entry.bestMove, this.scores);

////                // Reset the scores
////                this.scores.setAll(-this.evalBound);

////                // Reset killer moves
////                this.killerMoves = new Move[2*searchDepth];

////                // Determine scores
////                NegaScoutAlphaBetaVDSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

////                // Print time per iteration
////                if (this.printIterations)
////                {
////                    // Print performance
////                    Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds + " " + this.scores.Max() + " " +  this.scores.Min());
////                }

////                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
////                if (this.sw.ElapsedMilliseconds < this.maxTime)
////                {
////                    finishedScore = (int[])this.scores.Clone();
////                    finishedMoves = this.moves;
////                    actualDepth = searchDepth;
////                    nrOfNodes = this.seenNodes;
////                }

////                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
////                this.searchDepth++;
////                B.updateCannons();
////            }


////            //Console.WriteLine(this.scores.toPrint());

////            // Stop stopwatch
////            sw.Stop();

////            // Print nodes evaluated
////            Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
////                ". In " + this.sw.ElapsedMilliseconds + " [ms].");

////            // Make best move
////            Move bestMove = finishedMoves[finishedScore.argMax()];
////            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
////            this.historyHeuristic.multiplyDiscount();
////            B.movePiece(bestMove, print, false);
////        }
////        else
////        {
////            this.NoLegalMoves();
////        }
////    }

////    int NegaScoutAlphaBetaVDSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
////    {
////        // Perform null move
////        if (!lastNullMove && depth != this.searchDepth && depth > 0 && B.getPiecesCoords().Count() > 10)
////        {
////            this.currentHash = this.zH.switchPlayer(this.currentHash);
////            B.switchPlayer(playerOne, playerTwo);
////            int value = -NegaScoutAlphaBetaVDSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
////            B.switchPlayer(playerOne, playerTwo);
////            this.currentHash = this.zH.switchPlayer(this.currentHash);

////            if (value > Beta)
////            {
////                return value;
////            }
////        }

////        this.seenNodes++;
////        // Initialise
////        bool skipAllMoves = false;
////        int bestValue = -this.evalBound - 1;
////        Move bestMove = default(Move);
////        int startLoop = 0;

////        // Transposition Table look up
////        int olda = Alpha;
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        // If position is new, depth = -1 (initial value of TTEntry)
////        if (depth >= 0 && entry.depth >= depth)
////        {
////            // Check if a value can be returned. (based on value of table)
////            if (entry.type == TTEntry.flag.exact)
////                return entry.value;
////            else if (entry.type == TTEntry.flag.lowerBound)
////                Alpha = Math.Max(Alpha, entry.value);
////            else if (entry.type == TTEntry.flag.upperBound)
////                Beta = Math.Min(Beta, entry.value);
////            if (Alpha >= Beta)
////                return entry.value;

////            // Else, make the TT move first (can it already been pruned ?) (iff depth != 0)
////            if (depth > 0)
////            {
////                int value = simulateMove(B, depth, Alpha, Beta, entry.bestMove, B.getCurrentPlayer().getPlayerId(),
////                playerOne, playerTwo, color, placeTown);

////                // We don't need to loop over this value anymore (within our moves)
////                startLoop++;

////                // Save value
////                if (!placeTown && depth == this.searchDepth)
////                {
////                    this.scores[0] = value;
////                }

////                // Check if we can prune -> if yes, we don't need to determine all the other moves anymore
////                Alpha = Math.Max(Alpha, value);
////                if (Alpha >= Beta)
////                {
////                    skipAllMoves = true;
////                    bestValue = value;
////                    bestMove = entry.bestMove;
////                }
////            }
////        }

////        // Terminal nodes?
////        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
////        if (depth <= 0) { return this.Evaluate(B, new int[] { 0 }) * color; }

////        // If TT move hasn't pruned yet, search all (ignoring the first one), otherwise continue
////        if (!skipAllMoves)
////        {
////            // Get all possible moves
////            List<Move> possibleMoves;

////            // If it is the first depth, take over the sorted moves (unless we are looking at the town placement)
////            if (!placeTown && depth == this.searchDepth)
////            {
////                possibleMoves = this.moves;
////            }
////            // Else sort them on TT and knowledge
////            else
////            {
////                possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId());
////                possibleMoves = orderMovesVD(possibleMoves, entry.bestMove, this.killerMoves, depth, this.historyHeuristic);
////            }

////            if (possibleMoves.Count() > 0)
////            {
////                // Initialise for search
////                int currentPlayerId = B.getCurrentPlayer().getPlayerId();

////                // Determine bestValue
////                if (startLoop == 0)
////                {
////                    bestValue = simulateMove(B, depth, Alpha, Beta, possibleMoves[startLoop], currentPlayerId, playerOne, playerTwo, color, placeTown);
////                    bestMove = possibleMoves[0];
////                }

////                for (int i = 1; i < possibleMoves.Count(); i++)
////                {
////                    // Determine value, by replacing pieces, calling the search function, and placing the pieces back
////                    int value = simulateMove(B, depth, Alpha, Beta, possibleMoves[i], currentPlayerId, playerOne, playerTwo, color, placeTown);

////                    // Compare with best value
////                    if (value > bestValue)
////                    {
////                        bestValue = value;
////                        bestMove = possibleMoves[i];
////                    }

////                    // If it is at our search depth, and isn't during placement, add score to list
////                    if (!placeTown && depth == this.searchDepth)
////                    {
////                        this.scores[i] = value;
////                    }

////                    // Prune when possible
////                    if (value >= Beta)
////                    {
////                        this.killerMoves[(int)(2*depth - 1)] = bestMove;
////                        this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
////                        break;
////                    }

////                    if (this.sw.ElapsedMilliseconds > this.maxTime)
////                        break;
////                }
////            }
////            else
////            {
////                return -this.evalBound; // No color, looking from current perspective - terminal node
////            }
////        }

////        // Store entry in TT
////        TTEntry.flag flagType;
////        if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
////        else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
////        else { flagType = TTEntry.flag.exact; }

////        this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, bestMove,
////            depth, this.zH.getHashValue(this.currentHash));

////        // Return value
////        return bestValue;
////    }

////    int simulateMove(Board B, float depth, int Alpha, int Beta, Move move, int currentPlayerId, Player playerOne, Player playerTwo, int color, bool placeTown)
////    {
////        // // Determine fractional ply
////        float fracply = (move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) ? .5f : 1;

////        // Check if to position is town (when captured or shoot)
////        bool isTown = false;
////        if ((move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture) &&
////            B.getSpaces()[move.To.x, move.To.y].getPieceType() == Piece.epieceType.town)
////            isTown = true;

////        // Make Move (and update hash)
////        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
////        B.movePiece(move, false, placeTown);

////        // Update Cannons
////        B.updateCannons();

////        // Switch player (and update hash)
////        this.currentHash = this.zH.switchPlayer(this.currentHash);
////        B.switchPlayer(playerOne, playerTwo);

////        // Get value
////        int value = -NegaScoutAlphaBetaVDSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

////        // Switch player (and update hash)
////        this.currentHash = this.zH.switchPlayer(this.currentHash);
////        B.switchPlayer(playerOne, playerTwo);

////        // Undo Move (and update hash)
////        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
////        B.UndoMove(move, placeTown);

////        return value;
////    }

////    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
////    {
////        // Start sw
////        sw.Restart(); sw.Start();

////        // Get moves and entry (entry = reference type, so changes automatically)
////        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
////        List<Coord> placements = B.getPossiblePlacements(this.playerId);

////        //List<int[]> testMoves = this.moves;

////        // Initialise
////        int nrOfNodes = 0;
////        int actualDepth = 0;
////        this.seenNodes = 0;
////        this.searchDepth = 1;
////        int[] scores = new int[placements.Count()];
////        int[] finishedScore = new int[placements.Count()];
////        List<Move> finishedMoves = new List<Move>();

////        while (this.sw.ElapsedMilliseconds < this.maxTime && finishedScore.Max() < this.evalBound && finishedScore.Max() > -this.evalBound)
////        {
////            // Entry will change automatically (reference type)

////            // Order moves (based on previous scores)
////            placements = orderPlacements(placements, scores);

////            // Determine scores
////            scores = new int[placements.Count()];

////            // Reset killer moves
////            this.killerMoves = new int[2*searchDepth - 1][];

////            // Determine placement
////            for (int i = 0; i < placements.Count(); i++)
////            {
////                B.placeTown(placements[i], false);

////                // Update cannons
////                B.updateCannons();

////                // switch player
////                B.switchPlayer(playerOne, playerTwo);
////                this.currentHash = this.zH.switchPlayer(this.currentHash);

////                scores[i] = -NegaScoutAlphaBetaVDSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true, false);

////                this.currentHash = this.zH.switchPlayer(this.currentHash);
////                B.removeTown(placements[i]);
////            }

////            // Print time per iteration
////            if (this.printIterations)
////            {
////                // Print performance
////                Console.WriteLine("Depth " + (this.searchDepth) + ". ElapsedTime [ms]: " + this.sw.ElapsedMilliseconds);
////            }

////            // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
////            if (this.sw.ElapsedMilliseconds < this.maxTime)
////            {
////                finishedScore = (int[])scores.Clone();
////                finishedMoves = placements;
////                actualDepth = searchDepth;
////                nrOfNodes = this.seenNodes;
////            }

////            // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
////            this.searchDepth++;
////            B.updateCannons();
////        }


////        //Console.WriteLine(this.scores.toPrint());

////        // Stop stopwatch
////        sw.Stop();

////        // Print nodes evaluated
////        Console.WriteLine("Nodes evaluated: " + nrOfNodes + ", Depth: " + (actualDepth) +
////            ". In " + this.sw.ElapsedMilliseconds + " [ms].");

////        // Place town
////        B.placeTown(finishedMoves[scores.argMax()], print);
////    }

////    public override void resetTT()
////    {
////        this.TT.reset();
////    }
////}

//// Attempt 3

/// Attempt 3
internal class xIterativeDeepening : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    //int[] weights = new int[] { -7, -7, 7, 4, -10, 2, -9, 3, 4, 1, -4 }; // Training against random

    // Basic NegaMax with alpha beta pruning
    public xIterativeDeepening(int id, int searchTimeMs, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.evalBound = this.getBoundsEval(this.weights);

        if (id == 1)
        {
            this.weights = new int[] { -2, -4, -5, -9, 5, 3, -6, 3, -10, 2, -1 };
        }
    }

    public override int[] getWeights() { return this.weights; }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.searchDepth < 2 && this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores
                this.scores.setAll(-this.evalBound+1);

                // Determine scores
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Console.WriteLine(this.scores.toPrint());

            //Print nodes evaluated
            //Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true);
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = B.getPossibleMoves(B.getCurrentPlayer().getPlayerId()).orderByMoveType();

        int value = -this.evalBound;
        int bestValue = value;
        if (possibleMoves.Count() > 0)
        {
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, playerOne, playerTwo, color, placeTown);

                //if (possibleMoves[i].type == Move.moveType.slide)
                //{
                //    Console.WriteLine($"{depth} {value}");
                //}

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            return bestValue;
        }
        else
        {
            return -this.evalBound; // No color, looking from current perspective
        }
    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        // Get placements
        //B.getPiecesCoords().ForEach(c => Console.Write(c.ToString())); Console.WriteLine();
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());
        //B.getPiecesCoords().ForEach(c => Console.Write(c.ToString())); Console.WriteLine();

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

            // Reset scores
            scores.setAll(-this.evalBound);

            // Determine scores
            for (int i = 0; i < placements.Count(); i++)
            {
                B.placeTown(placements[i], false);

                // Update cannons
                B.updateCannons();

                // switch player
                B.switchPlayer(playerOne, playerTwo);

                //Console.WriteLine($"Depth: {this.searchDepth} - Placement number {i}");
                //B.getPiecesCoords().ForEach(c => Console.Write(c.ToString())); Console.WriteLine();
                scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);
                //B.getPiecesCoords().ForEach(c => Console.Write(c.ToString())); Console.WriteLine("\n");

                B.switchPlayer(playerOne, playerTwo);

                B.removeTown(placements[i]);
            }

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

        //Print nodes evaluated
        //Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

        // Make best move
        B.placeTown(bestPlacement, print);
    }

    int simulateMove(Board B, Move move, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Make Move
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

        // Switch player
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        B.UndoMove(move, placeTown);

        return value;
    }

    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }

}

internal class xIterativeDeepeningTT : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;

    // Basic NegaMax with alpha beta pruning and TT
    public xIterativeDeepeningTT(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores
                this.scores.setAll(-this.evalBound);

                // Determine scores
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true);
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth)
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove);

        int value = -this.evalBound;
        int bestValue = value;
        int bestValueIndex = -1;
        if (possibleMoves.Count() > 0)
        {
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

            // Reset scores
            scores.setAll(-this.evalBound);

            // Determine scores
            for (int i = 0; i < placements.Count(); i++)
            {
                B.placeTown(placements[i], false);

                // Update cannons
                B.updateCannons();

                // switch player
                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                B.removeTown(placements[i]);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningTTKM : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, 

    // Basic NegaMax with alpha beta pruning and TT and Killer move
    public xIterativeDeepeningTTKM(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine scores
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            B.movePiece(bestMove, print, false, true);
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth)
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth);

        int value = -this.evalBound;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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
                            this.killerMoves[depth - 1] = possibleMoves[i];
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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
            this.killerMoves = new Move[searchDepth];

            // Determine scores
            for (int i = 0; i < placements.Count(); i++)
            {
                B.placeTown(placements[i], false);

                // Update cannons
                B.updateCannons();

                // switch player
                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                B.removeTown(placements[i]);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningOrdered : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningOrdered(int id, int searchTimeMs, int lengthHashKey, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine scores
                NegaMaxAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth)
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound-1;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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
                            this.killerMoves[depth - 1] = possibleMoves[i];
                            this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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
            this.killerMoves = new Move[searchDepth];

            // Determine scores
            for (int i = 0; i < placements.Count(); i++)
            {
                B.placeTown(placements[i], false);

                // Update cannons
                B.updateCannons();

                // switch player
                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, -this.evalBound, this.evalBound, playerOne, playerTwo, -1, true);

                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);

                B.removeTown(placements[i]);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningAS : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int delta;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningAS(int id, int searchTimeMs, int lengthHashKey, int delta, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.delta = delta;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

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

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[searchDepth];

                // Determine Alpha and Beta
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false);

                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false);
                }
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false);
                }

                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, int depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
        // If position is new, depth = -1 (initial value of TTEntry)
        if (entry.depth >= depth)
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth == 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMoves(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound-1;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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
                            this.killerMoves[depth - 1] = possibleMoves[i];
                            this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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
            this.killerMoves = new Move[searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // readjust window
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - 1, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningNM : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int delta;
    int R;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningNM(int id, int searchTimeMs, int lengthHashKey, int delta, int R, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.delta = delta;
        this.R = R;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

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

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2*searchDepth];

                // Determine Alpha and Beta
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                if (score >= beta)
                {
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                else if (score <= alpha)
                {
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound-1;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (!lastNullMove && depth != this.searchDepth && depth > 0 && !this.endGame)
            {
                this.currentHash = this.zH.switchPlayer(this.currentHash);
                B.switchPlayer(playerOne, playerTwo);
                int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                B.switchPlayer(playerOne, playerTwo);
                this.currentHash = this.zH.switchPlayer(this.currentHash);

                if (score > Beta)
                {
                    return score;
                }
            }

            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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
                            this.killerMoves[(int)(2*depth - 1)] = possibleMoves[i];
                            this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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
            this.killerMoves = new Move[2*searchDepth];

            // Determine alpha and gamma
            int alpha = guess - this.delta; int beta = guess + this.delta;

            // Determine scores
            scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);

            // readjust window
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningFullAS : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int delta;
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullAS(int id, int searchTimeMs, int lengthHashKey, int delta, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.delta = delta;
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

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

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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


            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;

                    // If it is the first one of this loop (i==1), also add the best value (the first one, outside this loop)
                    if (i == 1)
                    {
                        this.scores[0] = bestValue;
                    }
                }

                // Check if value is higher
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
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

            // readjust window
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningFullNS : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullNS(int id, int searchTimeMs, int lengthHashKey, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine scores
                int score = NegaScoutAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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

                // End -> add One search depth, and update cannons, such that new possible moves (next depth) can be determined correctly
                this.searchDepth++;
                B.updateCannons();
            }

            // Stop stopwatch
            sw.Stop();

            //Print nodes evaluated
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaScoutAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { this.seenNodes++; return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { this.seenNodes++; return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound - 1;
        int bestValue;
        int bestValueIndex = 0;

        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaScoutAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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

            // NegaScout
            bestValue = simulateMove(B, possibleMoves[0], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);
            if (bestValue < Beta)
            {
                for (int i = 1; i < possibleMoves.Count(); i++)
                {
                    // Calculate bounds
                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

                    // Determine value
                    value = simulateMove(B, possibleMoves[i], depth, lbound, ubound, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // Check if result is fail high
                    if (value >= ubound && value < Beta)
                        value = simulateMove(B, possibleMoves[i], depth, ubound, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // If it is at our search depth, and isn't during placement, add score to list
                    if (!placeTown && depth == this.searchDepth)
                    {
                        this.scores[i] = value;

                        // If it is the first one of this loop (i==1), also add the best value (the first one, outside this loop)
                        if (i == 1)
                        {
                            this.scores[0] = bestValue;
                        }
                    }

                    // Check if value is higher
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
                                this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                break;
                            }
                        }
                    }

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
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

            // Determine scores
            scores = simulatePlacements(B, placements, -this.evalBound, this.evalBound, playerOne, playerTwo);

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaScoutAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaScoutAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningFullASLast : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int delta;
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullASLast(int id, int searchTimeMs, int lengthHashKey, int delta, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.delta = delta;
        this.R = R;
        this.C = C;
        this.M = M;


        if (id == 1)
        {
            this.weights = new int[] { 2, 4, 5, 0, 5, -3, -6, -3, -10, 2, 1 };
        }
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType(); ;
        this.scores = new int[moves.Count()];

        this.seenNodes = 0;

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

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine Alpha and Beta
                int alpha = guess - this.delta; int beta = guess + this.delta;

                // Determine scores
                int score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);

                if (score >= beta)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = score; beta = this.evalBound;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }
                else if (score <= alpha)
                {
                    this.scores.setAll(-this.evalBound);
                    alpha = -this.evalBound; beta = score;
                    score = NegaMaxAlphaBetaSearch(B, this.searchDepth, alpha, beta, playerOne, playerTwo, 1, false, false);
                }

                guess = score;

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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
                    int bestVal = this.scores[0];
                    int i = 1;

                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
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

            // Make best move, update HH and currenthash
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            this.currentHash = this.zH.makeMoveHash(this.currentHash, bestMove, this.playerId, false);
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaMaxAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        this.seenNodes++; 
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int value = -this.evalBound - 1;
        int bestValue = value;
        int bestValueIndex = -1;

        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaMaxAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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


            for (int i = 0; i < possibleMoves.Count(); i++)
            {
                // Determine value
                value = simulateMove(B, possibleMoves[i], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                // If it is at our search depth, and isn't during placement, add score to list
                if (!placeTown && depth == this.searchDepth)
                {
                    this.scores[i] = value;
                }

                // Check if value is higher
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
                            this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                            this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                            break;
                        }
                    }
                }

                if (this.sw.ElapsedMilliseconds > this.maxTime)
                    break;
            }

            // Store entry in TT
            TTEntry.flag flagType;
            if (bestValue <= olda) { flagType = TTEntry.flag.upperBound; }
            else if (bestValue >= Beta) { flagType = TTEntry.flag.lowerBound; }
            else { flagType = TTEntry.flag.exact; }

            // Set entry
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

            // readjust window
            if (scores.Max() >= beta)
            {
                alpha = scores.Max(); beta = this.evalBound;
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }
            else if (scores.Max() <= alpha)
            {
                alpha = -this.evalBound; beta = scores.Max();
                scores = simulatePlacements(B, placements, alpha, beta, playerOne, playerTwo);
            }

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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaMaxAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaMaxAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }

    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

internal class xIterativeDeepeningFullNSLast : Player
{
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    //int[] weights = new int[] { 2, 1, 3, 4, 2, 6, 100, 2, -2, -1, -1, -10, 4, 3, 2, 1, 1 };
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullNSLast(int id, int searchTimeMs, int lengthHashKey, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine scores
                NegaScoutAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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
                    int bestVal = this.scores[0];
                    int i = 1;

                    while (i+1 < this.scores.Length && this.scores[i+1] > -this.evalBound)
                    {
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
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            this.currentHash = this.zH.makeMoveHash(this.currentHash, bestMove, this.playerId, false);
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaScoutAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        this.seenNodes++;
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { return this.Evaluate(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int bestValueIndex = 0;
        int value;
        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaScoutAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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

            // NegaScout
            int bestValue = simulateMove(B, possibleMoves[0], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);            
            if (bestValue < Beta)
            {
                for (int i = 1; i < possibleMoves.Count(); i++)
                {
                    // Calculate bounds
                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

                    // Determine value
                    value = simulateMove(B, possibleMoves[i], depth, lbound, ubound, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // Check if result is fail high
                    if (value >= ubound && value < Beta)
                        value = simulateMove(B, possibleMoves[i], depth, ubound, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // If it is at our search depth, and isn't during placement, add score to list
                    if (!placeTown && depth == this.searchDepth)
                    {
                        this.scores[i] = value;

                        // If it is the first one of this loop (i==1), also add the best value (the first one, outside this loop)
                        if (i == 1)
                        {
                            this.scores[0] = bestValue;
                        }
                    }

                    // Check if value is higher
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
                                this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                break;
                            }
                        }
                    }

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
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaScoutAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaScoutAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }

    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

internal class xIterativeDeepeningFullNSLast2 : Player
{
    // More features
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 4, 2, 6, 100, 2, -2, -1, -1, -10, 4, 3, 2, -1, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullNSLast2(int id, int searchTimeMs, int lengthHashKey, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval2(this.weights);
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine scores
                NegaScoutAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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
                    int bestVal = this.scores[0];
                    int i = 1;

                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
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
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaScoutAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        this.seenNodes++;
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { return this.Evaluate2(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int bestValueIndex = 0;
        int value;
        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaScoutAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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

            // NegaScout
            int bestValue = simulateMove(B, possibleMoves[0], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);
            if (bestValue < Beta)
            {
                for (int i = 1; i < possibleMoves.Count(); i++)
                {
                    // Calculate bounds
                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

                    // Determine value
                    value = simulateMove(B, possibleMoves[i], depth, lbound, ubound, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // Check if result is fail high
                    if (value >= ubound && value < Beta)
                        value = simulateMove(B, possibleMoves[i], depth, ubound, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // If it is at our search depth, and isn't during placement, add score to list
                    if (!placeTown && depth == this.searchDepth)
                    {
                        this.scores[i] = value;

                        // If it is the first one of this loop (i==1), also add the best value (the first one, outside this loop)
                        if (i == 1)
                        {
                            this.scores[0] = bestValue;
                        }
                    }

                    // Check if value is higher
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
                                this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                break;
                            }
                        }
                    }

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
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaScoutAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaScoutAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }
}

internal class xIterativeDeepeningFullNSLast3 : Player
{
    // structs for features instead of int[] -> Turned out slower
    int searchDepth;
    int seenNodes = 0;
    Stopwatch sw = new Stopwatch();
    int evalBound;
    int[] scores;
    int maxTime;
    bool printIterations;
    List<Move> moves = new List<Move>();
    int[] weights = new int[] { 2, 1, 3, 100, 2, -2, -1, -1, -10, 2, 1 };
    ZobristHashing zH;
    TranspositionTable TT;
    ulong currentHash;
    Move[] killerMoves; // depth, move
    int[,,,] historyHeuristic = new int[Board.n, Board.n, Board.n, Board.n]; // Add 1, when pruning or actually making the move
    int R, C, M;
    bool endGame = false;

    // Basic NegaMax with alpha beta pruning and TTm Killer move and History Heuristic
    public xIterativeDeepeningFullNSLast3(int id, int searchTimeMs, int lengthHashKey, int R, int C, int M, bool printIterations)
    {
        this.playerId = id;
        this.maxTime = searchTimeMs;
        this.printIterations = printIterations;
        this.zH = new ZobristHashing(lengthHashKey);
        this.TT = new TranspositionTable(lengthHashKey);
        this.evalBound = this.getBoundsEval(this.weights);
        this.R = R;
        this.C = C;
        this.M = M;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Start sw
        sw.Restart(); sw.Start();

        this.moves = B.getPossibleMoves(this.playerId).orderByMoveType();
        this.seenNodes = 0;

        if (moves.Count() > 0)
        {
            // Initialise
            int nrOfNodes = 0;
            int actualDepth = 0;
            this.seenNodes = 0;
            this.searchDepth = 1;
            this.scores = new int[this.moves.Count()];
            Move bestMove = moves[0];

            while (this.sw.ElapsedMilliseconds < this.maxTime && this.scores.Max() < this.evalBound && this.scores.Max() > -this.evalBound)
            {
                // Order this.moves
                this.moves = orderMovesScore(this.moves, this.scores);

                // Reset scores and killermoves
                this.scores.setAll(-this.evalBound);
                this.killerMoves = new Move[2 * searchDepth];

                // Determine scores
                NegaScoutAlphaBetaSearch(B, this.searchDepth, -this.evalBound, this.evalBound, playerOne, playerTwo, 1, false, false);

                // Replace the oldScores with new scores (if enough time is left, otherewise score isn't complete)
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
                    int bestVal = this.scores[0];
                    int i = 1;

                    while (i + 1 < this.scores.Length && this.scores[i + 1] > -this.evalBound)
                    {
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
            Console.WriteLine($"Nodes evaluated: {nrOfNodes} at depth {actualDepth}. In {this.sw.ElapsedMilliseconds} [ms].");

            // Make best move
            this.historyHeuristic[bestMove.From.x, bestMove.From.y, bestMove.To.x, bestMove.To.y]++;
            this.historyHeuristic.multiplyDiscount();
            B.movePiece(bestMove, print, false, true);

            if (!this.endGame && countMinTotalPieces(B) < 8)
            {
                this.endGame = true;
            }
        }
        else
        {
            this.NoLegalMoves();
        }
    }

    int NegaScoutAlphaBetaSearch(Board B, float depth, int Alpha, int Beta, Player playerOne, Player playerTwo, int color, bool placeTown, bool lastNullMove)
    {
        this.seenNodes++;
        // Transposition Table look up
        int olda = Alpha;
        TTEntry entry = this.TT.retrieve(this.zH.getHashKey(this.currentHash), this.zH.getHashValue(this.currentHash));
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

        if (!placeTown && !B.TownsInGame()) { return B.TownInGame(this.playerId) ? this.evalBound * color : -this.evalBound * color; }
        if (depth <= 0) { return this.Evaluate3(B, this.weights) * color; }

        int currentPlayerId = B.getCurrentPlayer().getPlayerId();

        // Ordering moves correctly
        List<Move> possibleMoves;
        if (depth == this.searchDepth)
            possibleMoves = this.moves;
        else
            possibleMoves = orderMovesVD(B.getPossibleMoves(currentPlayerId), entry.bestMove, this.killerMoves, depth, this.historyHeuristic);

        int bestValueIndex = 0;
        int value;
        if (possibleMoves.Count() > 0)
        {
            // Null Move
            if (depth != this.searchDepth && depth > 0 && !this.endGame && !placeTown)
            {
                if (!lastNullMove)
                {
                    this.currentHash = this.zH.switchPlayer(this.currentHash);
                    B.switchPlayer(playerOne, playerTwo);
                    int score = -NegaScoutAlphaBetaSearch(B, depth - 1 - this.R, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, true);
                    B.switchPlayer(playerOne, playerTwo);
                    this.currentHash = this.zH.switchPlayer(this.currentHash);

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

            // NegaScout
            int bestValue = simulateMove(B, possibleMoves[0], depth, Alpha, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);
            if (bestValue < Beta)
            {
                for (int i = 1; i < possibleMoves.Count(); i++)
                {
                    // Calculate bounds
                    int lbound = Math.Max(bestValue, Alpha); int ubound = lbound + 1;

                    // Determine value
                    value = simulateMove(B, possibleMoves[i], depth, lbound, ubound, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // Check if result is fail high
                    if (value >= ubound && value < Beta)
                        value = simulateMove(B, possibleMoves[i], depth, ubound, Beta, currentPlayerId, playerOne, playerTwo, color, placeTown);

                    // If it is at our search depth, and isn't during placement, add score to list
                    if (!placeTown && depth == this.searchDepth)
                    {
                        this.scores[i] = value;

                        // If it is the first one of this loop (i==1), also add the best value (the first one, outside this loop)
                        if (i == 1)
                        {
                            this.scores[0] = bestValue;
                        }
                    }

                    // Check if value is higher
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
                                this.killerMoves[(int)(2 * depth - 1)] = possibleMoves[i];
                                this.historyHeuristic[possibleMoves[i].From.x, possibleMoves[i].From.y, possibleMoves[i].To.x, possibleMoves[i].To.y]++;
                                break;
                            }
                        }
                    }

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
            this.TT.setEntry(this.zH.getHashKey(this.currentHash), bestValue, flagType, possibleMoves[bestValueIndex],
                depth, this.zH.getHashValue(this.currentHash));

            return bestValue;
        }
        else
        {
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

        //Print nodes evaluated
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
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.movePiece(move, false, placeTown, false);

        // Update Cannons
        B.updateCannons();

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Get value
        int value = -NegaScoutAlphaBetaSearch(B, depth - fracply, -Beta, -Alpha, playerOne, playerTwo, color * -1, placeTown, false);

        // Switch player
        this.currentHash = this.zH.switchPlayer(this.currentHash);
        B.switchPlayer(playerOne, playerTwo);

        // Undo Move
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, currentPlayerId, isTown);
        B.UndoMove(move, placeTown);

        return value;
    }

    int[] simulatePlacements(Board B, List<Coord> placements, int alpha, int beta, Player playerOne, Player playerTwo)
    {
        int[] scores = new int[placements.Count()];
        for (int i = 0; i < placements.Count(); i++)
        {
            B.placeTown(placements[i], false);

            // Update cannons
            B.updateCannons();

            // switch player
            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            scores[i] = -NegaScoutAlphaBetaSearch(B, this.searchDepth - 1, alpha, beta, playerOne, playerTwo, -1, true, false);

            this.currentHash = this.zH.switchPlayer(this.currentHash);
            B.switchPlayer(playerOne, playerTwo);

            B.removeTown(placements[i]);
        }

        return scores;
    }

    int countMinTotalPieces(Board B)
    {
        int[] count = new int[2];

        // Count number of 
        for (int i = 0; i < B.getPiecesCoords().Count(); i++)
        {
            count[B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId() - 1]++;
        }

        return count.Min();
    }

    public override void resetTT()
    {
        this.TT.reset();
    }

    public override void setWeights(int[] wghts)
    {
        this.weights = wghts;
        this.evalBound = getBoundsEval(wghts);
    }
}

// Corrected (alpha beta - color, etc)

internal class Template : Player
{
        
    public Template(int id)
    {
        this.playerId = id;
    }

    public override void makeMove(Board B, bool print, Player playerOne, Player playerTwo)
    {
        List<Move> moves = new List<Move>();

        // If legal moves continue game
        if (moves.Count() > 0)
        {
            // Determine best move
            //Move move = moves[rnd.Next(moves.Count())];

            // Make move
            //B.movePiece(move, print, placeTown);
        }
        // Else set no legal moves
        else
            this.NoLegalMoves();

    }

    public override void placeTown(Board B, bool print, Player playerOne, Player playerTwo)
    {
        // Get placements
        List<Coord> placements = B.getPossiblePlacements(B.getCurrentPlayer().getPlayerId());

        // Determine placement
        //int[] placement = placements[rnd.Next(placements.Count())];

        // Place town
        //B.placeTown(placement, print);
    }
}
