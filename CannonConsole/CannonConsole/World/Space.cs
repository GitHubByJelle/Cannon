using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Space
{
    Piece p = null;

    string spaceName;
    int[] coords;
    int[] distEdge;

    public Space(string name, int x, int y)
    {
        this.spaceName = name;
        this.coords = new int[] { x, y };
        this.distEdge = new int[] { Board.n - 1 - y, Board.n - 1 - x, y, x }; // Up, right, down, left
    }

    public void setPiece(int id, Piece.epieceType pieceType)
    {
        if (this.p == null)
        {
            this.p = new Piece(id, id == 1 ? 1 : -1, pieceType);
        }
        else
            throw new NotImplementedException();
    }

    public void setPiece(Piece p)
    {
        if (this.p == null)
        {
            this.p = p;
        }
        else
            throw new NotImplementedException();
    }

    public void removePiece()
    {
        this.p = null;
    }

    public bool isOccupied()
    {
        return this.p != null;
    }

    public string getName()
    {
        return this.spaceName;
    }

    public Piece getPiece()
    {
        return this.p;
    }

    public int getPieceId()
    {
        return this.p == null ? 0 : this.p.getId();
    }

    public Piece.epieceType getPieceType()
    {
        return this.p.getPieceType();
    }

    public int[] getCoords()
    {
        return this.coords;
    }

    public int[] getDistEdge()
    {
        return this.distEdge;
    }

    public bool pieceIsCannon()
    {
        return this.p.isCannon();
    }

    //public void setCannon(bool cannon)
    //{
    //    this.cannon = cannon;
    //}

    public List<int[]> getCannonDir()
    {
        return this.p.getCannonDir();
    }

    public void pieceResetCannon()
    {
        this.p.resetCannon();
    }

    public void setCannon(List<int[]> cannonDir)
    {
        this.p.setCannon(cannonDir);
    }
}
