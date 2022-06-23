using Actions;
using Data;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Actions.ActionTools;

public class GameManager : MonoBehaviour
{
    //General
    public int NumberOfTurns;
    public int CurrentTurn = 1;
    public int SectionsNumber;
    public LayerMask MapRayCastLayer;
    public LayerMask SceneRayCastLayer;
    public ResolvingPhaseInputs resolvingPhaseInputs;

    //References
    public GameObject Timeline;
    public GameObject Triangle;
    public GameObject SceneUI;
    public GameObject ProgressBar;
    public Camera SceneView;
    public Camera MapView;
    public Canvas Actions;

    //Temporary to get access to materials
    public Texture2D _default;
    public Material BaseMaterial;
    public GameObject SendButtonsContainer;
    public Material TransparentMaterial;

    //Prefabs
    public GameObject CirclePrefab;
    public GameObject SectionPrefab;

    //Dynamics For Preparation Phase
    private GameObject SelectedLeader;
    private GameObject SelectedObject;
    private PlayerUnit SelectedUnit;
    private SectionManager SelectedSection;
    private RaycastHit hit;
    public GameObject MovingObject;
    public List<SectionManager> SectionList = new List<SectionManager>();

    public float StepTimeInSeconds;
    public float DistanceMultiplier;

    //UI
    private float ListHeight;
    private float TimelineHeight;
    public float TurnCellWidth;
    public Scrollbar ObjectListScrollbar;
    public Scrollbar TimelineScrollbar;
    public GameObject Info;

    // Start is called before the first frame update
    void Start()
    {
        SceneUI.SetActive(false);
        Triangle.SetActive(true);
        Timeline.SetActive(true);
        Info = GameObject.Find("InfoZone");
        NumberOfTurns = GameData.NombreDeTours;
        SectionsNumber = GameData.Camps[0].Sections.Length;
        ListHeight = GameObject.Find("ObjectList").GetComponent<RectTransform>().rect.height;
        TimelineHeight = GameObject.Find("Timeline").GetComponent<RectTransform>().rect.height;
        TimelineScrollbar.gameObject.SetActive(false);
        ObjectListScrollbar.gameObject.SetActive(false);
        Triangle.transform.Find("Panel").gameObject.SetActive(false);
        Cursor.SetCursor(_default, Vector2.zero, CursorMode.ForceSoftware);
        SceneView.enabled = false;

        MapView.enabled = true;
        Actions.enabled = SceneView.enabled;
        for (int i = 1; i <= SectionsNumber; i++)
        {
            GameObject section = Instantiate(SectionPrefab);
            Section sectionData = GameData.Camps[0].Sections[i - 1];
            SectionManager sectionManager = section.GetComponent<SectionManager>();
            section.name = sectionData.Nom;
            section.transform.position = new Vector3(i * 40, 0, 50);
            if (i != 1) sectionManager.IsOwned = false;
            else sectionManager.IsOwned = true;
            SectionList.Add(sectionManager);
            sectionData.gameObject = section;
            sectionData.sectionManager = sectionManager;
            sectionManager.DATA = sectionData;

        }
        TurnCellWidth = (MapView.pixelWidth - 450) / NumberOfTurns;

        StartCoroutine(LateStart());
    }

    IEnumerator<WaitForSeconds> LateStart()
    {
        yield return new WaitForSeconds(0.5f);
        TimelineUIScrollManager();
        LeftUIScrollManager();
    }


    // Update is called once per frame
    void Update()
    {
        if (resolvingPhaseInputs != null && resolvingPhaseInputs.currentTime != resolvingPhaseInputs.timeIterator)
        {
            ResolvingPhaseStep();
        }
        else if (resolvingPhaseInputs != null) return;


        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeView();
        }

        if (MapView.enabled)
        {
            Ray ray = MapView.ScreenPointToRay(Input.mousePosition);

            if (MovingObject != null && SelectedSection != null && !SelectedSection.Performing) MoveMapUIElement(ray);

            if (Input.GetKeyDown(KeyCode.Mouse0) && !EventSystem.current.IsPointerOverGameObject())
            {
                SelectMapUIElement(ray);
                MapViewSelectionControl(ray, null);
            }

            else if (Input.GetKeyUp(KeyCode.Mouse0) && !EventSystem.current.IsPointerOverGameObject()) MovingObject = null;
        }

        else if (SceneView.enabled)
        {
            Ray ray = SceneView.ScreenPointToRay(Input.mousePosition);
            if (SelectedUnit != null)
            {
                var _actions = SelectedUnit.GetComponent<PlayerUnit>().ActionArray;
                var lastActiveAction = _actions.Length == 0
                    ? null
                    : Array.Find(_actions, a => a.active);
                var lastAction = _actions.Length == 0
                    ? null
                    : _actions[_actions.Length - 1];

                if (lastActiveAction != null)
                {
                    lastActiveAction.elements.Draw();

                    if (Input.GetKeyDown(KeyCode.Mouse0) && lastActiveAction.State == 0)
                    {
                        lastActiveAction.elements.Set();
                    }
                    else if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        SelectedUnit.RemoveAction(lastAction);
                    }

                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0)) SceneViewSelectionControl();
        }

    }

    #region MapSelection

    public void MapViewSelectionControl(Ray ray, GameObject SectionLeader)
    {
        if (SelectedSection != null && SelectedSection.Performing) return;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, MapRayCastLayer))
        {
            if (hit.collider.gameObject == SelectedLeader)
            {
                MapViewUnselect();
            }

            else if (hit.collider.CompareTag("Leader"))
            {
                MapViewUnselect();
                MapViewSelect(hit.collider.gameObject);
            }

            else if (SelectedLeader != null)
            {
                MapViewUnselect();
            }
        }

        else if (SectionLeader != null && SelectedLeader != SectionLeader)
        {
            MapViewUnselect();
            MapViewSelect(SectionLeader);
        }

        else if (SectionLeader != null)
        {
            MapViewUnselect();
        }
    }

    void MapViewSelect(GameObject SectionLeader)
    {
        SelectedLeader = SectionLeader;
        Triangle.transform.Find("Panel").gameObject.SetActive(true);
        SectionLeader.GetComponent<MeshRenderer>().material.color = Palette.selectedGreen;
        SelectedSection = SelectedLeader.GetComponentInParent<SectionManager>();
        SelectedSection.SectionUI.GetComponentInChildren<Image>().color = Palette.selectedGreen;
        var imgs = SelectedSection.sectionUITools.timelineAsset.GetComponentsInChildren<Image>();
        imgs[imgs.Length - 1].color = Palette.selectedGreen;
        imgs[0].enabled = true;
        SelectedSection.Selected = true;

        SelectedSection.SectionUI.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 300);
        RectTransform[] Units = SelectedSection.SectionUI.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform Unit in Units)
        {
            if (Unit.gameObject != SelectedSection.SectionUI)
            {
                Unit.gameObject.SetActive(true);
            }
        }

        if (SelectedSection.ActionsList.Find(s => !s.IsValid) != null)
        {
            var cancelOrSend = SendButtonsContainer;
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.AddListener(() => SelectedSection.CancelPendingActions(false));
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.AddListener(() => SelectedSection.SendPendingActions());
            cancelOrSend.SetActive(true);
        }
        LeftUIScrollManager();
    }

    void MapViewUnselect()
    {
        if (SelectedLeader != null && MovingObject == null)
        {
            var cancelOrSend = SendButtonsContainer;
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.RemoveAllListeners();
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.RemoveAllListeners();
            cancelOrSend.SetActive(false);

            Triangle.transform.Find("Panel").gameObject.SetActive(false);
            SelectedLeader.GetComponent<MeshRenderer>().material.color = Color.blue;
            SelectedSection.SectionUI.GetComponentInChildren<Image>().color = Palette.playerUnit;
            var imgs = SelectedSection.sectionUITools.timelineAsset.GetComponentsInChildren<Image>();
            imgs[imgs.Length - 1].color = Palette.playerUnit;
            imgs[0].enabled = false;
            SelectedSection.Selected = false;
            SelectedSection.SectionUI.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
            RectTransform[] Units = SelectedSection.SectionUI.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform Unit in Units)
            {
                if (!Unit.gameObject.name.Contains("Section") && !Unit.gameObject.name.Contains("Button") && !Unit.gameObject.name.Contains("Text"))
                {
                    Unit.gameObject.SetActive(false);
                }
            }
            SelectedLeader = null;
            SelectedSection = null;
        }
    }

    #endregion

    #region Move Map Element
    private void SelectMapUIElement(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.name == "Circle")
            {
                MovingObject = hit.collider.gameObject;
            }
        }
    }

    private void MoveMapUIElement(Ray ray)
    {
        if (MovingObject == null) return;
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("FOWRGB")))
        {
            hit.point += Vector3.up;
            var container = MovingObject.transform.parent;
            MovingObject.transform.position = hit.point;

            SectionManager.GameMission _mission = null;
            foreach (SectionManager _section in SectionList)
            {
                var correctmission = _section.ActionsList.Find(m => m.MapObjectContainer == container.transform.parent.gameObject);
                if (correctmission != null)
                {
                    _mission = correctmission;
                    break;
                }
                else continue;
            }
            if (_mission == null) return;
            if (_mission.IsValid)
            {
                _mission.SetTemporary();
                _mission.sectionUITools.DisplayValidationButtons();
                _mission.HasChanged = true;
            }
            if (_mission.OriginalState == null)
            {
                _mission.OriginalState = Instantiate(container.transform.parent.gameObject, _mission.sectionManager.transform);
                _mission.OriginalState.SetActive(false);
            }

            for (int i = 0; i < container.transform.parent.childCount; i++)
            {
                if (container.transform.parent.GetChild(i) == MovingObject.transform.parent)
                {
                    container.transform.parent.GetComponentInChildren<LineRenderer>().SetPosition(i + 1, hit.point);
                }

                if (container.transform.parent.name.Contains("Surveiller"))
                {
                    container.position = hit.point;
                    var line1 = container.GetChild(1).GetComponent<LineRenderer>();
                    var line2 = container.GetChild(2).GetComponent<LineRenderer>();
                    line1.SetPositions(new Vector3[2]
                    {
                        hit.point,
                        hit.point + (line1.GetPosition(1) - line1.GetPosition(0)),
                    });
                    line2.SetPositions(new Vector3[2]
                    {
                        hit.point,
                        hit.point + (line2.GetPosition(1) - line2.GetPosition(0)),
                    });
                }
            }

        }
    }

    private void TimelineUIScrollManager()
    {
        float _AddedHeights = 0;

        foreach (var _sect in SectionList)
        {
            _AddedHeights += 40;
        }

        if (_AddedHeights > TimelineHeight)
        {
            _AddedHeights += 10;
            TimelineScrollbar.gameObject.SetActive(true);
            TimelineScrollbar.size = 0.5f;
            TimelineScrollbar.onValueChanged.AddListener((val) =>
            {
                var element = TimelineScrollbar.transform.parent.Find("Container");
                var rect = element.GetComponent<RectTransform>();
                element.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    rect.anchoredPosition.x,
                    (_AddedHeights - rect.rect.height) * val);
            });
        }

        else if (_AddedHeights < TimelineHeight)
        {
            TimelineScrollbar.gameObject.SetActive(false);
            TimelineScrollbar.size = ListHeight / _AddedHeights;
            var element = TimelineScrollbar.transform.parent.Find("Container");
            var rect = element.GetComponent<RectTransform>();
            element.GetComponent<RectTransform>().anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
        }
    }

    private void LeftUIScrollManager()
    {
        float AddedHeights = 0;

        foreach (var _sect in SectionList)
        {
            AddedHeights += _sect.SectionUI.GetComponent<RectTransform>().rect.height;
            AddedHeights += 10;
        }

        if (AddedHeights > ListHeight)
        {
            AddedHeights += 10;
            ObjectListScrollbar.gameObject.SetActive(true);
            ObjectListScrollbar.size = ListHeight / AddedHeights;
            ObjectListScrollbar.onValueChanged.AddListener((val) =>
            {
                var element = ObjectListScrollbar.transform.parent.Find("Container");
                var rect = element.GetComponent<RectTransform>();
                element.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    rect.anchoredPosition.x,
                    (AddedHeights - rect.rect.height) * val);
            });
        }

        else if (AddedHeights < ListHeight)
        {
            ObjectListScrollbar.gameObject.SetActive(false);
            ObjectListScrollbar.size = ListHeight / AddedHeights;
            var element = ObjectListScrollbar.transform.parent.Find("Container");
            var rect = element.GetComponent<RectTransform>();
            element.GetComponent<RectTransform>().anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
        }
    }
    #endregion

    #region 3D View Selection

    //Check for physical unit selection by raycasting
    void SceneViewSelectionControl()
    {
        Ray ray = SceneView.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, SceneRayCastLayer))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            };

            if (hit.collider.gameObject.layer == 5) return;

            SceneViewUnselect();
            if (hit.collider.GetComponent<PlayerUnit>() != null)
            {
                SceneViewSelect(hit.collider.gameObject, hit.collider.GetComponent<PlayerUnit>());
            }
            else if (hit.collider.GetComponentInParent<PlayerUnit>() != null)
            {
                SceneViewSelect(hit.collider.gameObject, hit.collider.GetComponentInParent<PlayerUnit>());
            }
            else if (hit.collider.CompareTag("EnemyUnit"))
            {
                var info = hit.collider.name.Contains("Multi")
                    ? hit.collider.transform.parent.GetComponent<EnemyUnitManager>().GetInfo()
                    : hit.collider.GetComponent<EnemyUnitManager>().GetInfo();
                DisplayInfo(info);
            }
        }
    }

    public void SceneViewSelect(GameObject castedObject, PlayerUnit playerUnit)
    {
        SelectedObject = castedObject;
        SelectedUnit = playerUnit;
        if (!playerUnit.IsOwned)
        {
            SelectedObject = null;
            return;
        }
        playerUnit.MainUiButton.onClick.AddListener(SceneViewUnselect);
        playerUnit.MainUiObject.GetComponentInChildren<Image>().color = Palette.selectedGreen;
        playerUnit.DisplayActionUI();

        //game-view and Object modifications
        playerUnit.Selected = true;
        foreach (var t in playerUnit.GetComponentsInChildren<Transform>())
        {
            if (t.name.Contains("Multi") || t.name.Contains("Hull"))
            {
                t.GetComponent<MeshRenderer>().material.color = Palette.selectedGreen;
            }
            else continue;        
        }

        Renderer renderer = SelectedObject.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = Palette.selectedGreen;

        DisplayInfo(new UnitInfo()
        {
            Type = "Infanterie",
            Name = castedObject.name,
            Side = "Joueur",
            State = "Complet",
            _infoString = new string[] { "FAMAS", "Grenade M67" }
        });
    }

    public void DisplayInfo(UnitInfo info)
    {
        var grid = Info.transform.Find("Grid");
        grid.gameObject.SetActive(true);
        grid.Find("Type").GetComponentInChildren<Text>().text = info.Type;
        grid.Find("Name").GetComponentInChildren<Text>().text = info.Name;
        grid.Find("Side").GetComponentInChildren<Text>().text = info.Side;
        grid.Find("State").GetComponentInChildren<Text>().text = info.State;
        grid.Find("Info").GetComponentInChildren<Text>().text = string.Join("\n", info._infoString);
    }

    public void SceneViewUnselect()
    {
        if (SelectedUnit != null)
        {
            //UI modifications
            SelectedUnit.MainUiButton.onClick.RemoveAllListeners();
            SelectedUnit.AssignListener();
            SelectedUnit.MainUiObject.GetComponentInChildren<Image>().color = Palette.playerUnit;
            SelectedUnit.HideActionUI();
            foreach (var t in SelectedObject.GetComponentsInChildren<Transform>())
            {
                if (t.name.Contains("Multi") || t.name.Contains("Hull"))
                {
                    t.GetComponent<MeshRenderer>().material.color = SelectedUnit.BaseColor;
                }
                else continue;
                
            }

            SelectedUnit.Selected = false;
            Renderer renderer = SelectedObject.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = SelectedUnit.BaseColor;
            SelectedObject = null;
            SelectedUnit = null;
            var info = Info.transform.Find("Grid");
            info.gameObject.SetActive(false);
        }
    }

    public void NextTurn()
    {
        CurrentTurn++;
        var barpos = ProgressBar.GetComponent<RectTransform>().position;
        if (CurrentTurn > NumberOfTurns) return;
        ProgressBar.GetComponent<RectTransform>().position = new Vector3(barpos.x + TurnCellWidth, 75);

        foreach (var Section in SectionList)
        {
            Section.CancelPendingActions(true);
            Image CurrentTurnCell = Section.sectionUITools.timelineAsset.transform.Find("Panel").GetComponentsInChildren<Image>()[CurrentTurn - 1];
            CurrentTurnCell.color = new Color(0.3f, 0.3f, 0.3f, 1);
            var _actionList = Section.ActionsList;
            foreach (var _action in _actionList)
            {
                if (_action.Passed) continue;
                else if (_action.EndingTurn <= CurrentTurn - 1)
                {
                    Section.CurrentMission = _actionList[_action.Number];
                    _action.Passed = true;
                    _action.TimelineObject.GetComponentInChildren<CursorChanger>().gameObject.SetActive(false);
                    _action.MapObjectContainer.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region On Key Press 

    private void ChangeView()
    {
        SceneView.enabled = !SceneView.enabled;
        MapView.enabled = !MapView.enabled;
        Actions.enabled = SceneView.enabled;
        Timeline.SetActive(!Timeline.activeInHierarchy);
        Triangle.SetActive(!Triangle.activeInHierarchy);
        SceneUI.SetActive(!SceneUI.activeInHierarchy);
        foreach (SectionManager _Section in SectionList)
        {
            if (!_Section.IsOwned)
            {
                _Section.SectionUI.SetActive(!_Section.SectionUI.activeInHierarchy);
            }
        }
    }
    #endregion

    #region Resolving Phase
    public void ResolveTurn()
    {
        MapViewUnselect();
        SceneViewUnselect();
        resolvingPhaseInputs = new ResolvingPhaseInputs();
        //NextTurn();
    }

    public void ResolvingPhaseStep()
    {
        resolvingPhaseInputs.currentTime++;

        foreach (var Section in SectionList)
        {
            if (Section.IsOwned)
            {
                MissionModifiers mods = Section.CurrentMission != null
                    ? new MissionModifiers(Section.CurrentMission.Type)
                    : new MissionModifiers(MissionsTools.MissionTypes.None);

                foreach (PlayerUnit Unit in Section.PlayerUnits)
                {
                    Unit.UpdateActionResolutionUI();
                    if (Unit.ActionArray.Length == 0 || resolvingPhaseInputs.actionIndex == Unit.ActionArray.Length)
                    {
                        if (resolvingPhaseInputs.timeIterator % 5 == 0) RunDetectionTest(Unit, mods, new ActionModifiers(ActionTools.ActionTypes.None));
                        continue;
                    }

                    var Action = Unit.ActionArray[resolvingPhaseInputs.actionIndex];
                    ActionModifiers actionMods = new ActionModifiers(Action.Type);

                    if (Action.actionPointsCost == 0)
                    {
                        resolvingPhaseInputs.actionIndex++;
                        resolvingPhaseInputs.costOffset += Action.actionPointsCost;
                        Action.elements.DeleteUI();
                    }
                    else Action.actionPointsCost--;
                    if (resolvingPhaseInputs.timeIterator % 5 == 0)
                    {
                        RunDetectionTest(Unit, mods, new ActionModifiers(Action.Type));
                        Action.elements.Execute();
                        RealtimeDuelsManager.ResolveDuels();
                    }
                }
            }
            else continue;
        }

        if (resolvingPhaseInputs.currentTime <= 50) StartCoroutine(WaitForNextStep());
        else
        {
            resolvingPhaseInputs = null;
            StopCoroutine(WaitForNextStep());
            foreach (var Section in SectionList)
            {
                if (Section.IsOwned)
                {
                    foreach (PlayerUnit Unit in Section.PlayerUnits)
                    {
                        Unit.ActionArray = new UnitAction[0];
                    }
                }
                else continue;
            }
        }
        return;
    }

    IEnumerator<WaitForSecondsRealtime> WaitForNextStep()
    {
        yield return new WaitForSecondsRealtime(StepTimeInSeconds);
        resolvingPhaseInputs.timeIterator++;
    }

    public class ResolvingPhaseInputs
    {
        public int actionIndex;
        public int costOffset;
        public int timeIterator;
        public int currentTime;
        public ResolvingPhaseInputs()
        {
            actionIndex = costOffset = timeIterator = 0;
            currentTime = -1;
        }
    }
    #endregion
}
