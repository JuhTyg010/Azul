using System;
using System.Collections.Generic;
using System.IO;
using Azul;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Board {
    public class GameController : MonoBehaviour
    {
        public GameObject tile;
        
        [SerializeField] private GameObject otherPlayersPanel;
        [SerializeField] private GameObject playerBoardPrefab;
        [SerializeField] private Vector2 firstPlayerPosition;
        [SerializeField] private Vector2 playerOffset;

        [SerializeField] private GameObject mainPlayerPanel;
        [SerializeField] private GameObject mainPlayerBoardPrefab;
        [SerializeField] private Vector2 mainPlayerPosition;
        
        [SerializeField] private GameObject centerPlatePanel;
        [SerializeField] private GameObject centerPlateBoardPrefab;
        [SerializeField] private Vector2 centerPlatePosition;
        
        [SerializeField] private GameObject platesPanel;
        [SerializeField] private GameObject platePrefab;
        [SerializeField] private Vector2 firstPlatePosition;
        [SerializeField] private Vector2 plateOffset;
        [SerializeField] private bool isFlippedY = false;
        
        [SerializeField] private List<Sprite> tileSprites;
        
        public bool isHolding = false;
        private Vector3 handData; //x=typeId y=count z=plateId

        private int currentPlayer;
        private Azul.Board board;
        public List<GameObject> plates = new List<GameObject>();
        
        
        void Start()
        {
            int playerCount = PlayerPrefs.GetInt("PlayerCount");
            Debug.Log(playerCount);
            List<int> bots = new List<int>();
            string[] names = new string[playerCount];
            string[] passedNames = PlayerPrefs.GetString("PlayerNames").Split('\n');
            string[] passedTypes = PlayerPrefs.GetString("PlayerTypes").Split('\n');
            
            for (int i = 0; i < playerCount; i++) {
                Debug.Log(passedNames[i]);
                Debug.Log(passedTypes[i]);
                if(passedTypes[i] == "AI") bots.Add(i);
                names[i] = passedNames[i];
            }
            
            board = new Azul.Board(playerCount, names);
            currentPlayer = board.CurrentPlayer;
            Debug.Log(board.Plates.Length);
            GeneratePlates(board.Plates.Length);
            FillPlates();
            GenerateOtherPlayersBoards(currentPlayer);
            
        }

        public Azul.Player GetPlayerData(int id) {
            if(id < 0 || id >= board.Players.Length) throw new System.IndexOutOfRangeException("asking impossible");
            return board.Players[id];
        }

        public Vector2 GetHoldingData() {
            if (isHolding) return handData;
            return new Vector2(Azul.Globals.EMPTY_CELL, Azul.Globals.EMPTY_CELL);
        }

        public void PutToHand(int typeId, int count, int plateId) {
            if (!isHolding) {
                handData.x = typeId;
                handData.y = count;
                handData.z = plateId;
                isHolding = true;
            }
            else throw new InvalidOperationException("Player already is holding something");
        }

        public void TryPlaceFromHand(int bufferId) {
            if (isHolding) {
                bool answer = board.Move((int) handData.z, (int) handData.x, bufferId);
                if (!answer) {
                    Debug.LogError("Something went wrong, illegal move happend");
                    plates[(int)handData.z].GetComponent<Plate>().ReturnFromHand();
                }
                else {
                    var toCenter = plates[(int)handData.z].GetComponent<Plate>().EmptyTiles();
                    for(int i = 0; i < plates.Count; i++) {
                        plates[i].GetComponent<Plate>().UpdateData(board.Plates[i]);
                    }
                    //TODO: call update on center on player and on the plates
                    //TODO: add rest to the center (is in lib should be done automatically)
                }
                isHolding = false;
                handData = new Vector3(Globals.EMPTY_CELL, Globals.EMPTY_CELL, Globals.EMPTY_CELL);
            }
            else throw new System.InvalidOperationException("Can't place if not holding anything");
        }

        public Sprite GetTileSprite(int id) {
            if (id >= tileSprites.Count)
                throw new IndexOutOfRangeException("Want tile with no sprite");

            return tileSprites[id];
        }

        private void GeneratePlates(int plateCount) {
            Vector3 currentPosition = (Vector3) firstPlatePosition;
            for (int i = 0; i < plateCount; i++) {
                var plate = Instantiate(platePrefab, currentPosition, Quaternion.identity, platesPanel.transform);
                plates.Add(plate);
                plate.GetComponent<Plate>().Init(i, this);
                plate.GetComponent<RectTransform>().anchoredPosition = currentPosition;
                currentPosition.x += plateOffset.x;
                if(isFlippedY) currentPosition.y *= -1;
                else currentPosition.y += plateOffset.y;
            }
        }

        private void GenerateOtherPlayersBoards(int currentPlayer) {
            //TODO: maybe do it in other players panel script
            Vector3 currentPosition =  firstPlatePosition;
            for (int i = 0; i < board.Players.Length; i++) {
                if(i == currentPlayer) continue;
                var player = Instantiate(playerBoardPrefab, otherPlayersPanel.transform);
                player.GetComponent<PlayersBoard>().Initialize(i, this);
                player.GetComponent<RectTransform>().anchoredPosition = currentPosition;
                currentPosition.x += plateOffset.x;
                currentPosition.y += plateOffset.y;
            }
        }

        private void FillPlates() {
            for (int i = 0; i < board.Plates.Length; i++) {
                plates[i].GetComponent<Plate>().UpdateData(board.Plates[i]);
            }
        }
    }
}
