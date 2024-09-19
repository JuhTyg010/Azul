using System.Collections.Generic;
using UnityEngine;

namespace Board {
    public class PlayersBoard : MonoBehaviour
    {
        public int id { get; private set; }
        
        [SerializeField] private GameObject wallHolder;
        [SerializeField] private List<GameObject> bufferHolders;
        [SerializeField] private GameObject floorHolder;

        private GameController gameController;

        void Awake() {
            gameController = FindObjectOfType<GameController>();
        }

        void Update(){
            //TODO: get from gameManager playerData
        }

        public void Initialize(int id, GameController gameController) {
            this.id = id;
            this.gameController = gameController;
            
        }

        private void UpdateWall(Azul.Player me) {
            //TODO: wall should have some script which translates from int[,] to wall
        }

        private void UpdateBuffers(Azul.Player me) {
            for (int i = 0; i < bufferHolders.Count; i++) {
                var bufferLogic = bufferHolders[i].GetComponent<BufferHolder>();
                var data = me.GetBufferData(i);
                bufferLogic.LoadData(data.id, data.count);
            }
        }

        private void UpdateFloor(Azul.Player me) {
            //TODO: script for the floor and placing there
        }
        
    }
}
