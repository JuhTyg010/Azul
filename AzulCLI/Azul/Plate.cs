using System.Diagnostics;

namespace Azul {
    public class Plate {
        protected Tiles tiles;
        public bool isEmpty { get; protected set; }

        internal Plate() {
            tiles = new Tiles(0, 0);
            isEmpty = true;
        }

        internal Plate(int typesCount) {
            tiles = new Tiles(typesCount, 0);
            isEmpty = true;
        }

        /// <summary>
        /// Method gives info what is on it
        /// </summary>
        /// <returns>array of tile with data from plate</returns>
        public Tile[] GetCounts() {
            Tile[] tile = new Tile[tiles.typesCount];
            for (int i = 0; i < tiles.typesCount; i++) {
                tile[i] = new Tile(i, tiles.TileCountOfType(i));

            }

            return tile;
        }

        /// <summary>
        /// Method to get how much of specific type of tile is there
        /// </summary>
        /// <param name="typeId"> id of tile type</param>
        /// <returns>number of tiles with typeId on the plate</returns>
        public int TileCountOfType(int typeId) {
            return tiles.TileCountOfType(typeId);
        }

        internal void SetTiles(Tiles tiles_) {
            tiles = tiles_;
            isEmpty = tiles.TotalTiles() == 0;
        }

        
        internal virtual Tile TakeTile(int id) {
            Tile outTile = tiles.TakeTiles(id);
            Debug.Assert(outTile.Count != 0, "We can't take tile which is not on the plate");
            isEmpty = tiles.TotalTiles() == 0;
            return outTile;
        }

        internal void ClearPlate() {
            tiles = new Tiles(tiles.typesCount, 0);
            isEmpty = true;
        }

        public override string ToString() {
            string outStr = "[ ";
            for (int i = 0; i < tiles.typesCount; i++) {
                outStr += tiles.TileCountOfType(i) + ", ";
            }

            outStr += "]";
            return outStr;
        }
    }

    public class CenterPlate : Plate {
        public bool isFirst { get; protected set; }

        public CenterPlate() {
            tiles = new Tiles(0, 0);
            isEmpty = true;
            isFirst = true;
        }

        public CenterPlate(int typesCount) {
            tiles = new Tiles(typesCount, 0);
            isEmpty = true;
            isFirst = true;
        }

        internal override Tile TakeTile(int id) {
            Tile outTile = tiles.TakeTiles(id);
            isEmpty = tiles.TotalTiles() == 0;
            isFirst = false;
            return outTile;
        }

        internal void AddTiles(Tiles toPut) {
            tiles.Union(toPut);
            isEmpty = tiles.TotalTiles() == 0;
        }
    }
}