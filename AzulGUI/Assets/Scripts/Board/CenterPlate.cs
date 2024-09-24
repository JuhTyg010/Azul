using System.Collections;
using System.Collections.Generic;
using Azul;
using UnityEngine;

namespace Board {


    public class CenterPlate : Plate {
        [SerializeField] private float radius = 1.5f;
        private Unity.Mathematics.Random random;


        void Awake() {
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);
            gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();

        }
        
        public void UpdateData(Azul.CenterPlate plateData) {
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

            if (!plateData.isFirst && tiles.Exists(x => x.GetComponent<TileObject>().id == Globals.FIRST)) {
                var tile = tiles.Find(x => x.GetComponent<TileObject>().id == Globals.FIRST);
                Destroy(tile);
            }
            inHand.Clear();
            if (plateData.isFirst && !tiles.Exists(x => x.GetComponent<TileObject>().id == Globals.FIRST)) {
                var tile = Instantiate(gameController.tile,transform);
                tiles.Add(tile);
                tile.GetComponent<TileObject>().Initialize(GetComponent<Plate>(), Azul.Globals.FIRST);
                tile.GetComponent<RectTransform>().anchoredPosition = GetRandomPosition();
                tile.GetComponent<RectTransform>().Rotate(Vector3.forward, Random.Range(0f, 90f));
            }
            //Add only tiles which are not there
            for (int i = 0; i < data.Length; i++){
                for (int j = 0; j < difference[0,i].count; j++) {
                    var tile = Instantiate(gameController.tile,transform);
                    tiles.Add(tile);
                    tile.GetComponent<TileObject>().Initialize(GetComponent<Plate>(), difference[0,i].id);
                    tile.GetComponent<RectTransform>().anchoredPosition = GetRandomPosition();
                    tile.GetComponent<RectTransform>().Rotate(Vector3.forward, Random.Range(0f, 90f));
                }
            }

            oldData = data;
        }

        private Vector2 GetRandomPosition() {
            float r = radius * Mathf.Sqrt(random.NextFloat());
            float theta = random.NextFloat() * 2 * Mathf.PI;
            Vector2 output = new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            return output;
        }
    }
}
