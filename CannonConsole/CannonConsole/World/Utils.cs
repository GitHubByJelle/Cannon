using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace World
{
    class Utils
    {
        public static string formatString(double val)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            return String.Format(val % 1 == 0 ? "{0:0}" : "{0:0.00}", val);
        }

        public static void printBits(ulong value)
        {
            Console.WriteLine(Convert.ToString((long)value, toBase: 2),64);
        }
    }

    public static class ExtensionMethods
    {
        public static int FindAndRemoveAll(this List<int[]> list, int[] items)
        {
            return list.RemoveAll(arr => arr.SequenceEqual(items));
        }

        public static List<int[]> getMatches(this List<int[]> list1, List<int[]> list2)
        {
            return list1.Where(x => list2.Any(y => x.SequenceEqual(y))).ToList();
        }

        public static bool ContainsArr(this List<int[]> list, int[] item)
        {
            return list.Any(arr => arr.SequenceEqual(item));
        }

        public static List<Move> orderByMoveType(this List<Move> list)
        {
            return list.OrderBy(x => -(int)x.type).ToList();
        }

        public static int argMax(this int[] arr)
        {
            return arr.ToList().IndexOf(arr.Max());
        }

        public static string toPrint(this int[] arr)
        {
            return $"[{string.Join(", ", arr)}]";
        }

        public static List<Move> cloneList (this List<Move> lst)
        {
            List<Move> newList = new List<Move>(lst.Count);

            lst.ForEach((item) =>
            {
                newList.Add((Move)item);
            });

            return newList;
        }

        public static void multiplyDiscount(this int[,,,] arr)
        {
            for (int i = 0; i < Board.n; i++)
            {
                for (int j = 0; j < Board.n; j++)
                {
                    for (int k = 0; k < Board.n; k++)
                    {
                        for (int l = 0; l < Board.n; l++)
                        {
                            arr[i, j, k, l] = arr[i, j, k, l] >> 4;
                        }
                    }
                }
            }
        }

        public static void setAll(this int[] arr, int value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        } 

        public static int FullSum(this int[,] arr)
        {
            int count = 0;
            for (int x = 0; x < arr.GetLength(0); x++)
            {
                for (int y = 0; y < arr.GetLength(1); y++)
                {
                    count += arr[x, y];
                }
            }

            return count;
        }

        public static int multiplyArr(this int[] arr, int[] arr2)
        {
            int count = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                count += arr[i] * arr2[i];
            }

            return count;
        }

        public static int getMax(this int[,,,] arr, List<Move> moves)
        {
            int max = 0;

            for (int i = 0; i < moves.Count(); i++)
            {
                if (arr[moves[i].From.x, moves[i].From.y, moves[i].To.x, moves[i].To.y] > max)
                {
                    max = arr[moves[i].From.x, moves[i].From.y, moves[i].To.x, moves[i].To.y];
                }
            }

            return max;
        }

        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
