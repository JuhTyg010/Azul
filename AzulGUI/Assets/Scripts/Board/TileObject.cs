using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Board {
    
    [RequireComponent(typeof(Image))]
    public class TileObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            
        }
    }
}
