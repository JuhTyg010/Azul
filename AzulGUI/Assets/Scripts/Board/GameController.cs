using UnityEngine;

namespace Board {
    public class GameController : MonoBehaviour
    { 
        public GameObject[] tiles;
        [SerializeField] private GameObject playerBoardPrefab = null;
        [SerializeField] private GameObject otherPlayersPanel;
        
        private Azul.Board board;
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
