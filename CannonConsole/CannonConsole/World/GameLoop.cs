using System;
using System.Collections.Generic;
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

        public GameLoop()
        {
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
            //playerOne = new NegaMaxAlphaBetaTT3(1, 3, 20);
            //playerOne = new IterativeDeepeningSimple(1, 5000, 20, true);
            //playerOne = new IterativeDeepening2(1, 5000, 20, true);
            //playerOne = new IterativeDeepeningHH(1, 5000, 20, true);
            //playerOne = new IterativeDeepeningNegaScout(1, 5000, 20, true);
            //playerOne = new xIterativeDeepeningOrdered(1, 5000, 20, true);
            //playerOne = new xIterativeDeepening(1, 1000, true);
            //playerOne = new xIterativeDeepeningAS(1, 5000, 20, 11, true);
            //playerOne = new xIterativeDeepeningNM(1, 5000, 20, 11, 2, true);
            //playerOne = new xIterativeDeepeningNMMC(1, 5000, 20, 11, 2, 5, 10, true);
            //playerOne = new xIterativeDeepeningFullNSLast(1, 1000, 20, 2, 5, 10, true);
            //playerOne = new xIterativeDeepeningFullNSLast(1, 5000, 20, 2, 5, 10, true);
            //playerOne = new xIterativeDeepeningFullNSLast(1, 5000, 20, 2, 5, 10, true);
            //playerOne = new xIterativeDeepeningFullASLast(1, 10, 20, 11, 2, 5, 10, false);
            playerOne = new xIterativeDeepeningFullASLast(1, 5000, 20, 11, 2, 5, 10, true);
            //playerOne = new xIterativeDeepeningOrdered(1, 5000, 20, true);
            //playerOne = new xIterativeDeepeningAS(1, 5000, 20, 11, true);
            //playerOne = new RandomBot(1);
            //playerOne = new cIterativeDeepeningFullNSLast(1, 1000, 20, 2, 5, 10, true);
            //playerOne = new NegaMaxAlphaBetaTT(1, 4, 20);
            //playerOne = new NegaMax2(1, 3);
            //playerOne = new RandomBot(1);
            //playerOne = new NegaMaxAlphaBetaTT(1, 3, 20);
            playerTwo = new xIterativeDeepeningFullASLast(2, 5000, 20, 11, 2, 5, 10, true);
            //playerTwo = new xIterativeDeepeningFullNSLast(2, 5000, 20, 2, 5, 10, true);
            //playerOne = new IterativeDeepeningPlus(1, 4000, 20, true);
            //playerTwo = new IterativeDeepeningHH(2, 5000, 20, true);
            //playerTwo = new IterativeDeepening(2, 20000, 20, true);
            //playerTwo = new xIterativeDeepening(2, 1000, true);
            //playerTwo = new RandomBot(2);
            //playerTwo = new xIterativeDeepeningFullASLast(2, 5000, 20, 11, 2, 5, 10, true);
            //playerTwo = new Human(2);
            //playerTwo = new xIterativeDeepeningFullNSLast(2, 5000, 20, 2, 5, 10, true);
            //playerTwo = new IterativeDeepeningSimple(2, 5000, 20, true);
            //playerTwo = new NegaMaxAlphaBetaTT3(2, 3, 20);
            //playerTwo = new NegaMaxAlphaBeta(2, 2);
            //playerTwo = new IterativeDeepening(2, 2000, 20, true);

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
                "2) RandomBot(Easy).\n" +
                "3) Iterative Bot.\n" +
                "4) Iterative Bot optimized.\n");
        }

        Player loadPlayer(int n, int id)
        {
            if (n == 1)
                return new Human(id);
            else if (n == 2)
                return new RandomBot(id);
            else if (n == 3)
                //return new xIterativeDeepening(id, 5000, true);
                return new RandomBot(id);
            else
                return new xIterativeDeepeningFullASLast(id, 5000, 20, 11, 2, 5, 10, true);

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
