using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace World
{
    class GameLoop
    {
        Board B;
        Player playerOne;
        Player playerTwo;
        int maxPly = 1000;
        bool startPhaseDone = false;

        public GameLoop(bool centralLoop)
        {
            if (centralLoop)
                setup();
        }

        public Board getBoard()
        {
            return this.B;
        }

        public Player getPlayerOne()
        {
            return this.playerOne;
        }

        public Player getPlayerTwo()
        {
            return this.playerTwo;
        }

        public void setup()
        {
            // Setup board
            B = new Board();
            B.setup();

            // Set players
            //playerOne = new Human(1);
            //playerOne = new RandomBot(1);
            //playerOne = new OptimizedAS(1, 5000, 20, 11, 2, 5, 10, true);
            playerOne = new OptimizedASAdjust(1, 5*60*1000, 1000, 5, .05f, 20, 11, 2, 5, 10, true);
            //playerOne = new OptimizedASBH(1, 5000, 20, 11, 2, 3, 10, true);
            //playerOne = new OrderedPVS(1, 5000, 20, true);
            //playerOne = new ID_TT_KM(1, 5000, 20, true);
            //playerOne = new IterativeDeepening(1, 5000, true);
            //playerTwo = new AS_FP_NL(2, 5000, 20, 11, 2, true);
            //playerTwo = new OptimizedASAdjust(2, 5 * 60 * 1000, 1000, 5, .05f, 20, 11, 2, 5, 10, true);
            //playerTwo = new OrderedAS(2, 5000, 20, 11, true);
            //playerTwo = new OrderedID(2, 1000, 20, true);
            //playerTwo = new Temp(2, 5000, true);
            playerTwo = new Temp2(2, 5 * 60 * 1000, 1000, 5, .05f, 20, 11, 2, 5, 10, true);
            //playerTwo = new ID_TT(2, 5000, 20, true);
            //playerTwo = new HeuristicBot(2, 0f);
            //playerTwo = new Human(2);
            //playerTwo = new OptimizedAS(2, 1000, 20, 11, 2, 5, 10, true);

            // Set currentplayer
            B.setCurrentPlayer(playerOne);
            B.createInitialHash();
        }

        public void setupPlayer(int playerOneIdSelection, int playerTwoIdSelection)
        {
            // Setup board
            B = new Board();
            B.setup();

            playerOne = loadPlayer(playerOneIdSelection, 1, 1000);
            playerTwo = loadPlayer(playerTwoIdSelection, 2, 1000);

            Console.WriteLine($"Now: {playerOne.GetType().Name} vs {playerTwo.GetType().Name}.");

            // Set currentplayer
            B.setCurrentPlayer(playerOne);
            B.createInitialHash();
        }

        void setupGUI()
        {
            // Ask to start
            bool reload = askStart();

            // Create a board
            this.B = new Board();

            // Ask which players to use
            int[] players = askPlayers();

            // Assign player
            playerOne = loadPlayer(players[0], 1);
            playerTwo = loadPlayer(players[1], 2);

            // Create new board or reload from previous game
            if (reload)
            {
                this.B.generateBoardFromFEN(playerOne, playerTwo);
                this.startPhaseDone = true;
            }
            else
            {
                this.B.setup();
                B.setCurrentPlayer(playerOne);
                B.createInitialHash();
            }

        }

        void setPlayer(int id, Player player)
        {
            if (id == 1)
                playerOne = player;
            else
                playerTwo = player;
        }

        private bool askStart()
        {
            Console.WriteLine(
                "Welcome!\n\n" +
                "Press Enter to start a new game, or type 'reload' to get the board from last game.\n");
            string outp = Console.ReadLine();

            if (outp == "reload")
                return true;
            else
                return false;
        }

        public void collectPowerPointData(int numOfGames, int[,] selectedPlayerIds)
        {
            // Create file for saving the data
            File.Create("powerpointdata.txt").Dispose();

            for (int i = 0; i < selectedPlayerIds.GetLength(0); i++)
            {
                playGamesPowerPoint(numOfGames, false, selectedPlayerIds[i,0], selectedPlayerIds[i,1]);

                Console.WriteLine($"{i + 1} of {selectedPlayerIds.GetLength(0)} completed.");
            }
        }

        private int[] askPlayers()
        {
            // Print intro text
            printIntro();

            // Receive player answers
            int[] players = new int[2];
            Console.Write("\nPlayer 1: ");
            players[0] = Convert.ToInt32(Console.ReadLine());
            Console.Write("Player 2: ");
            players[1] = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine();

            // Give instruction for the Human player
            if (players[0] == 1 || players[1] == 1)
                printHumanInstructions();

            return players;
        }

        private void printHumanInstructions()
        {
            Console.WriteLine("You selected Human!\n" +
                "Entering the moves will work as follows:\n" +
                "For a step or slide: A1-B2\n" +
                "For a capture: A1xB2\n" +
                "For a shot: xB2\n" +
                "For placing the town: A2\n" +
                "To undo a move: undo\n");
        }

        private void printIntro()
        {
            Console.WriteLine(
                "To start the game choose two players, by entering the integer in front of the options.\n\n" +
                "Options:\n" +
                "1) Human.\n" +
                "2) RandomBot (Easy).\n" +
                "3) HeuristicBot (Epsilon greedy)\n" +
                "4) Iterative Bot (no ordering)" +
                "5) Iterative Bot (ordering on knowledge).\n" +
                "6) Iterative Bot with TT.\n" +
                "7) Iterative Bot with TT and KM.\n" +
                "8) Ordered Iterative Bot (TT, KM and HH).\n" +
                "9) Principal Variation Search / NegaScout.\n" +
                "10) Aspiration Search.\n" +
                "11) Aspiration Search with Fractional Plies and Null Moves.\n" +
                "12) Aspiration Search (Variable Depth = Fractional Plies, Null Move and Multi-Cut).\n" +
                "13) Aspiration Search using Monte Carlo Evaluation to adjust time (Variable Depth).\n");
        }

        Player loadPlayer(int n, int id)
        {
            if (n == 1)
                return new Human(id);
            else if (n == 2)
                return new RandomBot(id);
            else if (n == 3)
                return new HeuristicBot(id, .05f);
            else if (n == 4)
                return new IterativeDeepeningNoOrdering(id, 5000, true);
            else if (n == 5)
                return new IterativeDeepening(id, 5000, true);
            else if (n == 6)
                return new ID_TT(id, 5000, 20, true);
            else if (n == 7)
                return new ID_TT_KM(id, 5000, 20, true);
            else if (n == 8)
                return new OrderedID(id, 5000, 20, true);
            else if (n == 9)
                return new OrderedPVS(id, 5000, 20, true);
            else if (n == 10)
                return new OrderedAS(id, 5000, 20, 11, true);
            else if (n == 11)
                return new AS_FP_NL(id, 5000, 20, 11, 2, true);
            else if (n == 12)
                return new OptimizedAS(id, 5000, 20, 11, 2, 5, 10, true);
            else if (n == 13)
                return new OptimizedASAdjust(id, 10 * 60 * 1000, 12000, 10, .05f, 20, 11, 2, 5, 10, true);

            else
            {
                Console.WriteLine("That one didn't exist. Try again!");
                return new Human(id);
            }

        }

        Player loadPlayer(int n, int id, int timeSearch)
        {
            if (n == 1)
                return new Human(id);
            else if (n == 2)
                return new RandomBot(id);
            else if (n == 3)
                return new HeuristicBot(id, .05f);
            else if (n == 4)
                return new IterativeDeepeningNoOrdering(id, timeSearch, false);
            else if (n == 5)
                return new IterativeDeepening(id, timeSearch, false);
            else if (n == 6)
                return new ID_TT(id, timeSearch, 20, false);
            else if (n == 7)
                return new ID_TT_KM(id, timeSearch, 20, false);
            else if (n == 8)
                return new OrderedID(id, timeSearch, 20, false);
            else if (n == 9)
                return new OrderedPVS(id, timeSearch, 20, false);
            else if (n == 10)
                return new OrderedAS(id, timeSearch, 20, 11, false);
            else if (n == 11)
                return new AS_FP_NL(id, timeSearch, 20, 11, 2, false);
            else if (n == 12)
                return new OptimizedAS(id, timeSearch, 20, 11, 2, 5, 10, false);
            else if (n == 13)
                return new OptimizedASAdjust(id, 10 * 60 * 1000, timeSearch, 10, .05f, 20, 11, 2, 5, 10, false);

            else
            {
                Console.WriteLine("That one didn't exist. Try again!");
                return new Human(id);
            }

        }

        public void setupSimulate(int[] wghts, int updatePlayerId)
        {
            setup();

            if (updatePlayerId == 1)
                this.playerOne.setWeights(wghts);
            else
                this.playerTwo.setWeights(wghts);

            B.setCurrentPlayer(playerOne);
            B.createInitialHash();
        }

        public void setupRandomBoard(int numPiecesOne, int numPiecesTwo, bool TownOne, bool TownTwo)
        {
            // Setup board
            B = new Board();
            B.getRandomBoard(numPiecesOne, numPiecesTwo, TownOne, TownTwo);

            // Set players
            playerOne = new RandomBot(1);
            playerTwo = new RandomBot(2);

            // Set currentplayer
            B.setCurrentPlayer(playerOne);
            B.createInitialHash();
        }

        public void playGames(int numberOfGames, bool print)
        {
            for (int num = 0; num < numberOfGames; num++)
            {
                // Play a game
                playGame(print);

                // Set up the board again (for the next game)
                setup();
            }
        }

        public int[] playGamesOutput(int numberOfGames, bool print, int[] wghts, int updatePlayerId)
        {
            int[] wins = new int[3];
            for (int num = 0; num < numberOfGames; num++)
            {
                //this.B.printBoard();
                // Play a game
                int winner = playGame(print);

                wins[winner]++;

                //this.B.printBoard();

                // Set up the board again (for the next game)
                setupSimulate(wghts, updatePlayerId);
            }

            return wins;
        }

        public void playGamesPowerPoint(int numberOfGames, bool print, int playerOneSelection, int playerTwoSelection)
        {
            int[] wins = new int[3];
            for (int num = 0; num < numberOfGames; num++)
            {
                // Set up the board 
                setupPlayer(playerOneSelection, playerTwoSelection);

                // Play a game
                int winner = playGame(print);

                // Fill in win
                wins[winner]++;

                Console.WriteLine($"Game {num} of {numberOfGames} completed");
            }

            // Add win to file
            File.AppendAllText("powerpointdata.txt", $"{playerOne.GetType().Name} vs {playerTwo.GetType().Name}: {wins[0]} - {wins[1]} - {wins[2]}\n");
        }

        public void playGames(int numberOfGames, bool print, bool analyses)
        {
            int[] measures;
            int TotalPly = 0;
            int TotalBranches = 0;
            for (int num = 0; num < numberOfGames; num++)
            {
                // Play a game
                measures = playGame(print, analyses);

                // Set up the board again (for the next game)
                setup();

                // Keep track of measures
                TotalPly += measures[0];
                TotalBranches = checked(TotalBranches + measures[1]);
            }
            if (analyses)
            {
                Console.WriteLine("Average Game Length [plys] = " + Utils.formatString((double)TotalPly / numberOfGames));
                Console.WriteLine("Average Branches [branches/plys] = " + Utils.formatString((double)TotalBranches / TotalPly));
            }
        }

        public int playGame(bool print)
        {
            int ply = 0;

            if (print)
                B.printBoard();

            if (!this.startPhaseDone)
                startPhase(print);

            while (inGame() && ply < maxPly)
            {
                
                process(print);

                determineTimePerMove();

                ply++;
            }

            //Console.WriteLine($"Ply: {ply}");

            return getWinner(print);
        }

        int[] playGame(bool print, bool analyses)
        {
            int ply = 0;
            int branches = 0;

            if (print)
                B.printBoard();

            startPhase(print);

            while (inGame() && ply < maxPly)
            {
                branches += process(print, analyses);

                ply++;
            }

            int winnerPlayerId = getWinner(print);

            return new int[2] { ply, branches} ;
        }

        void startPhase(bool print)
        {
            for (int id = 0; id < 2; id++)
            {
                // Place Town
                this.B.getCurrentPlayer().placeTown(B, print, this.playerOne, this.playerTwo);

                // Update Cannons
                B.updateCannons();

                // Reset TT
                B.getCurrentPlayer().resetTT();

                // Switch players
                switchPlayer();

                // Print board (if wanted)
                if (print)
                {
                    B.printBoard();
                }
            }
        }

        void process(bool print)
        {
            // Make move
            B.getCurrentPlayer().makeMove(B, print, this.playerOne, this.playerTwo);

            // Update Cannons
            B.updateCannons();

            // Print board
            if (print)
                B.printBoard();

            // Save the FEN of the board (in case the game crashes)
            B.saveFEN();

            // Reset TT
            B.getCurrentPlayer().resetTT();

            // Switch player
            switchPlayer();
        }

        private int determinePlies(int num, float epsilon)
        { 
            int ply = 0;
            for (int i = 0; i < num; i++)
            {
                GameLoop temp_gl = new GameLoop(false);

                temp_gl.setPlayer(1, new HeuristicBot(1, epsilon));
                temp_gl.setPlayer(2, new HeuristicBot(2, epsilon));
                temp_gl.setBoard(this.B);

                while (temp_gl.inGame() && ply < maxPly)
                {
                    temp_gl.process(false);

                    ply++;
                }
            }

            return ply / num;
        }

        private void determineTimePerMove()
        {
            switchPlayer();

            if (this.B.getCurrentPlayer().getAdjustTime() && inGame())
            {
                this.B.getCurrentPlayer().adjustMaxTime(determinePlies(this.B.getCurrentPlayer().numberOfEstimations, this.B.getCurrentPlayer().epsilon));
            }

            switchPlayer();
        }

        private void setBoard(Board b)
        {
            this.B = new Board();
            this.B.setup();
            
            this.B.setCurrentPlayer(this.playerOne);
            this.B.createInitialHash();

            this.B.placeTown(b.getTownCoords()[0], false);
            this.switchPlayer();
            this.B.placeTown(b.getTownCoords()[1], false);
            this.switchPlayer();

            List<Move> allMoves = b.getMadeMoves();

            for (int i = 0; i < allMoves.Count(); i++)
            {
                this.B.movePiece(allMoves[i], false, false, true, false);
                this.switchPlayer();
            }
        }

        int process(bool print, bool analyses)
        {
            // Make move
            int branches = B.getCurrentPlayer().makeMove(B, print, analyses);

            // Update Cannons
            B.updateCannons();

            // Switch player
            switchPlayer();

            return branches;
        }

        public void switchPlayer()
        {
            B.switchPlayer(this.playerOne, this.playerTwo);
        }

        public bool inGame()
        {
            if (this.B.getCurrentPlayer().getPlayerId() == 1)
                return townsInGame() & this.playerTwo.LegalMoveLeft() & !threeFold();
            else
                return townsInGame() & this.playerOne.LegalMoveLeft() & !threeFold();
        }

        bool townsInGame()
        {
            return townInGame(1) && townInGame(2);
        }

        bool threeFold()
        {
            return this.B.getMaxFolds() == 3;
        }

        bool townInGame(int playerId)
        {
            return this.B.getTownCoords()[playerId - 1] != default(Coord);
        }

        int getWinner(bool print)
        {
            int winner;
            if (!townInGame(1) || !playerOne.LegalMoveLeft())
                winner = 2;
            else if (!townInGame(2) || !playerTwo.LegalMoveLeft())
                winner = 1;
            else
                // Threefold or to much moves
                winner = 0;

            if (print)
            {
                if (winner > 0)
                    Console.WriteLine($"PLAYER {winner} WON!");
                else
                    Console.WriteLine("IT IS A DRAW.");
            }

            return winner;
        }
    }
}
