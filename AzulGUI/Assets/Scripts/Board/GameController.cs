using System;
using System.Collections;
using System.Collections.Generic;
using Azul;
using Statics;
using TMPro;
using UnityEngine;
using randomBot;
using Unity.VisualScripting;

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
        [SerializeField] private Notification notification;
        
        [SerializeField] private List<Sprite> tileSprites;
        [SerializeField] private Sprite nextMoveFirstTile;
        [SerializeField] public Sprite emptyTileSprite;
        
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private CursorSprite cursorSprite;
        
        public Holding holding;
        public bool isPlacing = false;
        public bool isTaking = false;
        public bool isBotsTurn = false;
        public Phase phase;
        public bool isAdvanced;
        public int[,] predefinedWall;
        
        
        private int currentPlayer;
        private Azul.Board board;
        private List<Bot> bots = new List<Bot>();
        public List<GameObject> plates = new List<GameObject>();
        public List<GameObject> players = new List<GameObject>();
        
        
        private bool keyPressed = false;
        
        void Awake()
        {
            int playerCount = PlayerPrefs.GetInt("PlayerCount");
            string[] names = new string[playerCount];
            string[] passedNames = PlayerPrefs.GetString("PlayerNames").Split('\n');
            string[] passedTypes = PlayerPrefs.GetString("PlayerTypes").Split('\n');
            
            for (int i = 0; i < playerCount; i++) {
                if(passedTypes[i] == "AI") bots.Add(new Bot(i));
                names[i] = passedNames[i];
            }
            
            
            board = new Azul.Board(playerCount, names);
            
            phase = board.Phase;
            isAdvanced = board.isAdvanced;
            predefinedWall = board.predefinedWall;
            currentPlayer = board.CurrentPlayer;
            
            GeneratePlates(board.Plates.Length);
            GenerateOtherPlayersBoards();
            mainPlayerBoard.GetComponent<PlayersBoard>().Init(board.Players[currentPlayer]);
            
            
            DisplayNextPlayerPanel();
            
        }

        void Update() {
            if(!Input.anyKey) keyPressed = false;
            if (isPlacing && !isAdvanced && Input.anyKey && !keyPressed && !isBotsTurn) {
                keyPressed = true;
                PlaceNextTileToWall();
            }
        }

        public Player GetPlayerData(int id) {
            if(id < 0 || id >= board.Players.Length) throw new System.IndexOutOfRangeException("asking impossible");
            return board.Players[id];
        }

        public Vector3Int GetHoldingData() {
            if (holding.isHolding) return holding.GetData();
            return new Vector3Int(Globals.EMPTY_CELL, Globals.EMPTY_CELL, Globals.EMPTY_CELL);
        }

        public void PutToHand(int typeId, int plateId) {
            if (!holding.isHolding) {
                int count;
                if(plateId == board.Plates.Length) count = board.Center.TileCountOfType(typeId);
                else count = board.Plates[plateId].TileCountOfType(typeId);
                holding.PutToHand(typeId, count, plateId);
                cursorSprite.SetVisible(true, tileSprites[typeId], count.ToString());
            }
            else throw new InvalidOperationException("Player already is holding something");
        }

        public void TryPlaceFromHand(int bufferId) {
            if(phase != Phase.Taking) throw new InvalidOperationException("You are not in phase to put to buffers");
            if (!holding.isHolding) throw new InvalidOperationException("Can't place if not holding anything");
            cursorSprite.SetVisible(false, nextMoveFirstTile, "");
            bool answer = board.Move(holding.plateId, holding.typeId, bufferId);
            if (!answer) {
                Debug.LogError("Something went wrong, illegal move happened");
                if(holding.plateId == plates.Count) 
                    centerPlateBoard.GetComponent<CenterPlate>().ReturnFromHand();
                else plates[holding.plateId].GetComponent<Plate>().ReturnFromHand();
            }
            else {
                isTaking = false;
                if(holding.plateId < plates.Count) plates[holding.plateId].GetComponent<Plate>().EmptyTiles();
                UpdatePlates();
                UpdatePlayers();
                
                currentPlayer = board.CurrentPlayer; //cause in board it's already updated and we need previous player
                StartCoroutine(NextMoveInputWaiter());
                notification.ShowMessage("Press any key to end turn");
            }
            holding.EmptyHand();
        }

        public void TryPlaceOnWall(Vector2Int position) {
            if(phase != Phase.Placing) throw new InvalidOperationException("You are not in phase to place on wall");
            if(!isAdvanced) throw new InvalidOperationException("in not advanced placing is automaptic");
            bool answer = board.Calculate(position.y);
            if (!answer) {
                ShowMessage("You tried to place on illegal position on the wall");
            }
            else {
                isPlacing = false;
                if (currentPlayer != board.CurrentPlayer || phase != Phase.Placing) {
                    StartCoroutine(NextMoveInputWaiter());
                } 
                else {  //player still have fullBuffers
                    NextMove(); //skip the nextPlayerPanel
                }
            }
        }

        public void TryPlaceOnWall(int x, int y) {
            TryPlaceOnWall(new Vector2Int(x, y));
        }

        public void ShowMessage(string message) {
            notification.ShowMessage(message);
        }

        public void ShowLongMessage(string message) {
            notification.ShowLongMessage(message);
        }
        
        public Sprite GetTileSprite(int id) {
            if (id == Globals.FIRST) return nextMoveFirstTile;
            if (id >= tileSprites.Count || id < 0)
                throw new IndexOutOfRangeException($"Want tile with no sprite (id: {id})");

            return tileSprites[id];
        }

        private void NextMove() {
            currentPlayer = board.CurrentPlayer;
            phase = board.Phase;
            nextPlayerPanel.SetActive(false);
            if(bots.Exists(x => x.id == currentPlayer)) isBotsTurn = true;
            if (phase == Phase.Taking && !isBotsTurn) {
                notification.ShowLongMessage("Choose plate, and take some tile to buffer");
                isTaking = true;
            }
            else if (phase == Phase.Placing && !isBotsTurn) {
                
                isPlacing = true;

                if (board.isAdvanced) {
                    notification.ShowLongMessage(
                        "Choose a column on the wall where you would like to place a tile from the buffer");
                }
                else {
                    notification.ShowLongMessage("Press any button to move tile from buffer to the wall");
                }
            }
            UpdatePlates();
            UpdatePlayers();
        }

        private void PlaceNextTileToWall() {
            isPlacing = false;
            board.Calculate();
            if (currentPlayer != board.CurrentPlayer || board.Phase != Phase.Placing) {
                UpdatePlayers();
                if(!isBotsTurn) StartCoroutine(NextMoveInputWaiter());
                else DisplayNextPlayerPanel();
                Debug.Log("Next one placing");
            } 
            else {  //player still have fullBuffers
                NextMove(); //skip the nextPlayerPanel
                Debug.Log("I still can place a tile");
            }
        }
        
        private void BotMove(Bot bot) {
            Debug.Log("hiding panel");
            nextPlayerPanel.SetActive(false);
            if (phase == Phase.Taking) {
                var response = bot.DoMove(board).Split();
                int typeId = int.Parse(response[1]);
                int plateId = int.Parse(response[0]);
                int bufferId = int.Parse(response[2]);
                board.Move(plateId, typeId, bufferId);
                isTaking = false;

            } else if (phase == Phase.Placing) {
                if (!isAdvanced) {
                    while(currentPlayer == board.CurrentPlayer)
                        PlaceNextTileToWall();
                }
                else {
                    while (currentPlayer == board.CurrentPlayer) {
                        var response = int.Parse(bot.Place(board));
                        board.Calculate(response);
                    }
                }
                isPlacing = false;
            }
            else {
                Debug.LogError($"Unknown possible move for bot in this phase: {phase}");
            }

            isBotsTurn = false;
            DisplayNextPlayerPanel();
        }

        private void ShowGameOverPanel() {
            Dictionary<string, int> players = new Dictionary<string, int>();
            int maxScore = -999;
            string maxName = null;
            gameOverPanel.SetActive(true);
            foreach (var player in board.Players) {
                if (player.pointCount > maxScore) {
                    maxScore = player.pointCount;
                    maxName = player.name;
                }
                players.Add(player.name, player.pointCount);
            }
            var endGame = gameOverPanel.GetComponent<endGamePanel>();
            endGame.setTable(players);
            endGame.setWinner(maxName);
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
            centerPlateBoard.GetComponent<CenterPlate>().Init(plateCount, this);
        }

        private void GenerateOtherPlayersBoards() {
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
            centerPlateBoard.GetComponent<CenterPlate>().UpdateData(board.Center);
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
        
        private void DisplayNextPlayerPanel() {
            currentPlayer = board.CurrentPlayer;
            var panelHandler = nextPlayerPanel.GetComponent<NextPlayerPanel>();
            if (board.Phase == Phase.GameOver) {
                ShowGameOverPanel();
                return;
            }
            NextMove();
            string currentPhase = phase == Phase.Placing ? "placing" : "taking";
            nextPlayerPanel.GetComponentInChildren<TMP_Text>().text = board.Players[currentPlayer].name;
            if(isBotsTurn) panelHandler.SetText($"It's {currentPhase} phase. Press to let <color=red>Bot</color> play");
            panelHandler.SetText($"It's {currentPhase} phase. Press any button to continue");
            nextPlayerPanel.SetActive(true);
            StartCoroutine(NextPlayerReactionWaiter());
        }

       
        IEnumerator NextMoveInputWaiter()
        {
            ShowMessage("Press any key to end turn");
            yield return new WaitUntil(() => Input.anyKey && !keyPressed);
            keyPressed = true;
            DisplayNextPlayerPanel();
        }

        IEnumerator NextPlayerReactionWaiter() {
            yield return new WaitUntil(() => Input.anyKey && !keyPressed);
            keyPressed = true;
            nextPlayerPanel.SetActive(false);
            NextMove();
            if (isBotsTurn) {
                BotMove(bots.Find(x => x.id == currentPlayer));
            }
        }

        
    }
}
