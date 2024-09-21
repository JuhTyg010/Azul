using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Azul;


namespace Board {
    public class FloorHandler : MonoBehaviour {
        private const int FLOOR_SIZE = 7;

        [SerializeField] private GameObject tile;
        [SerializeField] private Vector2 leftPos;
        [SerializeField] private Vector2 offset;

        private FloorTile[] tiles;
        private GameController gameController;


        private void Awake() {
            gameController = FindObjectOfType<GameController>();

            tiles = new FloorTile[FLOOR_SIZE];
            for (int i = 0; i < FLOOR_SIZE; i++) {
                var nextTile = Instantiate(tile, transform);
                Vector2 realPosition = leftPos;
                realPosition.x += offset.x * i;
                realPosition.y += offset.y * i;
                nextTile.GetComponent<RectTransform>().anchoredPosition = realPosition;
                tiles[i] = nextTile.GetComponent<FloorTile>();
                tiles[i].Init(Globals.EMPTY_CELL, this);
            }
        }

        public void UpdateData(int[] floorIds) {
            if (floorIds.Length != FLOOR_SIZE) {
                Debug.LogErrorFormat("Invalid number of floorIds for floorHandler: {0}", floorIds.Length);
            }

            for (int i = 0; i < FLOOR_SIZE; i++) {
                tiles[i].SetTile(floorIds[i]);
            }
        }
    }
}