using UnityEngine;

namespace Board {
    public class PlayersBoard : MonoBehaviour
    {
        public int id;

        private GameController gameController;
        private GameObject wallPanel;
        private GameObject[] buffers;
        private GameObject floor;
        void Start()
        {
            gameController = FindObjectOfType<GameController>();
            wallPanel = transform.Find("Wall").gameObject;
            floor = transform.Find("Floor").gameObject;
        }

        
        void Update()
        {
            
        }
    }
}
