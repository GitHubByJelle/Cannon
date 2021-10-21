using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TT
{
    class TTEntry
    {
        public enum flag
        {
            exact,
            lowerBound,
            upperBound
        }

        // Currently using new, no check needed on sameIdentification
        public int value = -1000;
        public flag type = flag.exact;
        public Move bestMove = default(Move);
        public float depth = -1;
        public ulong identification;

        public void setEntry(int value, flag type, Move bestMove, int depth, ulong identification)
        {
            this.value = value;
            this.type = type;
            this.bestMove = bestMove;
            this.depth = depth;
            this.identification = identification;
        }

        public void setEntry(int value, flag type, Move bestMove, float depth, ulong identification)
        {
            this.value = value;
            this.type = type;
            this.bestMove = bestMove;
            this.depth = depth;
            this.identification = identification;
        }

        bool sameIdentification(ulong identification)
        {
            if (this.depth == -1)
                return this.identification == identification;
            else
                return true;
        }
    }

    class TranspositionTable
    {
        TTEntry[] Table;
        int numBits;
        TTEntry defaultEntry = new TTEntry();

        public TranspositionTable(int numBits)
        {
            this.numBits = numBits;
            createTable(numBits);
        }

        void createTable(int numBits)
        {
            Table = new TTEntry[1 << numBits];

            for (int i = 0; i < (1 << numBits); i++)
            {
                Table[i] = new TTEntry();
            }
        }

        public TTEntry retrieve(ulong hashKey, ulong hashValue)
        {
            return this.Table[hashKey].identification == hashValue ? this.Table[hashKey] : this.defaultEntry;
        }

        public void setEntry(ulong hashKey, int value, TTEntry.flag type, Move bestMove, int depth, ulong hashValue)
        {
            this.Table[hashKey].setEntry(value, type, bestMove, depth, hashValue);
        }

        public void setEntry(ulong hashKey, int value, TTEntry.flag type, Move bestMove, float depth, ulong hashValue)
        {
            this.Table[hashKey].setEntry(value, type, bestMove, depth, hashValue);
        }

        public void reset()
        {
            for (int i = 0; i < (1 << this.numBits); i++)
            {
                Table[i] = new TTEntry();
            }
        }
    }
}
