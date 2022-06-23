using Actions;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ActionButtonManager : MonoBehaviour
{
    public string[] ChildButtonsNames;
    public GameObject Container;
    private int full = 70;
    private int mid = 50;
    private Vector3[] circleCoords;
    public PlayerUnit Unit;
    public GameObject ChildPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        if (GetComponentInChildren<Text>().text == "Button") return;
        circleCoords = new Vector3[]
        {
            new Vector3(0, full),
            new Vector3(mid, mid),
            new Vector3(full, 0),
            new Vector3(mid, -mid),
            new Vector3(0, -full),
            new Vector3(-mid, -mid),
            new Vector3(-full, 0),
            new Vector3(-mid, mid),
        };
        Container = new GameObject
        {
            name = "Container"
        };
        Container.transform.SetParent(transform);
        int i = 0;
        foreach (var button in ChildButtonsNames)
        {
            GameObject child = Instantiate(ChildPrefab);
            Destroy(child.GetComponent<ActionButtonManager>());
            child.name = button;
            child.GetComponentInChildren<Text>().text = button;
            child.transform.position = new Vector3(
                transform.position.x + circleCoords[i].x,
                transform.position.y + circleCoords[i].y, 
                transform.position.z + circleCoords[i].z
            );
            switch (button)
            {
                case "Tirer": break;
                case "Assaut": break;
                case "Rapide": break;
                case "Observer": break;
                case "Brèche": break;
                case "Embarquer": break;
                default: child.GetComponent<Button>().interactable = false; break;
            }
            child.transform.SetParent(Container.transform);
            AssignListenerFromName(child.GetComponent<Button>(), Unit);
            i++;
        }
        Container.SetActive(false);


    }

    public static void AssignListenerFromName(Button button, PlayerUnit unitController)
    {
        string ActionName = button.GetComponentInChildren<Text>().text;

        button.onClick.AddListener(() =>
        {
            unitController.HideActionUI();
            new UnitAction(ActionTools.ActionNameToEnum(ActionName), unitController);
        });
    }

    public void ShowChild()
    {
        Container.SetActive(!Container.activeInHierarchy);
        foreach(var button in Unit.ActionButtons)
        {
            if (button == gameObject) continue;
            button.SetActive(!button.activeInHierarchy);
        }
    }

    public void HideChild()
    {
        Container.SetActive(false);
        foreach (var button in Unit.ActionButtons)
        {
            if (button == gameObject) continue;
            button.SetActive(false);
        }
    }
}
