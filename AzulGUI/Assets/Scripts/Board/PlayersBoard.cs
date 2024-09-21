using System.Collections.Generic;
using Azul;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Board {
    public class PlayersBoard : MonoBehaviour
    {
        public int id { get; private set; }

        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private bool isMain;
        [SerializeField] private TMP_Text pointCoutText;
        [SerializeField] private GameObject wallHolder;
        [SerializeField] private List<GameObject> bufferHolders;
        [SerializeField] private GameObject floorHolder;

        private GameController gameController;

        void Awake() {
            gameController = FindObjectOfType<GameController>();
        }

        public void UpdateData(Player me){
            UpdateBuffers(me);
            UpdateFloor(me);
            UpdateWall(me);
            pointCoutText.text = me.pointCount.ToString();
            playerNameText.text = me.name;
        }

        public void Initialize(int id, GameController gameController) {
            this.id = id;
            this.gameController = gameController;
            //TODO: somehow get own name
            
            
        }

        private void UpdateWall(Player me) {
            var wall = wallHolder.GetComponent<WallHandler>();
            wall.SetWall(me.wall);

        }

        private void UpdateBuffers(Player me) {
            for (int i = 0; i < bufferHolders.Count; i++) {
                var bufferLogic = bufferHolders[i].GetComponent<BufferHolder>();
                var data = me.GetBufferData(i);
                bufferLogic.LoadData(data.id, data.count);
            }
        }

        private void UpdateFloor(Player me) {
            //TODO: script for the floor and placing there
        }
        
    }
}
