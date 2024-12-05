using System.Collections.Generic;
using Azul;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

namespace Board {
    
    [RequireComponent(typeof(Image))]
    public class TileObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
       
        public int id;
        private Image image;
        private Plate myPlate;
        public GameController gameController {get; private set;}
        
        void Awake() {
            image = GetComponent<Image>();
        }
        
        public void Initialize(Plate plate, int id_) {
            myPlate = plate;
            id = id_;
            image = GetComponent<Image>();
            gameController = plate.gameController;
            image.sprite = gameController.GetTileSprite(id);

        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            if(!gameController.holding.isHolding && 
               id != Globals.FIRST && 
               gameController.isTaking) 
                GetComponent<RectTransform>().localScale = Vector3.one * 1.5f;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            GetComponent<RectTransform>().localScale = Vector3.one;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if(!gameController.holding.isHolding && 
               id != Globals.FIRST && 
               gameController.isTaking) myPlate.PutInHand(id);
        }
    }
}
