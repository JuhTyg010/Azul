using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Board {
    public class Plate : MonoBehaviour {
    
        [SerializeField] private Vector2[] localPositions;
        private Azul.Plate plate;
    
        private GameController gameController;
        void Start()
        {   
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            plate = new Azul.Plate();
        }

        // Update is called once per frame
        void Update()
        {
            if (!plate.isEmpty) {
                var tiles = plate.GetCounts();
                int index = 0;
                foreach (var tile in tiles) {
                    while (tile.count > 0) {
                        Instantiate(gameController.tiles[tile.id], transform.position + (Vector3)localPositions[index], Quaternion.identity);
                        index++;
                    }
                }
            }
        }

    }
}
