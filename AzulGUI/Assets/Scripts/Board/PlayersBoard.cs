using System.Collections.Generic;
using UnityEngine;

namespace Board {
    public class PlayersBoard : MonoBehaviour
    {
        public int id { get; private set; }
        
        [SerializeField] private GameObject wallHolder;
        [SerializeField] private List<GameObject> bufferHolders;
        
    }
}
