using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Board {
    
    [RequireComponent(typeof(Image))]
    public class TileObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] List<Sprite> sprites = new List<Sprite>();
        public int id; 
        private Image image;
        
        void Start()
        {
            image = GetComponent<Image>();
        }

        // Update is called once per frame
        void Update()
        {
            image.sprite = sprites[id];
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            
        }
    }
}
