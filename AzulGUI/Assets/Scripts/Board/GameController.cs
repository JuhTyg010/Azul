using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Azul;
using Statics;
using TMPro;
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

        [SerializeField] private GameObject nextPlayerPanel;
        [SerializeField] private GameObject notificationPanel;
        
        [SerializeField] private List<Sprite> tileSprites;
        
        public Holding holding;
        
        public bool isPlacing = false;

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
            notificationPanel.GetComponent<Notification>().ShowMessage("This is the test");
            mainPlayerBoard.GetComponent<PlayersBoard>().Init(board.Players[currentPlayer]);
            
        }

        public Player GetPlayerData(int id) {
            if(id < 0 || id >= board.Players.Length) throw new System.IndexOutOfRangeException("asking impossible");
            return board.Players[id];
        }

        public Vector3Int GetHoldingData() {
            if (holding.isHolding) return holding.GetData();
            return new Vector3Int(Globals.EMPTY_CELL, Globals.EMPTY_CELL, Globals.EMPTY_CELL);
        }

        public void PutToHand(int typeId, int count, int plateId) {
            Debug.Log("try to put to hand");
            if(!holding.isHolding) holding.PutToHand(typeId, count, plateId);
            else throw new InvalidOperationException("Player already is holding something");
        }

        public void TryPlaceFromHand(int bufferId) {
            if (holding.isHolding) {
                bool answer = board.Move(holding.plateId, holding.typeId, bufferId);
                if (!answer) {
                    Debug.LogError("Something went wrong, illegal move happened");
                    plates[holding.plateId].GetComponent<Plate>().ReturnFromHand();
                }
                else {
                    var toCenter = plates[holding.plateId].GetComponent<Plate>().EmptyTiles();
                    UpdatePlates();
                    UpdatePlayers();
                    //TODO: call update on center on player and on the plates
                    //TODO: add rest to the center (is in lib should be done automatically)
                    
                    currentPlayer = board.CurrentPlayer; //cause in board it's already updated and we need previous player
                }
                holding.EmptyHand();
                Debug.Log("Hand is empty");
                StartCoroutine(inputWaiter());
                //TODO: add some info to player what to do to finish the move than call DisplayNextPlayerPanel
            }
            else throw new InvalidOperationException("Can't place if not holding anything");
        }

        
        
        public Sprite GetTileSprite(int id) {
            if (id >= tileSprites.Count || id < 0)
                throw new IndexOutOfRangeException($"Want tile with no sprite (id: {id})");

            return tileSprites[id];
        }

        private void NextMove() {
            //TODO: update plates, clear center, reload ids of the player's boards show nextPLayer panel probably with the name of the player
            UpdatePlates();
            UpdatePlayers();
        }

        private void DisplayNextPlayerPanel() {
            nextPlayerPanel.GetComponentInChildren<TMP_Text>().text = board.Players[currentPlayer].name;
            //TODO: event on click
            nextPlayerPanel.SetActive(true);
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

        private void GenerateOtherPlayersBoards() {
            //TODO: maybe do it in other players panel script
            Vector3 currentPosition =  firstPlayerPosition;
            for (int i = 0; i < board.Players.Length - 1; i++) {    //one is for the main view
                var player = Instantiate(playerBoardPrefab, otherPlayersPanel.transform);
                players.Add(player);
                player.GetComponentInChildren<PlayersBoard>().Init(board.Players[i]);   //only one board in there so it's kinda safe
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
            int offset = 0;
            for (int i = 0; i < board.Players.Length; i++) {
                if (i == currentPlayer) {
                    mainPlayerBoard.GetComponent<PlayersBoard>().UpdateData(board.Players[i]);
                    offset = 1;
                }
                else {
                    players[i - offset].GetComponentInChildren<PlayersBoard>().UpdateData(board.Players[i]);
                }
            }
        }
        IEnumerator inputWaiter()
        {
            yield return new WaitForSeconds(1f);
            NextMove();

        }   
    }
}
