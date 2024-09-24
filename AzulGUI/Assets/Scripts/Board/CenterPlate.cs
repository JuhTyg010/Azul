using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Board {


    public class CenterPlate : Plate {
        [SerializeField] private float radius = 1.5f;
        private Unity.Mathematics.Random random;


        void Awake() {
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();

        }
        
        public override void UpdateData(Azul.Plate plateData) {
            var data = plateData.GetCounts();
            tiles.Clear();
            var children = GetComponentsInChildren<TileObject>();
            foreach (var child in children) {
                Destroy(child.gameObject);
            }
            inHand.Clear();
            int current = 0;
            for (int i = 0; i < data.Length; i++){
                for (int j = 0; j < data[i].count; j++) {
                    var tile = Instantiate(gameController.tile,transform);
                    tiles.Add(tile);
                    tile.GetComponent<TileObject>().Initialize(GetComponent<Plate>(), data[i].id);
                    tile.GetComponent<RectTransform>().anchoredPosition = GetRandomPosition();
                    tile.GetComponent<RectTransform>().Rotate(Vector3.forward, Random.Range(0f, 90f));
                    current++;
                }
            }
        }

        private Vector2 GetRandomPosition() {
            float r = radius * Mathf.Sqrt(random.NextFloat());
            float theta = random.NextFloat() * 2 * Mathf.PI;
            Vector2 output = new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            return output;
        }
    }
}
