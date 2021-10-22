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
            //GA GenAlg = new GA(1, 16, 11, 10);
            //GenAlg.run(100, 3);

            //Console.ReadLine();

            //GameLoop GL = new GameLoop();
            //Board B = new Board();

            //B.getRandomBoard(10, 3, true, true);
            //B.printBoard();

            //GL.getPlayerTwo().printFeatures(B);
            //GL.getPlayerTwo().printFeatures3(B);

            //Console.ReadLine();

            GameLoop GL = new GameLoop(true);

            GL.playGame(true);

            Console.ReadLine();

            //var watch = System.Diagnostics.Stopwatch.StartNew();

            //GL.playGames(1000,false);

            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;

            ////GL.getBoard().printBoard();

            //Console.WriteLine($"Elapsed Time [ms]: {elapsedMs}.");

            //Console.WriteLine("\nPress any key to leave");
            //Console.ReadLine();
        }
    }
}
