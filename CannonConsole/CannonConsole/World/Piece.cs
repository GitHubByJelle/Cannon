using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Piece
{
    int id;
    int dir;
    epieceType pieceType;
    List<int[]> cannonDir;
    bool cannon = false;

    public enum epieceType
    {
        soldier,
        cannon,
        town
    }

    public Piece(int id, int dir, epieceType pieceType)
    {
        setId(id);
        setDir(dir);
        setPieceType(pieceType);
    }

    public int getId()
    {
        return this.id;
    }

    void setId(int id)
    {
        this.id = id;
    }

    public int getDir()
    {
        return this.dir;
    }

    void setDir(int dir)
    {
        this.dir = dir;
    }

    public epieceType getPieceType()
    {
        return this.pieceType;
    }

    public void setPieceType(epieceType pieceType)
    {
        this.pieceType = pieceType;
    }

    public List<int[]> getCannonDir()
    {
        return this.cannonDir;
    }
    public void setCannon(List<int[]> cannonDir)
    {
        this.cannonDir = cannonDir;
        this.cannon = cannonDir.Count() > 0;
        if (this.cannon)
        {
            this.pieceType = epieceType.cannon;
        }
    }

    public void resetCannon()
    {
        this.cannon = false;
        this.cannonDir = null;
        this.pieceType = epieceType.soldier;
    }

    public bool isCannon()
    {
        return this.cannon;
    }
}
