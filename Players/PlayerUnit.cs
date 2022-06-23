using Actions;
using System;
using Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using static Actions.ActionTools;

public class PlayerUnit : MonoBehaviour
{
    public GameObject CirclePrefab;
    public GameObject GhostPrefab;
    public GameObject UiPrefab;
    public GameObject ActionButtonPrefab;
    public Material LineMaterial;
    public Color BaseColor;

    public string[] ActionButtonsNames = new string[0];
    public List<GameObject> ActionButtons = new List<GameObject>();
    public List<DetectedUnit> detectedUnits = new List<DetectedUnit>();
    public GameObject MainUiObject;
    public Button MainUiButton;
    public RectTransform actionBar;
    public Vector3 ScreenObjectCenter;
    public Vector3 ReferenceObjectPosition;
    public NavMeshAgent Agent;

    public GameManager gameManager;
    public GameObject Ghost;
    public SectionManager SectionManager;
    public UnitAction[] ActionArray = new UnitAction[0];
    public bool IsOwned;
    public bool Selected;
    public int ActionPoints;
    public int numberInOrder;
    public int CombatValue = 8;
    public int ComsRange = 3;
    public int OperationalValue = 100;
    public bool CanEmbark = false;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        ReferenceObjectPosition = transform.position;
        StartCoroutine(Latestart());
    }

    protected IEnumerator<WaitUntil> Latestart() 
    { 
        yield return new WaitUntil(() => SectionManager != null);

        CreateUiObject(numberInOrder + 1);
        Agent = GetComponent<NavMeshAgent>();
        if (Agent!=null) Agent.speed = (float)(4 / (gameManager.DistanceMultiplier * gameManager.StepTimeInSeconds));
        ComsRange = CheckDistToLeader(transform, SectionManager.Leader.transform);
        UpdateActionResolutionUI();
    } 

    void Update()
    {
        UpdateActionPreparationUI(ReferenceObjectPosition);
    }

    private void LateUpdate()
    {
        if (actionBar != null && actionBar.gameObject.activeInHierarchy)
        {
            actionBar.transform.parent.LookAt(actionBar.transform.parent.position + gameManager.SceneView.transform.forward);
            UpdateActionBar();
        }
    }

    public void CreateUiObject(int numberInOrder)
    {
        actionBar.GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(0,1,1);
        ScreenObjectCenter = gameManager.SceneView.WorldToScreenPoint(gameObject.transform.position);
        MainUiObject = Instantiate(UiPrefab);
        MainUiObject.GetComponentInChildren<Image>().color = BaseColor;
        MainUiObject.GetComponentInChildren<Text>().text = gameObject.name;
        MainUiObject.transform.SetParent(SectionManager.SectionUI.transform);
        MainUiObject.transform.localPosition = new Vector3(50, 15 - 50 - (35 * numberInOrder), 0);
        MainUiButton = MainUiObject.GetComponentInChildren<Button>();
        AssignListener();
        MainUiObject.SetActive(false);

        if (IsOwned)
        {
            actionBar.GetChild(0).localScale = new Vector3(0, 1, 1);
        }

        int index = 0;
        foreach (string ActionButtonName in ActionButtonsNames)
        {
            Vector2 Positions = new Vector2(0, 70);
            Positions = index switch
            {
                1 => new Vector2(50, 50),
                2 => new Vector2(70, 0),
                3 => new Vector2(50, -50),
                4 => new Vector2(0, -70),
                _ => new Vector2(0, 70),
            };
            GameObject Button = Instantiate(ActionButtonPrefab);
            Button.name = ActionButtonName;
            Button.GetComponent<ActionButtonManager>().Unit = this;
            Button.GetComponentInChildren<Text>().text = ActionButtonName;
            Button.transform.position = new Vector3(ScreenObjectCenter.x + Positions.x, ScreenObjectCenter.y + Positions.y);
            Button.transform.SetParent(GameObject.Find("Actions").transform);
            Button.SetActive(false);
            ActionButtons.Add(Button);
            if (ActionButtonName != "Annuler") Button.GetComponent<ActionButtonManager>().ChildButtonsNames = DefineChildButtons.SetChilds(ActionButtonName);
            else
            {
                Button _button = Button.GetComponent<Button>();
                Destroy(Button.GetComponent<ActionButtonManager>());
                Button.GetComponent<Image>().color = Color.red;
                _button.onClick.AddListener(() =>
                {
                    ReferenceObjectPosition = transform.position;
                    foreach (UnitAction _action in ActionArray)
                    {
                        _action.elements.DeleteUI();
                    };
                    ActionArray = new UnitAction[0];
                });
            }
            index++;
        }
    }

    public void AssignListener()
    {
        MainUiButton.onClick.AddListener(() =>
        {
            gameManager.SceneViewUnselect();
            gameManager.SceneViewSelect(gameObject, this);
        });
    }

    public void UpdateActionBar()
    {
        int sumOfAP = 0;
        if(ActionArray.Length == 0) actionBar.GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(0, 1f, 1f);
        for (int i = 0; i < ActionArray.Length; i++)
        {
            UnitAction action = ActionArray[i];
            if(i == ActionArray.Length -1 && action.Type == ActionTools.ActionTypes.None)
            {
                RemoveAction(action, false);
            }
            RectTransform Slice;
            if (action.PlayerUnit == this) Slice = action.ActionBarSlice.GetComponent<RectTransform>();
            else Slice = action.RemoteActionBarSlice.GetComponent<RectTransform>();
            if (i != 0)
            {
                RectTransform previousSlice = ActionArray[i - 1].ActionBarSlice.GetComponent<RectTransform>();
                float AnchorX = previousSlice.anchoredPosition.x + (previousSlice.localScale.x * actionBar.rect.width);
                Slice.anchoredPosition = new Vector3(AnchorX, 0f, 0f);

                if (previousSlice.childCount == 0)
                {
                    RectTransform img = new GameObject().AddComponent<Image>().GetComponent<RectTransform>();
                    img.SetParent(previousSlice);
                    img.GetComponent<Image>().color = Color.black;
                    img.pivot = new Vector2(1, 0.5f);
                    img.anchorMax = img.anchorMin = new Vector2(1, 0.5f);
                    img.localPosition = new Vector3(0, 0, 0);
                    img.localEulerAngles = new Vector3(0, 0, 0);
                    img.anchoredPosition = new Vector3(0, 0, 0);
                    img.sizeDelta = new Vector2(0.2f, 0.5f);
                    img.localScale = new Vector2(1 + previousSlice.localScale.x, 1);
                }
            }

            float percentageOfAP = ((float)action.actionPointsCost / ActionPoints);
            float remainingAPPercentage = ((float)(ActionPoints - sumOfAP) / ActionPoints);
            sumOfAP += action.actionPointsCost;
            if (sumOfAP >= ActionPoints || percentageOfAP >= remainingAPPercentage)
            {
                Slice.localScale = new Vector3(remainingAPPercentage, 1f, 1f);
                Slice.GetComponentInChildren<Image>().color = Color.red;
            }
            else
            {
                Slice.localScale = new Vector3(percentageOfAP, 1f, 1f);
                Slice.GetComponentInChildren<Image>().color = action.BaseActionColor;
            }
        }
    }

    public void DisplayActionUI()
    {
        foreach (GameObject button in ActionButtons)
        {
            button.SetActive(true);
        }
        actionBar.transform.parent.gameObject.SetActive(true);
    }
    public void HideActionUI()
    {
        foreach (GameObject button in ActionButtons)
        {
            if (button.name != "Annuler") button.GetComponent<ActionButtonManager>().HideChild();
            button.SetActive(false);
        }
    }
    public void UpdateActionResolutionUI()
    {
        int directComs = CheckDistToLeader(transform, SectionManager.Leader.transform);
        if (ComsRange != directComs)
        {
            ComsRange = directComs;
            var comsIcon = MainUiObject.transform.Find("Coms");
            Image[] bars = comsIcon.GetComponentsInChildren<Image>();
            for (int i = 0; i < bars.Length; i++)
            {
                if (ComsRange > i) bars[i].color = Color.black;
                else bars[i].color = Palette.emptyGrey;
            }
        }
        var StateIcons = MainUiObject.transform.Find("State");
        Image[] icons = StateIcons.GetComponentsInChildren<Image>();;
        for (int i = 0; i < icons.Length; i++)
        {
            if (OperationalValue >= 67) icons[i].color = Palette.reconGreen;
            else if (OperationalValue >= 33 && i < 2) icons[i].color = Palette.midOrange;
            else if (OperationalValue >= 0 && i < 1) icons[i].color = Palette.lowRed;
            else icons[i].color = Palette.emptyGrey;
        }

    }
    public void UpdateActionPreparationUI(Vector3 ReferenceObjectPosition)
    {
        ScreenObjectCenter = gameManager.SceneView.WorldToScreenPoint(ReferenceObjectPosition);
        int index = 0;
        foreach (GameObject Button in ActionButtons)
        {
            Vector2 Positions = new Vector2(0, 70);
            Positions = index switch
            {
                1 => new Vector2(50, 50),
                2 => new Vector2(70, 0),
                3 => new Vector2(50, -50),
                4 => new Vector2(0, -70),
                5 => new Vector2(-50, -50),
                6 => new Vector2(-70, 0),
                7 => new Vector2(-50, 50),
                _ => new Vector2(0, 70),
            };
            Button.transform.position = new Vector3(ScreenObjectCenter.x + Positions.x, ScreenObjectCenter.y + Positions.y);
            index++;
        }
    }

    public void AddAction(UnitAction action)
    {
        int l = ActionArray.Length;
        UnitAction[] temp = ActionArray;
        ActionArray = new UnitAction[l + 1];
        for (int i = 0; i < l; i++)
        {
            ActionArray[i] = temp[i];
        }
        ActionArray[l] = action;
    }

    public void RemoveAction(UnitAction action)
    {
        DisplayActionUI();
        action.elements.DeleteUI();
        int l = ActionArray.Length;
        UnitAction[] temp = ActionArray;
        ActionArray = new UnitAction[l - 1];
        int counter = 0;
        for (int i = 0; i < l; i++)
        {
            if (temp[i] == action)
            {
                continue;
            }
            else
            {
                ActionArray[counter] = temp[i];
                counter++;
            }
        }
    }

    public void RemoveAction(UnitAction action, bool displayUI)
    {
        if (displayUI) DisplayActionUI();
        action.elements.DeleteUI();
        int l = ActionArray.Length;
        UnitAction[] temp = ActionArray;
        ActionArray = new UnitAction[l - 1];
        int counter = 0;
        for (int i = 0; i < l; i++)
        {
            if (temp[i] == action)
            {
                continue;
            }
            else
            {
                ActionArray[counter] = temp[i];
                counter++;
            }
        }
    }
}
