using UnityEngine;
using UnityEngine.UI;

public class DisplaySliderValue : MonoBehaviour
{
    private Slider slider;
    public string MainText;
    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
        GetComponentInChildren<Text>().text = MainText + " " + slider.value.ToString();
        slider.onValueChanged.AddListener((value) => GetComponentInChildren<Text>().text = MainText + " " + value.ToString());
    }

}
