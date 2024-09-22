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
        [SerializeField] private GameObject mainPlayerBoard;
        [SerializeField] private Vector2 mainPlayerPosition;
        
        [SerializeField] private GameObject centerPlatePanel;
        [SerializeField] private GameObject centerPlateBoard;
        [SerializeField] private Vector2 centerPlatePosition;
        
        [SerializeField] private GameObject platesPanel;
        [SerializeField] private GameObject platePrefab;
        [SerializeField] private Vector2 firstPlatePosition;
        [SerializeField] private Vector2 plateOffset;
        [SerializeField] private bool isFlippedY = false;
        
        [SerializeField] private List<Sprite> tileSprites;
        
        public bool isHolding = false;
        public bool isPlacing = false;
        private Vector3 handData; //x=typeId y=count z=plateId

        private int currentPlayer;
        private Azul.Board board;
        public List<GameObject> plates = new List<GameObject>();
        public List<GameObject> players = new List<GameObject>();
        
        
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
            UpdatePlates();
            GenerateOtherPlayersBoards();
            
            mainPlayerBoard.GetComponent<PlayersBoard>().UpdateData(board.Players[currentPlayer]);
            
        }

        public Player GetPlayerData(int id) {
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
                    
                    mainPlayerBoard.GetComponent<PlayersBoard>().UpdateData(board.Players[currentPlayer]);
                    for (int i = 0; i < board.Players.Length; i++) {
                        if(i == currentPlayer) continue;
                        players[i].GetComponent<PlayersBoard>().UpdateData(board.Players[i]);
                    }
                    //TODO: call update on center on player and on the plates
                    //TODO: add rest to the center (is in lib should be done automatically)
                    
                    currentPlayer = board.CurrentPlayer; //cause in board it's already updated and we need previous player
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

        private void NextMove() {
            //TODO: update plates, clear center, reload ids of the player's boards show nextPLayer panel probably with the name of the player
        }

        private void GenerateOtherPlayersBoards() {
            //TODO: maybe do it in other players panel script
            Vector3 currentPosition =  firstPlayerPosition;
            for (int i = 0; i < board.Players.Length - 1; i++) {    //one is for the main view
                var player = Instantiate(playerBoardPrefab, otherPlayersPanel.transform);
                players.Add(player);
                player.GetComponentInChildren<PlayersBoard>().Init(i, board.Players[i].name);   //only one board in there so it's kinda safe
                player.GetComponent<RectTransform>().anchoredPosition = currentPosition;
                currentPosition.x += playerOffset.x;
                currentPosition.y += playerOffset.y;
            }
        }

        private void UpdatePlates() {
            for (int i = 0; i < board.Plates.Length; i++) {
                plates[i].GetComponent<Plate>().UpdateData(board.Plates[i]);
            }
        }

        private void UpdatePlayers() {
            for (int i = 0; i < board.Players.Length; i++) {
                if (i == currentPlayer) {
                    mainPlayerBoard.GetComponent<PlayersBoard>().UpdateData(board.Players[i]);
                }
                else {
                    players[i].GetComponentInChildren<PlayersBoard>().UpdateData(board.Players[i]);
                }
            }
        }
    }
}
