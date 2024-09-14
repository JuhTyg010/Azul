using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerConfig : MonoBehaviour, IPointerClickHandler {
    
    public string playerName { get; private set; }
    public Sprite humanImage;
    public Sprite botImage;
    private Image myImage;
    
    [SerializeField] private TMP_InputField nameHolder;


    public bool isHuman { get; private set; }
    public int id;
    void Start() {

        myImage = gameObject.GetComponent<Image>();
        myImage.sprite = humanImage;
        isHuman = true;
        playerName = $"Player_{id + 1}";
        nameHolder.text = playerName;
    }
    
    public void OnPointerClick(PointerEventData eventData) {
        isHuman = !isHuman;
        myImage.sprite = isHuman ? humanImage : botImage;
    }

    public void OnValueChanged() {
        if (nameHolder.text != playerName && nameHolder.text.Length < 9) {
            playerName = nameHolder.text;
        }
        else {
            nameHolder.text = playerName;
        }
    }
}
