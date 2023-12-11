using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour
{
    public Button ExitButton;
    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
    }
    
    void CloseView()
    {
        gameObject.SetActive(false);
    }
    
}
