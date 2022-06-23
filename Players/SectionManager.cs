using Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tools;


public class SectionManager : MonoBehaviour
{
    public Section DATA;

    #region Unity Process

    public bool IsOwned;
    public bool Selected;
    public bool Performing;
    public GameManager gameManager;
    public GameObject Leader;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        StartSectionUI(gameObject);

        DATA.Pions = new Pion[4];
        if (!IsOwned)
        {
            GetComponentInChildren<Toggle>().gameObject.SetActive(false);
        }

        CreateSectionOwnedUnits();
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0)) sectionUITools.Displacing = false;

        if (gameManager.MapView.enabled && !EventSystem.current.IsPointerOverGameObject())
        {
            if (!Performing && gameManager.MovingObject == null) sectionUITools.DisplaceTimelineElement();
            if (Performing) sectionUITools.DrawMission();
        }
    }

    #endregion

    #region Section Units management

    public GameObject UnitPrefab;
    public List<PlayerUnit> PlayerUnits = new List<PlayerUnit>();
    void CreateSectionOwnedUnits()
    {
        Transform _Leader = transform.Find("SectionLeader");
        Leader = _Leader.gameObject;
        _Leader.GetComponent<PlayerUnit>().SectionManager = this;
        _Leader.GetComponent<PlayerUnit>().ReferenceObjectPosition = _Leader.transform.position;
        PlayerUnits.Add(_Leader.GetComponent<PlayerUnit>());
        for (int i = 0; i <= 3; i++)
        {
            GameObject Unit = Instantiate(UnitPrefab);
            PlayerUnits.Add(Unit.GetComponent<PlayerUnit>());
            Unit.name = "Groupe " + i.ToString();
            Unit.transform.parent = gameObject.transform;
            Unit.transform.position = gameObject.transform.position + new Vector3((i * -9) + 18, 1, 7);
            var unitData = Unit.GetComponent<UnitController>().DATA = new Pion()
            {
                Nom = Unit.name,
                Santé = int.MaxValue,
                Section = DATA,
            };
            DATA.Pions[i] = unitData;
            if (!IsOwned)
            {
                Unit.transform.Find("Cylinder").gameObject.SetActive(false);
                Unit.GetComponent<UnitController>().IsOwned = false;
                Unit.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region Mission Control

    public List<Mission> MissionsList = new List<Mission>();
    public Mission CurrentMission;

    public void CancelPendingMissions(bool _override)
    {
        if (!Selected && !_override) return;
        int i = 0;
        foreach (var mission in MissionsList)
        {
            if (mission.ValidIndex != 0)
            {
                mission.EndingTurn = mission.ValidIndex;
                int Index = MissionsList.FindIndex(m => m == mission);
                mission.StartingTurn = Index != 0
                    ? MissionsList[Index - 1].ValidIndex
                    : 0;
                mission.TimelineObject.GetComponent<RectTransform>().sizeDelta = new Vector2((mission.EndingTurn - mission.StartingTurn) * sectionUITools.CellWidth, 30);
                mission.TimelineObject.GetComponent<RectTransform>().anchoredPosition = new Vector2((mission.StartingTurn) * sectionUITools.CellWidth, -15);
                mission.SetValid();
                if (mission.OriginalState != null)
                {
                    StartCoroutine(WaitAndReset( mission, i));
                }
            }
            else if (!mission.IsValid)
            {
                mission.KillMission();
            }
            i++;
        }
        sectionUITools.HideValidationButtons();
    }

    private IEnumerator<WaitForEndOfFrame> WaitAndReset( Mission mission, int Index)
    {
        yield return new WaitForEndOfFrame();
        var _mission = Instantiate(mission.OriginalState, mission.sectionManager.transform).GetComponent<Mission>();
        _mission.gameObject.SetActive(true);
        _mission.MapObject = _mission.transform.Find("New Game Object").gameObject;
        _mission.Line = _mission.MapObject.GetComponent<LineRenderer>();
        Destroy(mission.gameObject);
        Destroy(mission.OriginalState);
        MissionsList[Index] = _mission;
    }

    public void SendPendingMissions()
    {
        bool HasAnythingChanged = false;
        if (!Selected) return;
        foreach (var _action in MissionsList)
        {
            if (_action.HasChanged) HasAnythingChanged = true;
            _action.IsValid = true;
            _action.TimelineObject.GetComponent<Image>().color = OverlayPrefab.GetComponent<Image>().color;
            _action.ValidIndex = _action.EndingTurn;
            _action.HasChanged = false;
            if (_action.OriginalState == null) Destroy(_action.OriginalState);
        }

        if (HasAnythingChanged)
        {
            Image CurrentTurnCell = sectionUITools.timelineAsset.transform.Find("Panel").GetComponentsInChildren<Image>()[gameManager.CurrentTurn];
            CurrentTurnCell.color = Color.red;
        }

        sectionUITools.HideValidationButtons();
    }

    #endregion

    #region UI Control

    public GameObject SectionUI;
    public GameObject SectionUIPrefab;
    public GameObject TimelinePrefab;
    public GameObject CellPrefab;
    public GameObject OverlayPrefab;
    public Sprite LineSprite;
    public Shader TransparentShader;
    public EventSystem m_EventSystem;
    public SectionUITools sectionUITools;

    public void StartSectionUI(GameObject Section)
    {
        m_EventSystem = GetComponent<EventSystem>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        sectionUITools = new SectionUITools(this);
        SectionUI = Instantiate(SectionUIPrefab, GameObject.Find("ObjectList").transform.Find("Container"));
        SectionUI.name = Section.name + "UI";

        RectTransform rect = SectionUI.GetComponent<RectTransform>();
        rect.offsetMin = new Vector3(10, 10, 0);
        rect.offsetMax = new Vector3(10, 10, 0);
        rect.sizeDelta = new Vector2(0, 30);


        SectionUI.GetComponentInChildren<Text>().text = Section.name;
        SectionUI.GetComponentInChildren<Button>().onClick.AddListener(
            () => gameManager.MapViewSelectionControl(new Ray(), gameObject.transform.Find("SectionLeader").gameObject.transform.Find("Cube").gameObject)
            );
        int SectionNumber = int.Parse(Section.name.Replace("Section", "")) - 1;
    }



    #endregion
}
