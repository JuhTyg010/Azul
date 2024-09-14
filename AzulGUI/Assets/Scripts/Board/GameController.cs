using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Board {
    public class GameController : MonoBehaviour
    { 
        public GameObject tile;
        [SerializeField] private GameObject playerBoardPrefab = null;
        [SerializeField] private GameObject otherPlayersPanel;
        
        
        
        private Azul.Board board;
        
        
        void Start()
        {
            using StreamReader reader = new StreamReader(Application.dataPath + "game_config.txt");
            string line = reader.ReadLine();
            string[] data = line.Split(' ');
            
            int playerCount = int.Parse(data[0]);
            List<int> bots = new List<int>();
            string[] names = new string[playerCount];
            
            for (int i = 1; i <= playerCount; i++) {
                string[] playerData = data[i].Split('_');
                if(playerData[0] == "B") bots.Add(i);
                names[i-1] = playerData[1];
            }
            
            board = new Azul.Board(playerCount, names);
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
