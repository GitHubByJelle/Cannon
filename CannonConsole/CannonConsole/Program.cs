using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using World;
using TT;
using EA;

namespace CannonConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            /// Genetic Algorithm
            //GA GenAlg = new GA(1, 16, 11, 10);
            //GenAlg.run(100, 3);

            //Console.ReadLine();

            /// Testing Features
            //GameLoop GL = new GameLoop();
            //Board B = new Board();

            //B.getRandomBoard(10, 3, true, true);
            //B.printBoard();

            //GL.getPlayerTwo().printFeatures(B);
            //GL.getPlayerTwo().printFeatures3(B);

            //Console.ReadLine();

            /// Collecting data playing games
            //GameLoop GL = new GameLoop(true);
            //int[,] selections = new int[,] { { 12, 14 }, {14, 12 }, {12, 15 }, {15, 12 } };
            //GL.collectPowerPointData(10, 1000, selections);

            /// Collecting data computation time
            //GameLoop GL = new GameLoop(true);
            //int[] selections = new int[] { 15 };
            //GL.collectPowerPointTimes(3 * 60 * 1000, selections);

            //Console.ReadLine();

            /// Play game
            GameLoop GL = new GameLoop(true);
            GL.playGame(true);
            Console.ReadLine();
        }
    }
}
