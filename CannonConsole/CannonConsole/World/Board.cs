using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TT;
using World;

public struct Move
{
    public Coord From, To;
    public moveType type;

    public enum moveType
    {
        step,
        slide,
        retreat,
        soldierCapture,
        shoot
    }

    public Move(int xFrom, int yFrom, int xTo, int yTo, moveType type)
    {
        this.From.x = xFrom;
        this.From.y = yFrom;
        this.To.x = xTo;
        this.To.y = yTo;
        this.type = type;
    }

    public static bool operator ==(Move c1, Move c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(Move c1, Move c2)
    {
        return !c1.Equals(c2);
    }

    public override string ToString()
    {
        return $"[{this.From.ToString()}, {this.To.ToString()}]";
    }
}

public struct Coord
{
    public int x, y;
    
    public Coord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static bool operator ==(Coord c1, Coord c2)
    {
        return c1.Equals(c2);
    }

    public static bool operator !=(Coord c1, Coord c2)
    {
        return !c1.Equals(c2);
    }

    public override string ToString()
    {
        return $"({this.x}, {this.y})";
    }
}

public struct sUnreachableCoords
{
    List<Coord> playerOne;
    List<Coord> playerTwo;

    public sUnreachableCoords(int i)
    {
        this.playerOne = new List<Coord>();
        this.playerTwo = new List<Coord>();
    }

    public void setPlayerOne(List<Coord> coords)
    {
        this.playerOne = coords;
    }

    public void setPlayerTwo(List<Coord> coords)
    {
        this.playerTwo = coords;
    }

    public List<Coord> getPlayer(int playerId)
    {
        if (playerId == 1)
            return playerOne;
        else
            return playerTwo;
    }

    public void Clear(int playerId)
    {
        if (playerId == 1)
            playerOne.Clear();
        else
            playerTwo.Clear();
    }

    public void Add(Coord coords, int playerId)
    {
        if (playerId == 1)
            playerOne.Add(coords);
        else
            playerTwo.Add(coords);
    }
}

public class Board
{
    public static int n = 10;
    Space[,] Spaces = new Space[n, n];
    List<Coord> PiecesCoords = new List<Coord>();
    Coord[] TownCoords = new Coord[2];
    sUnreachableCoords unreachableCoords = new sUnreachableCoords(0);
    //Coord[][] aroundTownCoords = new Coord[2][];
    Player currentPlayer;
    int[][,] captureDir = new int[2][,]
    {
        new int[5,2] { {-1, 0}, {-1, 1}, {0, 1}, {1, 1}, {1, 0}},
        new int[5,2] { {-1, 0}, {-1, -1}, {0, -1}, {1, -1}, {1, 0}}
    };
    int numCaptures;
    int[,] adjDir = new int[,] { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 }, };
    List<Move> allMoves = new List<Move>();

    ulong currentHash;
    ZobristHashing zH = new ZobristHashing(20);
    Dictionary<ulong, int> visitedStates = new Dictionary<ulong, int>();
    List<Move> moveList = new List<Move>();
    public string FEN;

    public void setup()
    {
        // Create Spaces
        createSpaces();

        // Set num of captures for getPossibleMoves() (captures and retreat);
        this.numCaptures = captureDir[0].GetLength(0);

        // Set Pieces
        setPieces(1);
        setPieces(2);

        // Determine Cannons
        determineCannons();
    }

    public void getRandomBoard(int numPiecesOne, int numPiecesTwo, bool TownOne, bool TownTwo)
    {
        // Create Spaces
        createSpaces();

        // Set num of captures for getPossibleMoves() (captures and retreat);
        this.numCaptures = captureDir[0].GetLength(0);

        // Setup random pieces
        Random rand = new Random();
        setPiecesRandom(1, numPiecesOne, TownOne, rand);
        setPiecesRandom(2, numPiecesTwo, TownTwo, rand);

        // Determine cannons
        determineCannons();
    }

    public Space[,] getSpaces()
    {
        return this.Spaces;
    }

    public List<Move> getMadeMoves()
    {
        return this.moveList;
    }

    public string coordsToName(int i, int j)
    {
        return ((char)(i + 65)).ToString() + (j + 1);
    }

    void createSpaces()
    {
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Spaces[i, j] = new Space(coordsToName(i, j), i, j);
            }
        }
    }

    internal void createInitialHash()
    {
        this.currentHash = this.zH.generateBoardHash(this);
        updateVisitedState(this.currentHash);
    }

    void setPieces(int playerId)
    {
        // Logic of placing pieces
        int xStart = playerId == 1 ? 0 : 1;
        int xEnd = playerId == 1 ? n - 2 : n - 1;
        int yStart = playerId == 1 ? 1 : 6;
        int yEnd = playerId == 1 ? 3 : 8;

        for (int x = xStart; x <= xEnd; x += 2)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                Spaces[x, y].setPiece(playerId, Piece.epieceType.soldier);
                PiecesCoords.Add(new Coord(x,y));

                //Debug.Log($"playerId: ({x}, {y})");
            }
        }
    }

    void setPiecesRandom(int playerId, int numPieces, bool Town, Random rand)
    {
        int count = 0;
        this.setCurrentPlayer(new Human(playerId));
        // Place town
        if (Town)
        {
            bool townPlaced = false;
            while (!townPlaced)
            {
                int x = rand.Next(1, n - 1); int y = playerId == 1 ? 0 : n - 1;
                if (!Spaces[x, y].isOccupied())
                {
                    this.placeTown(new Coord(x, y), false);
                    townPlaced = true;
                }
            }
        }

        // Place pieces
        while (count < numPieces)
        {
            int x = rand.Next(n); int y = rand.Next(n);
            if (!Spaces[x, y].isOccupied())
            {
                Spaces[x, y].setPiece(playerId, Piece.epieceType.soldier);
                PiecesCoords.Add(new Coord(x, y));
                count++;
            }
        }
    }

    public List<Coord> getPiecesCoords()
    {
        return this.PiecesCoords;
    }

    public Coord[] getTownCoords()
    {
        return this.TownCoords;
    }

    public void setCurrentPlayer(Player player)
    {
        this.currentPlayer = player;
    }

    public Player getCurrentPlayer()
    {
        return this.currentPlayer;
    }

    public sUnreachableCoords getUnreachableCoords()
    {
        return this.unreachableCoords;
    }

    //public Coord[][] getAroundTownCoords()
    //{
    //    return this.aroundTownCoords;
    //}

    public override string ToString()
    {
        string str = "";
        for (int y = n - 1; y >= 0; y--)
        {
            for (int x = 0; x < n; x++)
            {
                str += Spaces[x, y].getPieceId() + " ";
            }
            str += "\n";
        }

        return str;
    }

    public void printBoard()
    {
        // Print characters
        for (int rep = 0; rep < 2; rep++)
        {
            Console.Write("      ");
            for (int x = 0; x < n; x++)
            {
                if (rep == 0)
                    Console.Write(((char)(x + 65)).ToString() + " ");
                else
                    Console.Write("--");
            }
            Console.Write("\n");
        }

        // Print Pieces (and numbers)
        for (int y = n - 1; y >= 0; y--)
        {
            // Print number for reader
            Console.Write(" " + (y + 1) + ((y+1) < 10 ? "  | " : " | "));
            
            for (int x = 0; x < n; x++)
            {
                // Determine Color
                if (this.Spaces[x, y].getPieceId() == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                else if (this.Spaces[x, y].getPieceId() == 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (this.Spaces[x, y].getPieceId() == 0)
                {
                    Console.ResetColor();
                }

                // Print correct Value for piece
                if (this.Spaces[x, y].isOccupied())
                {
                    if (this.Spaces[x, y].getPieceType() == Piece.epieceType.soldier)
                    {
                        Console.Write("s ");
                    }
                    else if (this.Spaces[x, y].getPieceType() == Piece.epieceType.town)
                    {
                        Console.Write("t ");
                    }
                    else if (this.Spaces[x, y].getPieceType() == Piece.epieceType.cannon)
                    {
                        Console.Write("c ");
                    }
                }
                else
                {
                    Console.Write("0 ");
                }
            }
            Console.ResetColor();

            // Print number for reader
            Console.Write(" | " + (y + 1));

            Console.Write("\n");
        }
        // Print characters
        for (int rep = 0; rep < 2; rep++)
        {
            Console.Write("      ");
            for (int x = 0; x < n; x++)
            {
                if (rep == 1)
                    Console.Write(((char)(x + 65)).ToString() + " ");
                else
                    Console.Write("--");
            }
            Console.Write("\n");
        }

        Console.Write("\n");
    }

    public void updateCannons()
    {
        // Reset all cannons
        resetCannons();

        // Determine the cannons again
        determineCannons();
    }

    void determineCannons()
    {
        for (int c = 0; c < PiecesCoords.Count(); c++)
        {
            Coord coord = PiecesCoords[c];
            List<int[]> cannonDir = new List<int[]>();
            for (int i = 0; i < adjDir.GetLength(0); i++)
            {
                if (onBoard(coord.x + adjDir[i, 0] * 3, coord.y + adjDir[i, 1] * 3) &&
                    this.Spaces[coord.x + adjDir[i, 0], coord.y + adjDir[i, 1]].getPieceId() == this.Spaces[coord.x, coord.y].getPieceId() &&
                    this.Spaces[coord.x + adjDir[i, 0], coord.y + adjDir[i, 1]].getPieceType() != Piece.epieceType.town &&
                    this.Spaces[coord.x + adjDir[i, 0] * 2, coord.y + adjDir[i, 1] * 2].getPieceId() == this.Spaces[coord.x, coord.y].getPieceId() &&
                    this.Spaces[coord.x + adjDir[i, 0] * 2, coord.y + adjDir[i, 1] * 2].getPieceType() != Piece.epieceType.town &&
                    !this.Spaces[coord.x + adjDir[i, 0] * 3, coord.y + adjDir[i, 1] * 3].isOccupied())
                {
                    cannonDir.Add(new int[] { adjDir[i,0], adjDir[i,1] });
                }
            }
            this.Spaces[coord.x, coord.y].setCannon(cannonDir);
        }
    }

    void resetCannons()
    {
        for (int c = 0; c < PiecesCoords.Count(); c++)
        {
            this.Spaces[PiecesCoords[c].x, PiecesCoords[c].y].pieceResetCannon();
        }
    }

    public List<Move> getPossibleMoves(int playerId)
    {
        this.allMoves.Clear();
        for (int i = 0; i < PiecesCoords.Count(); i++)
        {
            if (this.Spaces[PiecesCoords[i].x, PiecesCoords[i].y].getPieceId() == playerId)
            {
               getPossibleMove(this.Spaces[PiecesCoords[i].x, PiecesCoords[i].y], playerId);
            }
        }

        // Sort moves based on moveType (decreasing) and return this
        return allMoves;
    }

    private void getPossibleMove(Space space, int playerId)
    {
        // Get coords and dir 
        int[] coords = space.getCoords();
        int dir = space.getPiece().getDir();

        /// GET STEPS AND CAPTURES
        getStepAndCapture(coords, dir, playerId);

        /// GET RETREAT
        getRetreat(coords, dir, playerId);

        // GET SLIDES AND SHOOT
        getSlideAndShoot(space, coords, playerId);
    }

    // Step and Capture merged, because we only have to loop one time (instead of two)
    void getStepAndCapture(int[] coords, int dir, int playerId)
    {
        // Check if forward isn't out of board
        if (coords[1] + dir < n && coords[1] + dir >= 0)
        {
            /// GET STEPS
            // Check if "to the left" isn't out of board and the Space is free
            if (coords[0] - 1 >= 0 && !this.Spaces[coords[0] - 1, coords[1] + dir].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0] - 1, coords[1] + dir, Move.moveType.step));
            }
            // Check if "to the right" isn't out of board and the Space is free
            if (coords[0] + 1 < n && !this.Spaces[coords[0] + 1, coords[1] + dir].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0] + 1, coords[1] + dir, Move.moveType.step));
            }
            // Check if the Space forward is free
            if (!this.Spaces[coords[0], coords[1] + dir].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0], coords[1] + dir, Move.moveType.step));
            }
        }

        /// GET SOLDIER CAPTURES
        // For every capture direction
        for (int i = 0; i < this.numCaptures; i++)
        {
            // If there is a enemy piece on the captureDirections
            if (onBoard(coords[0] + captureDir[playerId - 1][i, 0], coords[1] + captureDir[playerId - 1][i, 1]) &&
                this.Spaces[coords[0] + captureDir[playerId - 1][i, 0], coords[1] + captureDir[playerId - 1][i, 1]].isOccupied() &&
                this.Spaces[coords[0] + captureDir[playerId - 1][i, 0], coords[1] + captureDir[playerId - 1][i, 1]].getPieceId() != playerId)
            {
                this.allMoves.Add(new Move(coords[0], coords[1],
                    coords[0] + captureDir[playerId - 1][i, 0],
                    coords[1] + captureDir[playerId - 1][i, 1],
                    Move.moveType.soldierCapture));
            }
        }
    }

    void getRetreat(int[] coords, int dir, int playerId)
    {
        /// GET RETREAT
        if (underAttack(coords, playerId))
        {
            // Check if retreat diagonal left back is allowed (no piece between, and not retreat space, on board)
            if (onBoard(coords[0] - 2, coords[1] + dir * -2) && !this.Spaces[coords[0] - 1, coords[1] + dir * -1].isOccupied() &&
                !this.Spaces[coords[0] - 2, coords[1] + dir * -2].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0] - 2, coords[1] + dir * -2, Move.moveType.retreat));
            }

            // Check if retreat diagonal right back is allowed (no piece between, and not retreat space, on board)
            if (onBoard(coords[0] + 2, coords[1] + dir * -2) && !this.Spaces[coords[0] + 1, coords[1] + dir * -1].isOccupied() &&
                !this.Spaces[coords[0] + 2, coords[1] + dir * -2].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0] + 2, coords[1] + dir * -2, Move.moveType.retreat));
            }
            // Check if retreat back is allowed (no piece between, and not retreat space, on board)
            if (onBoard(coords[0], coords[1] + dir * -2) && !this.Spaces[coords[0], coords[1] + dir * -1].isOccupied() &&
                !this.Spaces[coords[0], coords[1] + dir * -2].isOccupied())
            {
                this.allMoves.Add(new Move(coords[0], coords[1], coords[0], coords[1] + dir * -2, Move.moveType.retreat));
            }
        }
    }

    // Slide and shoot merged, because we only have to loop one time (instead of two)
    void getSlideAndShoot(Space space, int[] coords, int playerId)
    {
        /// If cannon
        if (space.pieceIsCannon())
        {
            List<int[]> cannonDir = space.getCannonDir();

            for (int i = 0; i < cannonDir.Count(); i++)
            {
                // If the Space after the cannon is free, a piece can be placed (or we can shoot)
                // Potential check (not needed, otherwise it wasn't a cannon)
                //this.Spaces[coords[0] + cDir[0], coords[1] + cDir[1]].getPieceId() == currentPlayer.getPlayerId() &&
                //this.Spaces[coords[0] + cDir[0] * 2, coords[1] + cDir[1] * 2].getPieceId() == currentPlayer.getPlayerId() &&
                if (onBoard(coords[0] + cannonDir[i][0] * 3, coords[1] + cannonDir[i][1] * 3) &&
                    !this.Spaces[coords[0] + cannonDir[i][0] * 3, coords[1] + cannonDir[i][1] * 3].isOccupied())
                {
                    /// GET CANNON SLIDE
                    this.allMoves.Add(new Move(coords[0], coords[1], coords[0] + cannonDir[i][0] * 3, coords[1] + cannonDir[i][1] * 3, Move.moveType.slide));

                    /// GET SHOOT
                    if (onBoard(coords[0] + cannonDir[i][0] * 4, coords[1] + cannonDir[i][1] * 4) &&
                        this.Spaces[coords[0] + cannonDir[i][0] * 4, coords[1] + cannonDir[i][1] * 4].isOccupied() &&
                        this.Spaces[coords[0] + cannonDir[i][0] * 4, coords[1] + cannonDir[i][1] * 4].getPieceId() != playerId)
                    {
                        // GET SMALL SHOOT
                        this.allMoves.Add(new Move(coords[0], coords[1], coords[0] + cannonDir[i][0] * 4, coords[1] + cannonDir[i][1] * 4, Move.moveType.shoot));
                    }

                    // GET SHOOT
                    if (onBoard(coords[0] + cannonDir[i][0] * 5, coords[1] + cannonDir[i][1] * 5) &&
                        !this.Spaces[coords[0] + cannonDir[i][0] * 4, coords[1] + cannonDir[i][1] * 4].isOccupied() &&
                        this.Spaces[coords[0] + cannonDir[i][0] * 5, coords[1] + cannonDir[i][1] * 5].isOccupied() &&
                        this.Spaces[coords[0] + cannonDir[i][0] * 5, coords[1] + cannonDir[i][1] * 5].getPieceId() != playerId)
                    {
                        // GET BIG SHOOT
                        this.allMoves.Add(new Move(coords[0], coords[1], coords[0] + cannonDir[i][0] * 5, coords[1] + cannonDir[i][1] * 5, Move.moveType.shoot));
                    }
                }
            }
        }
    }

    public int[] countControlAndDanger(int currentId)
    {
        // Initialse
        //int[] countCurrent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        //int[] countOpponent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        int[] townInDanger = new int[2];
        //List<int[]> countedSpacesCurrent = new List<int[]>();
        //List<int[]> countedSpacesOpponent = new List<int[]>();
        int opponentId = currentId == 1 ? 2 : 1;

        int[,] currentControl = new int[Board.n, Board.n];
        int[,] currentInDangerP = new int[Board.n, Board.n];
        int[,] currentInDangerS = new int[Board.n, Board.n];
        int[,] currentInDangerC = new int[Board.n, Board.n];
        int[,] opponentControl = new int[Board.n, Board.n];
        int[,] opponentInDangerP = new int[Board.n, Board.n];
        int[,] opponentInDangerS = new int[Board.n, Board.n];
        int[,] opponentInDangerC = new int[Board.n, Board.n];

        // Loop over all pieces
        for (int j = 0; j < this.PiecesCoords.Count(); j++)
        {
            // If current piece, check if it's already counted, if no -> count in control (based on pieceId) and check if opponent piece can be attacked (opponentId is under attack)
            if (this.Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getPieceId() == currentId)
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll it
                        currentControl[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPieceId() == opponentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                opponentInDangerP[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    opponentInDangerC[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;
                                else
                                    opponentInDangerS[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                            }
                            else
                            {
                                townInDanger[1] = 1;
                            }
                        }

                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll 
                        opponentControl[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPieceId() == currentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                currentInDangerP[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    currentInDangerC[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                                else
                                    currentInDangerS[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                            }
                            else
                            {
                                townInDanger[0] = 1;
                            }
                        }
                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                
                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return new int[] { currentControl.FullSum(), currentInDangerP.FullSum(), currentInDangerS.FullSum(), currentInDangerC.FullSum(), townInDanger[0],
            opponentControl.FullSum(), opponentInDangerP.FullSum(), opponentInDangerS.FullSum(), opponentInDangerC.FullSum(), townInDanger[1] };
    }

    public int[] countControlAndDanger2(int currentId)
    {
        // Initialse
        //int[] countCurrent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        //int[] countOpponent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        int[] townInDanger = new int[2];
        //List<int[]> countedSpacesCurrent = new List<int[]>();
        //List<int[]> countedSpacesOpponent = new List<int[]>();
        int opponentId = currentId == 1 ? 2 : 1;

        int[,] currentControl = new int[Board.n, Board.n];
        int[,] currentInDangerP = new int[Board.n, Board.n];
        int[,] currentInDangerS = new int[Board.n, Board.n];
        int[,] currentInDangerC = new int[Board.n, Board.n];
        int[,] opponentControl = new int[Board.n, Board.n];
        int[,] opponentInDangerP = new int[Board.n, Board.n];
        int[,] opponentInDangerS = new int[Board.n, Board.n];
        int[,] opponentInDangerC = new int[Board.n, Board.n];

        // Loop over all pieces
        for (int j = 0; j < this.PiecesCoords.Count(); j++)
        {
            // If current piece, check if it's already counted, if no -> count in control (based on pieceId) and check if opponent piece can be attacked (opponentId is under attack)
            if (this.Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getPieceId() == currentId)
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll it
                        currentControl[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPieceId() == opponentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                opponentInDangerP[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    opponentInDangerC[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;
                                else
                                    opponentInDangerS[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                            }
                            else
                            {
                                townInDanger[1] = 1;
                            }
                        }

                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll 
                        opponentControl[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPieceId() == currentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                currentInDangerP[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    currentInDangerC[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                                else
                                    currentInDangerS[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                            }
                            else
                            {
                                townInDanger[0] = 1;
                            }
                        }
                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Count how much pieces around the town are in control (townCoordsCurrent = tcc)
        // Current
        int aroundTownCurrent = 0, row = 0, aroundTownOpponent = 0;
        if (this.TownCoords[currentId - 1] != default(Coord))
        {
            Coord tcc = this.TownCoords[currentId - 1];
            row = tcc.y == 0 ? 1 : Board.n - 2;
            aroundTownCurrent = currentControl[tcc.x - 1, tcc.y] + currentControl[tcc.x + 1, tcc.y] + currentControl[tcc.x - 1, row] +
                currentControl[tcc.x, row] + currentControl[tcc.x + 1, row];
        }

        if (this.TownCoords[opponentId - 1] != default(Coord))
        {
            Coord tco = this.TownCoords[opponentId - 1];
            row = tco.y == 0 ? 1 : Board.n - 2;
            aroundTownOpponent = opponentControl[tco.x - 1, tco.y] + opponentControl[tco.x + 1, tco.y] + opponentControl[tco.x - 1, row] +
                opponentControl[tco.x, row] + opponentControl[tco.x + 1, row];
        }

        // Check for every coord that is out of reach, how many squares are selected. Substract this from them all
        int inControlUnreachableCurrent = 0;
        for (int i = 0; i < this.unreachableCoords.getPlayer(currentId).Count(); i++)
        {
            inControlUnreachableCurrent += currentControl[this.unreachableCoords.getPlayer(currentId)[i].x, this.unreachableCoords.getPlayer(currentId)[i].y];
        }

        int inControlUnreachableOpponent = 0;
        for (int i = 0; i < this.unreachableCoords.getPlayer(opponentId).Count(); i++)
        {
            inControlUnreachableOpponent += opponentControl[this.unreachableCoords.getPlayer(opponentId)[i].x, this.unreachableCoords.getPlayer(opponentId)[i].y];
        }

        // TODO add to output
        // CurrentControl - inControlUnreachableCurrent

        return new int[] { currentControl.FullSum(), currentInDangerP.FullSum(), currentInDangerS.FullSum(), currentInDangerC.FullSum(), townInDanger[0],
            currentControl.FullSum() - inControlUnreachableCurrent, aroundTownCurrent,
            opponentControl.FullSum(), opponentInDangerP.FullSum(), opponentInDangerS.FullSum(), opponentInDangerC.FullSum(), townInDanger[1],
            opponentControl.FullSum() - inControlUnreachableOpponent, aroundTownOpponent};
    }

    public features countControlAndDanger3(int currentId, features fts)
    {
        // Initialse
        //int[] countCurrent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        //int[] countOpponent = new int[4]; // Control, InDangerPieces, InDangerSoldier, InDangerCannon
        int[] townInDanger = new int[2];
        //List<int[]> countedSpacesCurrent = new List<int[]>();
        //List<int[]> countedSpacesOpponent = new List<int[]>();
        int opponentId = currentId == 1 ? 2 : 1;

        int[,] currentControl = new int[Board.n, Board.n];
        int[,] currentInDangerP = new int[Board.n, Board.n];
        int[,] currentInDangerS = new int[Board.n, Board.n];
        int[,] currentInDangerC = new int[Board.n, Board.n];
        int[,] opponentControl = new int[Board.n, Board.n];
        int[,] opponentInDangerP = new int[Board.n, Board.n];
        int[,] opponentInDangerS = new int[Board.n, Board.n];
        int[,] opponentInDangerC = new int[Board.n, Board.n];

        // Loop over all pieces
        for (int j = 0; j < this.PiecesCoords.Count(); j++)
        {
            // If current piece, check if it's already counted, if no -> count in control (based on pieceId) and check if opponent piece can be attacked (opponentId is under attack)
            if (this.Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getPieceId() == currentId)
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll it
                        currentControl[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPieceId() == opponentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                opponentInDangerP[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    opponentInDangerC[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;
                                else
                                    opponentInDangerS[this.PiecesCoords[j].x + captureDir[currentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[currentId - 1][i, 1]] = 1;

                            }
                            else
                            {
                                townInDanger[1] = 1;
                            }
                        }

                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                currentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == opponentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        opponentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            opponentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            opponentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[1] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // SoldierCaptures
                for (int i = 0; i < this.numCaptures; i++)
                {
                    // If there is a player piece on the captureDirections
                    if (onBoard(this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]))
                    {
                        // If we can acces it, we can controll 
                        opponentControl[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                        if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPieceId() == currentId)
                        {
                            if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() != Piece.epieceType.town)
                            {
                                // If there is an enemy piece, their piece is in danger
                                currentInDangerP[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]].getPiece().getPieceType() == Piece.epieceType.cannon)
                                    currentInDangerC[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                                else
                                    currentInDangerS[this.PiecesCoords[j].x + captureDir[opponentId - 1][i, 0], this.PiecesCoords[j].y + captureDir[opponentId - 1][i, 1]] = 1;
                            }
                            else
                            {
                                townInDanger[0] = 1;
                            }
                        }
                    }
                }

                /// If cannon
                if (Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].pieceIsCannon())
                {
                    List<int[]> cannonDir = Spaces[this.PiecesCoords[j].x, this.PiecesCoords[j].y].getCannonDir();

                    for (int i = 0; i < cannonDir.Count(); i++)
                    {
                        // If the Space after the cannon is free, we can shoot
                        if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3) &&
                            !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 3, this.PiecesCoords[j].y + cannonDir[i][1] * 3].isOccupied())
                        {
                            /// GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4))
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;

                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }

                            // GET SHOOT
                            if (onBoard(this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5) &&
                                !this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 4, this.PiecesCoords[j].y + cannonDir[i][1] * 4].isOccupied())
                            {
                                opponentControl[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;

                                if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPieceId() == currentId)
                                {
                                    if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() != Piece.epieceType.town)
                                    {
                                        currentInDangerP[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        if (this.Spaces[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5].getPiece().getPieceType() == Piece.epieceType.cannon)
                                            currentInDangerC[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                        else
                                            currentInDangerS[this.PiecesCoords[j].x + cannonDir[i][0] * 5, this.PiecesCoords[j].y + cannonDir[i][1] * 5] = 1;
                                    }
                                    else
                                    {
                                        townInDanger[0] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        fts.InControl = currentControl.FullSum() - opponentControl.FullSum();
        fts.InDangerPiece = currentInDangerP.FullSum() - opponentInDangerP.FullSum();
        fts.InDangerSoldier = currentInDangerS.FullSum() - opponentInDangerS.FullSum();
        fts.InDangerCannon = currentInDangerC.FullSum() - opponentInDangerC.FullSum();
        fts.InDangerTown = townInDanger[0] - townInDanger[1];

        return fts;
    }


    bool underAttack(int[] coords, int playerId)
    {

        for (int i = 0; i < adjDir.GetLength(0); i++)
        {
            // If there is a enemy soldier adjacent
            if (onBoard(coords[0] + adjDir[i, 0], coords[1] + adjDir[i, 1]) &&
                this.Spaces[coords[0] + adjDir[i, 0], coords[1] + adjDir[i, 1]].isOccupied() &&
                this.Spaces[coords[0] + adjDir[i, 0], coords[1] + adjDir[i, 1]].getPieceId() != playerId &&
                this.Spaces[coords[0] + adjDir[i, 0], coords[1] + adjDir[i, 1]].getPieceType() == Piece.epieceType.soldier)
            {
                return true;
            }
        }

        return false;
    }

    bool onBoard(int x, int y)
    {
        return x >= 0 && x < n && y >= 0 && y < n;
    }

    public List<Coord> getPossiblePlacements(int playerId)
    {
        List<Coord> placements = new List<Coord>();
        int y = playerId == 1 ? 0 : n - 1;
        for (int x = 1; x < n-1; x++)
        {
            placements.Add(new Coord(x, y));
        }

        return placements;
    }

    public void movePiece(Move move, bool print, bool placeTown, bool realMove, bool isTown)
    {
        // Update hash
        //Console.WriteLine("Make Move");
        //Console.WriteLine(this.currentHash);
        this.currentHash = this.zH.makeMoveHash(this.currentHash, move, this.currentPlayer.getPlayerId(), isTown);
        //Console.WriteLine(this.currentHash);Console.WriteLine();

        // Take the piece (on from)
        Piece p = this.Spaces[move.From.x, move.From.y].getPiece();

        // Check if Town gets removed (if so, change jacked array)
        if (!placeTown && (move.type == Move.moveType.soldierCapture || move.type == Move.moveType.shoot) &&
            this.Spaces[move.To.x, move.To.y].getPiece().getPieceType() == Piece.epieceType.town)
        {
            int enemyId = this.currentPlayer.getPlayerId() == 1 ? 2 : 1;
            TownCoords[enemyId - 1] = default(Coord);
        }

        // Check if the move is a capture or shoot (if so, remove piece on to space)
        if (move.type == Move.moveType.soldierCapture || move.type == Move.moveType.shoot)
        {
            this.Spaces[move.To.x, move.To.y].removePiece();
            this.PiecesCoords.Remove(new Coord(move.To.x, move.To.y));
        }

        // If we didn't shoot, move our piece
        if (move.type != Move.moveType.shoot)
        {
            // Remove on from space
            this.Spaces[move.From.x, move.From.y].removePiece();
            this.PiecesCoords.Remove(new Coord(move.From.x, move.From.y)) ;

            // Add on to space
            this.Spaces[move.To.x, move.To.y].setPiece(p);
            this.PiecesCoords.Add(new Coord(move.To.x, move.To.y));
        }

        // Keep track of the visited states
        updateVisitedState(this.currentHash);

        // If it is a real move, add it to the list (to be able to do undo move)
        if (realMove)
        { 
            this.moveList.Add(move);
            //this.visitedStates.Keys.ToList().ForEach(x => Console.Write($"{x} ")); Console.WriteLine();
        }

        // Print results
        if (print)
        {
            printMove(move);
            printNumberOfPieces();
            Console.WriteLine();
        }
    }

    public void UndoMove(Move move, bool placeTown, bool isTown)
    {
        //Console.WriteLine("Undo Move");
        //Console.WriteLine(this.currentHash);
        // Remove visit of that board
        removeVisitedState(this.currentHash);

        // Update currentHash
        this.currentHash = this.zH.undoMoveHash(this.currentHash, move, this.currentPlayer.getPlayerId(), isTown);
        //Console.WriteLine(this.currentHash); Console.WriteLine();

        // Replace piece
        Piece.epieceType pType = Piece.epieceType.soldier;

        // Check if Town gets removed (if so, change jacked array)
        int enemyId = this.currentPlayer.getPlayerId() == 1 ? 2 : 1;
        if (!placeTown && (move.type == Move.moveType.soldierCapture || move.type == Move.moveType.shoot) &&
            TownCoords[enemyId - 1] == default(Coord))
        {
            TownCoords[enemyId - 1] = new Coord(move.To.x, move.To.y) ;
            pType = Piece.epieceType.town;
        }

        // If we didn't shoot, move our piece
        if (move.type != Move.moveType.shoot)
        {
            Piece p = this.Spaces[move.To.x, move.To.y].getPiece();

            // Add on from space
            this.Spaces[move.From.x, move.From.y].setPiece(p);
            this.PiecesCoords.Add(new Coord(move.From.x, move.From.y));

            // Remove on to space
            this.Spaces[move.To.x, move.To.y].removePiece();
            this.PiecesCoords.Remove(new Coord(move.To.x, move.To.y));
        }

        // Check if the move is a capture or shoot (if so, add piece on to space)
        if (move.type == Move.moveType.soldierCapture || move.type == Move.moveType.shoot)
        {
            this.Spaces[move.To.x, move.To.y].setPiece(enemyId, pType);
            
            if (pType == Piece.epieceType.soldier)
                this.PiecesCoords.Add(new Coord(move.To.x, move.To.y));
        }
    }

    public void undoMoveList(Player playerOne, Player playerTwo)
    {
        switchPlayer(playerOne, playerTwo);
        UndoMove(this.moveList[this.moveList.Count() - 1], false, false);
        switchPlayer(playerOne, playerTwo);
        UndoMove(this.moveList[this.moveList.Count() - 2], false, false);

        this.moveList.RemoveAt(this.moveList.Count() - 1);
        this.moveList.RemoveAt(this.moveList.Count() - 1);
    }

    public void placeTown(Coord placement, bool print)
    {
        // Place town and save location
        this.Spaces[placement.x, placement.y].setPiece(currentPlayer.getPlayerId(), Piece.epieceType.town);
        this.TownCoords[currentPlayer.getPlayerId() - 1] = placement;
        this.determineUnreachableCoords(placement);

        if (print)
        {
            printPlacement(placement);
            printNumberOfPieces();
            Console.WriteLine();
        }
    }

    public void removeTown(Coord placement)
    {
        // Remove town and remove location
        this.Spaces[placement.x, placement.y].removePiece();
        this.TownCoords[currentPlayer.getPlayerId() - 1] = default(Coord);
        this.unreachableCoords.Clear(currentPlayer.getPlayerId());
    }

    void determineUnreachableCoords(Coord placement)
    {
        // Determine the coords, where a soldier can't reach the town anymore (by only using steps)
        // First for left side, then for right side
        int row = placement.y;
        int col = placement.x - 1;
        while (col > 0)
        {
            for (int i = 0; i < col; i++)
            {
                this.unreachableCoords.Add(new Coord(i, row), this.currentPlayer.getPlayerId() == 1 ? 2 : 1);
            }

            row += this.currentPlayer.getPlayerId() == 1 ? 1 : -1;
            col--;
        }

        // Right side
        row = placement.y;
        col = placement.x + 2;
        while (col < Board.n)
        {
            for (int i = col; i < Board.n; i++)
            {
                this.unreachableCoords.Add(new Coord(i, row), this.currentPlayer.getPlayerId() == 1 ? 2 : 1);
            }

            row += this.currentPlayer.getPlayerId() == 1 ? 1 : -1;
            col++;
        }
    }

    public bool TownInGame(int currentPlayerId)
    {
        return this.TownCoords[currentPlayerId - 1] != default(Coord);
    }
    public bool TownsInGame()
    {
        return this.TownInGame(1) && this.TownInGame(2);
    }

    void printMove(Move move)
    {
        if (move.type == Move.moveType.step || move.type == Move.moveType.slide || move.type == Move.moveType.retreat)
            Console.WriteLine($"Player {this.currentPlayer.getPlayerId()}: {coordsToName(move.From.x, move.From.y)}-{coordsToName(move.To.x, move.To.y)}");
        else if (move.type == Move.moveType.soldierCapture)
            Console.WriteLine($"Player {this.currentPlayer.getPlayerId()}: {coordsToName(move.From.x, move.From.y)}x{coordsToName(move.To.x, move.To.y)}");
        else if (move.type == Move.moveType.shoot)
            Console.WriteLine($"Player {this.currentPlayer.getPlayerId()}: x{coordsToName(move.To.x, move.To.y)}");
    }

    void printPlacement(Coord placement)
    {
        Console.WriteLine($"Player {this.currentPlayer.getPlayerId()}: {coordsToName(placement.x, placement.y)}");
    }

    void printNumberOfPieces()
    {
        // Start on 1 1 for the town
        int[] count = new int[2] { 1, 1 };
        for (int i = 0; i < PiecesCoords.Count(); i++)
        {
            count[this.Spaces[PiecesCoords[i].x, PiecesCoords[i].y].getPieceId() - 1]++;
        }

        Console.WriteLine($"Player 1: {count[0]}, Player 2: {count[1]}.");
    }

    void generateFEN()
    {
        this.FEN = "";
        for (int y = 0; y < Board.n; y++)
        {
            int count = 0;
            for (int x = 0; x < Board.n; x++)
            {
                if (this.Spaces[x, y].isOccupied())
                {
                    if (this.Spaces[x,y].getPieceId() == 1)
                    {
                        if (this.Spaces[x, y].getPieceType() != Piece.epieceType.town)
                        {
                            if (count > 0)
                            {
                                this.FEN += $"{count}S";
                                count = 0;
                            }
                            else
                            {
                                this.FEN += $"S";
                            }
                        }
                        else
                        {
                            if (count > 0)
                            {
                                this.FEN += $"{count}T";
                                count = 0;
                            }
                            else
                            {
                                this.FEN += $"T";
                            }
                        }
                    }
                    else
                    {
                        if (this.Spaces[x, y].getPieceType() != Piece.epieceType.town)
                        {
                            if (count > 0)
                            {
                                this.FEN += $"{count}s";
                                count = 0;
                            }
                            else
                            {
                                this.FEN += $"s";
                            }
                        }
                        else
                        {
                            if (count > 0)
                            {
                                this.FEN += $"{count}t";
                                count = 0;
                            }
                            else
                            {
                                this.FEN += $"t";
                            }
                        }
                    }
                }
                else
                {
                    count++;
                }
            }

            if (count > 0)
                this.FEN += $"{count}/";
            else
                this.FEN += "/";
        }

        this.FEN += $"{(this.currentPlayer.getPlayerId() == 1 ? 2 : 1)}";
    }

    public void saveFEN()
    {
        // Generate the FEN
        generateFEN();

        // Save it to a file
        using (var sw = File.CreateText("FEN.txt"))
        {
            sw.Write(this.FEN);
        }
    }

    public void generateBoardFromFEN(Player playerOne, Player playerTwo)
    {
        // Load the FEN from the file
        using (StreamReader sr = File.OpenText("FEN.txt"))
        {
            this.FEN = sr.ReadLine();
        }

        Console.WriteLine(this.FEN);

        // Create Spaces
        createSpaces();

        // Set num of captures for getPossibleMoves() (captures and retreat);
        this.numCaptures = captureDir[0].GetLength(0);

        // Set pieces on correct position
        int i = 0;
        int x = -1, y = 0;
        while (y < Board.n)
        {
            if (this.FEN[i] == 'S')
            {
                x++;
                this.Spaces[x, y].setPiece(1, Piece.epieceType.soldier);
                PiecesCoords.Add(new Coord(x, y));
            }
            else if (this.FEN[i] == 's')
            {
                x++;
                this.Spaces[x, y].setPiece(2, Piece.epieceType.soldier);
                PiecesCoords.Add(new Coord(x, y));
            }
            else if (this.FEN[i] == 'T')
            {
                x++;
                this.currentPlayer = playerOne;
                placeTown(new Coord(x, y), false);
            }
            else if (this.FEN[i] == 't')
            {
                x++;
                this.currentPlayer = playerTwo;
                placeTown(new Coord(x, y), false);
            }
            else if (this.FEN[i] == '/')
            {
                x = -1;
                y++;
            }
            else if (i+1 < this.FEN.Length && this.FEN[i] == '1' && this.FEN[i+1] == '0')
            {
                x += 10;
                i++;
            }
            else
            {
                x += Convert.ToInt32(this.FEN[i].ToString());
            }

            i++;
        }

        // Determine cannons
        determineCannons();

        // Set correct player
        if (this.FEN[i] == '1')
            this.currentPlayer = playerOne;
        else
            this.currentPlayer = playerTwo;

        // Can't adjust time anymore
        playerOne.adjustTime = false;
        playerTwo.adjustTime = false;

        // Get Hash
        this.currentHash = this.zH.generateBoardHash(this);
        updateVisitedState(this.currentHash);
    }

    public void updateVisitedState(ulong hash)
    {
        // Always save from perspective of same player, because this is the same board (three fold doesn't care about player turn)
        if (this.currentPlayer.getPlayerId() == 2)
            hash = this.zH.switchPlayer(hash);

        //Console.WriteLine($"Add: {hash}");
        if (this.visitedStates.ContainsKey(hash))
            this.visitedStates[hash]++;
        else
            this.visitedStates.Add(hash, 1);
    }

    public void removeVisitedState(ulong hash)
    {
        // Always save from perspective of same player, because this is the same board (three fold doesn't care about player turn)
        if (this.currentPlayer.getPlayerId() == 2)
            hash = this.zH.switchPlayer(hash);

        //Console.WriteLine($"Remove: {hash}");
        this.visitedStates[hash]--;

        if (this.visitedStates[hash] == 0)
            this.visitedStates.Remove(hash);
    }

    public int getMaxFolds()
    {
        return this.visitedStates.Values.Max();
    }

    public void switchPlayer(Player playerOne, Player playerTwo)
    {
        // Update hash
        this.currentHash = this.zH.switchPlayer(this.currentHash);

        // Switch players
        if (this.currentPlayer.getPlayerId() == playerOne.getPlayerId())
            this.currentPlayer = playerTwo;
        else
            this.currentPlayer = playerOne;
    }

    //public void setCurrentHash(ulong hash)
    //{
    //    this.currentHash = hash;
    //    updateVisitedState(hash);
    //}

    public ulong getCurrentHash()
    {
        return this.currentHash;
    }

    public ulong getCurrentHashKey()
    {
        return this.zH.getHashKey(this.currentHash);
    }

    public ulong getCurrentHashValue()
    {
        return this.zH.getHashValue(this.currentHash);
    }
}
