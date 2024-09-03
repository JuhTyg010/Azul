
using UnityEngine;
using UnityEngine.UI;

public class PlayersSetup : MonoBehaviour {

    private const int maxCount = 4;
    private const int minCount = 2;

    [SerializeField] private int playerWidth = 50;
    [SerializeField] private GameObject playerPrefab;

    private int count;
    void Start() {
        count = 2;
        
        for (int i = 0; i < count; i++) {
            var obj = Instantiate(playerPrefab, transform);
            obj.name = "Player_" + i;
            obj.GetComponent<PlayerConfig>().id = i;
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * playerWidth, 0);


        }
    }

    public void OnPlusClicked() {
        if(count < maxCount) {
            var obj = Instantiate(playerPrefab, transform);
            obj.name = "Player_" + count;
            obj.GetComponent<PlayerConfig>().id = count;
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(count * playerWidth, 0);
            
            count++;
        }
    }

    public void OnMinusClicked() {
        if(count > minCount) {
            Destroy(transform.GetChild(count + 1).gameObject); //cause 2 indexes are taken by plus and minus
            count--;
        }
    }
    
    private GameObject Player(int id) {
        GameObject obj = new GameObject();
        obj.name = $"player_{id}";
        obj.AddComponent<PlayerConfig>();
        var transform = obj.AddComponent<RectTransform>();
        var config = obj.GetComponent<PlayerConfig>();
        config.id = id;

        return obj;
    }

    
}
