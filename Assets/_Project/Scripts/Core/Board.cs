namespace BiRyeoDo.Core
{
    /// <summary>
    /// The battle board: 6 columns x 3 rows, fixed for the whole fight. Our side stands in columns 0-2 (left, column 2 is the front line),
    /// the enemies in columns 3-5 (right, column 3 is their front line). Where everyone stands is chosen before the fight and never changes;
    /// attacks reach by line (see <see cref="InteractiveBattle.Depth"/>), not by distance.
    /// </summary>
    public static class Board
    {
        public const int Cols = 6, Rows = 3;
        public const int PartyCols = 3;            // columns 0..2 are ours
    }
}
