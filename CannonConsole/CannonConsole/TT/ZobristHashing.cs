using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using World;

namespace TT
{
    class ZobristHashing
    {
        Random rnd = new Random(123);
        public ulong[,] r; // x: soldier player One, town player One, soldier player Two, town player Two
        ulong[] playerR; // (playerId << 1) - 2 + town 
        readonly ulong initialHash = 1234567890;
        int keyShift;
        ulong valueMask;

        public ZobristHashing(int lengthKey)
        {
            generateTable(4, Board.n * Board.n);
            this.valueMask = (ulong)0x7FFFFFFFFFFFFFFF >> (lengthKey - 1);
            this.keyShift = 64 - lengthKey;
        }

        void generateTable(int numPieces, int numPositions)
        {
            // Create random ulong for pieces
            this.r = new ulong[numPieces, numPositions];

            for (int i = 0; i < numPieces; i++)
            {
                for (int j = 0; j < numPositions; j++)
                {
                    this.r[i, j] = randomLong();
                }
            }

            // Create random ulong for players
            this.playerR = new ulong[2];

            for (int i = 0; i < 2; i++)
            {
                this.playerR[i] = randomLong();
            }
        }

        ulong randomLong()
        {
            byte[] bytes = new byte[8];
            this.rnd.NextBytes(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        public ulong generateBoardHash(Board B)
        {
            ulong hash = this.initialHash;

            // Create hash with pieces;
            for (int i = 0; i < B.getPiecesCoords().Count(); i++)
            {
                hash ^= this.r[pieceToIndex(B.getSpaces()[B.getPiecesCoords()[i].x, B.getPiecesCoords()[i].y].getPieceId(), Piece.epieceType.soldier), 
                    positionToIndex(B.getPiecesCoords()[i])];
            }

            // Add current player
            hash ^= this.playerR[B.getCurrentPlayer().getPlayerId() >> 1];

            return hash;
        }

        public ulong makeMoveHash(ulong oldHash, Move move, int currentPlayerId, bool isTown)
        {
            // If shoot or capture
            if (move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture)
            {
                // Calculate the correct Id
                int enemyPlayerId = currentPlayerId == 1 ? 2 : 1;
                int removePiece = isTown ? (enemyPlayerId << 1) - 2 + 1 : (enemyPlayerId << 1) - 2;

                // Remove enemy piece on to spot
                oldHash ^= this.r[removePiece, positionToIndex(move.To.x, move.To.y)];
            }

            // If not shot
            if (move.type != Move.moveType.shoot)
            {
                // Calculate correct Id
                int movePiece = (currentPlayerId << 1) - 2;

                // Remove current piece on from spot
                oldHash ^= this.r[movePiece, positionToIndex(move.From.x, move.From.y)];

                // Add piece on to spot
                oldHash ^= this.r[movePiece, positionToIndex(move.To.x, move.To.y)];
            }

            // Return result
            return oldHash;
        }

        public ulong undoMoveHash(ulong oldHash, Move move, int currentPlayerId, bool isTown)
        {
            // If not shot
            if (move.type != Move.moveType.shoot)
            {
                // Calculate correct Id
                int movePiece = (currentPlayerId << 1) - 2;

                // Remove piece on to spot
                oldHash ^= this.r[movePiece, positionToIndex(move.To.x, move.To.x)];

                // Add piece on from spot
                oldHash ^= this.r[movePiece, positionToIndex(move.From.x, move.From.y)];

            }

            // If shoot or capture
            if (move.type == Move.moveType.shoot || move.type == Move.moveType.soldierCapture)
            {
                // Determine removed piece
                int enemyPlayerId = currentPlayerId == 1 ? 2 : 1;
                int removedPiece = isTown ? (enemyPlayerId << 1) - 2 + 1 : (enemyPlayerId << 1) - 2;

                // Add enemy piece on to spot
                oldHash ^= this.r[removedPiece, positionToIndex(move.To.x, move.To.y)];
            }

            // Return result
            return oldHash;
        }

        // Every game, a new transposition table is created. Because the towns can't move, placing them on the board, won't make sense
        //public ulong placeTown(ulong oldHash, int[] position, int currentPlayerId)
        //{
        //    // Determine piece
        //    int placePiece = (currentPlayerId << 1) - 2 + 1;

        //    // Place town on position and return
        //    return (oldHash ^ this.r[placePiece, positionToIndex(position)]);
        //}

        //public ulong removeTown(ulong oldHash, int[] position, int currentPlayerId)
        //{
        //    // Determine piece
        //    int opponentId = currentPlayerId == 1 ? 2 : 1;
        //    int removePiece = (opponentId << 1) - 2 + 1;

        //    // Remove town on position and return
        //    return (oldHash ^ this.r[removePiece, positionToIndex(position)]);
        //}

        public ulong switchPlayer(ulong oldHash)
        {
            // Should be switched from the currentPlayerId, to the new playerId -> So always with both
            return oldHash ^ this.playerR[0] ^ this.playerR[1];
        }

        int positionToIndex(Coord position)
        {
            return position.x + Board.n * position.y;
        }

        int positionToIndex(int x, int y)
        {
            return x + Board.n * y;
        }

        int pieceToIndex(int pieceId, Piece.epieceType pType)
        {
            // [soldier p1, town p1, soldier p2, town p2]
            int town = pType == Piece.epieceType.town ? 1 : 0;
            return (pieceId << 1) - 2 + town; // (1 * 2) - 2 = 0, (2 * 2) - 2 = 2 
        }

        public ulong getHashKey(ulong value)
        {
            return value >> this.keyShift;
        }

        public ulong getHashValue(ulong value)
        {
            return value & this.valueMask;
        }
    }
}
