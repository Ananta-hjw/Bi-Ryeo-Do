using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// The battle board: 6 columns x 3 rows. Our side starts in columns 0-2 (left), the enemies in columns 3-5 (right).
    /// Distance is the king-move distance (diagonals count as one step), so range 1 means "the next tile in any direction".
    /// </summary>
    public static class Board
    {
        public const int Cols = 6, Rows = 3;
        public const int PartyCols = 3;            // columns 0..2 are ours at the start
        public const int MoveRange = 2;            // tiles a unit may walk before acting

        public static bool InBounds(int col, int row) { return col >= 0 && col < Cols && row >= 0 && row < Rows; }

        public static int Dist(int col1, int row1, int col2, int row2)
        {
            return Math.Max(Math.Abs(col1 - col2), Math.Abs(row1 - row2));
        }

        public static int Dist(Combatant a, Combatant b) { return Dist(a.Col, a.Row, b.Col, b.Row); }
    }
}
