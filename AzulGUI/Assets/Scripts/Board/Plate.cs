using System;
using System.Collections.Generic;
using Azul;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;


namespace Board {
    public class Plate : MonoBehaviour {
    
        [SerializeField] private Vector2[] localPositions;
        protected List<GameObject> tiles = new List<GameObject>();
        protected List<GameObject> inHand = new List<GameObject>();
        public int id { get; private set; }

        protected Tile[] oldData;
    
        public GameController gameController { get; protected set; }
        void Awake()
        {   
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            Debug.Log(gameController.tile);
            oldData = Array.Empty<Tile>();
        }

        public void Init(int id, GameController gameController) {
            this.gameController = gameController;
            this.id = id;
            oldData = Array.Empty<Tile>();
        }
        
        public void PutInHand(int tileId) {
            int count = 0;
            foreach (var tile in tiles) {
                var tileObject = tile.GetComponent<TileObject>();
                if (tileObject.id == tileId) {
                    inHand.Add(tile);
                    tile.SetActive(false);
                    count++;
                }
            }
            gameController.PutToHand(tileId, count, id);
        }

        public void ReturnFromHand() {
            for (int i = 0; i < tiles.Count; i++) {
                tiles[i].SetActive(true);
            }
        }

        public List<GameObject> EmptyTiles() {
            var toReturn = new List<GameObject>();
            foreach (var tile in tiles) {
                var tileObject = tile.GetComponent<TileObject>();
                if (!inHand.Contains(tile)) {
                    toReturn.Add(tile);
                }
            }

            foreach (var tile in inHand) {
                Destroy(tile);
            }

            return toReturn;
        }

        public void ClearTiles() {
            tiles.Clear();
        }

        public virtual void UpdateData(Azul.Plate plateData) {
            var data = plateData.GetCounts();
            if(data == oldData) return;
            var difference = Differ(oldData, data);
            
            //remove tiles which are not longer in current plate
            for (int i = 0; i < oldData.Length; i++) {
                for (int j = 0; j < difference[1, i].count; j++) {
                    var tile = tiles.Find(x => x.GetComponent<TileObject>().id == oldData[i].id);
                    tiles.Remove(tile);
                    Destroy(tile);
                }
            }
            inHand.Clear();

            int current = 0;
            foreach (var tile in tiles) {
                tile.GetComponent<RectTransform>().anchoredPosition = localPositions[current];
                current++;
            }
            //Add only necessary tiles
            for (int i = 0; i < data.Length; i++){
                for (int j = 0; j < difference[0,i].count; j++) {
                    var tile = Instantiate(gameController.tile,transform);
                    tiles.Add(tile);
                    tile.GetComponent<TileObject>().Initialize(GetComponent<Plate>(), difference[0,i].id);
                    tile.GetComponent<RectTransform>().anchoredPosition = localPositions[current];
                    tile.GetComponent<RectTransform>().Rotate(Vector3.forward, Random.Range(0f, 90f));
                    current++;
                }
            }

            oldData = data;
        }

        protected Tile[,] Differ(Tile[] oldData, Tile[] newData) {
            Tile[,] differ = new Tile[2, newData.Length]; //those Lengths are the same
            for (int i = 0; i < newData.Length; i++) {
                if (i < oldData.Length && oldData[i].id == newData[i].id) {
                    if (oldData[i].count > newData[i].count) {
                        differ[1, i] = new Tile(newData[i].id, oldData[i].count - newData[i].count);
                    }
                    else {
                        differ[0, i] = new Tile(newData[i].id, newData[i].count - oldData[i].count);
                    }
                }
                else {
                    if(i < oldData.Length) differ[1, i] = oldData[i];
                    differ[0, i] = newData[i];
                }
            }

            return differ;
        }
    }
}
