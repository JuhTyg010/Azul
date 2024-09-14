using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Board {
    public class GameController : MonoBehaviour
    {
        public GameObject tile;
        
        [SerializeField] private GameObject otherPlayersPanel;
        [SerializeField] private GameObject platesPanel;
        
        [SerializeField] private GameObject playerBoardPrefab;
        [SerializeField] private GameObject platePrefab;
        
        [SerializeField] private Vector2 firstPlatePosition;
        [SerializeField] private Vector2 plateOffset;
        [SerializeField] private bool isFlippedY = false;
        
        public bool isHolding = false;
        private Vector3 handData; //x=typeId y=count z=plateId
        
        
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
            Debug.Log(board.Plates.Length);
            GeneratePlates(board.Plates.Length);
            FillPlates();
            
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
            else throw new System.InvalidOperationException("Player already is holding something");
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
                    //TODO: add rest to the center
                }
                isHolding = false;
            }
            else throw new System.InvalidOperationException("Can't place if not holding anything");
            
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

        private void FillPlates() {
            for (int i = 0; i < board.Plates.Length; i++) {
                var tiles = board.Plates[i].GetCounts();
                List<int> tileIds = new List<int>();
                foreach (var tile in tiles) {
                    for (int j = 0; j < tile.count; j++) {
                        tileIds.Add(j);
                    }
                }
                plates[i].GetComponent<Plate>().PutTiles(tileIds.ToArray());
            }
        }
    }
}
