using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


namespace Board {
    public class Plate : MonoBehaviour {
    
        [SerializeField] private Vector2[] localPositions;
        private List<GameObject> tiles = new List<GameObject>();
        private List<GameObject> inHand = new List<GameObject>();
    
        public GameController gameController { get; private set; }
        void Awake()
        {   
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            Debug.Log(gameController.tile);
        }

        public void PutTiles(int[] tilesIds) {
            tiles.Clear();
            int current = 0;
            foreach (var id in tilesIds) {
                
                var tile = Instantiate(gameController.tile,transform);
                tiles.Add(tile);
                tile.GetComponent<TileObject>().Initialize(GetComponent<Plate>(), id);
                tile.GetComponent<RectTransform>().anchoredPosition = localPositions[current];
                tile.GetComponent<RectTransform>().Rotate(Vector3.forward, Random.Range(0f, 90f));
                current++;
            }
        }

        public void PutInHand(int tileId) {
            foreach (var tile in tiles) {
                var tileObject = tile.GetComponent<TileObject>();
                if (tileObject.id == tileId) {
                    inHand.Add(tile);
                    tile.SetActive(false);
                }
            }
            gameController.isHolding = true;
        }

        public void ClearTiles() {
            tiles.Clear();
        }
    }
}
