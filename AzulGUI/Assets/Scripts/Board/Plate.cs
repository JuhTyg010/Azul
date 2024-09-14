using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


namespace Board {
    public class Plate : MonoBehaviour {
    
        [SerializeField] private Vector2[] localPositions;
        private List<GameObject> tiles = new List<GameObject>();
        private List<GameObject> inHand = new List<GameObject>();
    
        private GameController gameController;
        void Start()
        {   
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void PutTiles(int[] tilesIds) {
            tiles.Clear();
            int current = 0;
            foreach (var id in tilesIds) {
                var tile = Instantiate(gameController.tile);
                tile.GetComponent<TileObject>().id = id;
                tiles.Add(tile);
                tile.GetComponent<RectTransform>().anchoredPosition = localPositions[current];
                current++;
            }
        }
    }
}
