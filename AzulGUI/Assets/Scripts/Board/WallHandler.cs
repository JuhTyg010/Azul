
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Azul;

namespace Board {

    public class WallHandler : MonoBehaviour {
        [SerializeField] private GameObject tile;
        [SerializeField] private Vector2 topLeftPos;
        [SerializeField] private Vector2 offset;

        private GameObject[,] wallData;

        private void Awake() {
            wallData = new GameObject[Globals.WALL_DIMENSION, Globals.WALL_DIMENSION];

            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    var nextTile = Instantiate(tile, transform);
                    Vector2 realPosition = topLeftPos;
                    realPosition.x += i * offset.x;
                    realPosition.y -= j * offset.y;
                    nextTile.GetComponent<RectTransform>().anchoredPosition = realPosition;
                    nextTile.GetComponent<WallTile>().Initialize(i, j);
                    wallData[i, j] = nextTile;
                }
            }
        }

        public void SetWall(int[,] data) {
            if (data.GetLength(0) != Globals.WALL_DIMENSION
                || data.GetLength(1) != Globals.WALL_DIMENSION) {
                throw new Exception("wrong sized int[,] for wall");
            }

            for (int i = 0; i < Globals.WALL_DIMENSION; i++) {
                for (int j = 0; j < Globals.WALL_DIMENSION; j++) {
                    wallData[i, j].GetComponent<WallTile>().FillTile(data[i, j]);
                }
            }

        }
    }

}
