
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour {
    
    [SerializeField] private float notificationDuration;
    
    private TMP_Text text;
    private Image image;
    private bool notificationActive;
    
    private void Awake() {
        text = GetComponentInChildren<TMP_Text>();
        image = GetComponent<Image>();
    }

    public void ShowMessage(string message) {
        text.text = message;
        Color color = image.color;
        color.a = 1;
        image.color = color;
        gameObject.SetActive(true);
    }
    
    void Update() {
        if (notificationActive) {
            Color color = image.color;
            color.a -= Time.deltaTime / notificationDuration;
            if (color.a <= 0) {
                notificationActive = false;
                gameObject.SetActive(false);
            }
            image.color = color;
        }
    }
}
