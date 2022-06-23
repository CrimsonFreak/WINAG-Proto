using Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tools;
using static MissionsTools;
using static Actions.ActionTools;


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

    public List<GameMission> ActionsList = new List<GameMission>();
    public GameMission CurrentMission;

    public void CancelPendingActions(bool _override)
    {
        if (!Selected && !_override) return;
        foreach (var _action in ActionsList)
        {
            if (_action.ValidIndex != 0)
            {
                _action.EndingTurn = _action.ValidIndex;
                int Index = ActionsList.FindIndex(m => m == _action);
                _action.StartingTurn = Index != 0
                    ? ActionsList[Index - 1].ValidIndex
                    : 0;
                _action.TimelineObject.GetComponent<RectTransform>().sizeDelta = new Vector2((_action.EndingTurn - _action.StartingTurn) * sectionUITools.CellWidth, 30);
                _action.TimelineObject.GetComponent<RectTransform>().anchoredPosition = new Vector2((_action.StartingTurn) * sectionUITools.CellWidth, -15);
                _action.SetValid();
                if (_action.OriginalState != null)
                {
                    Destroy(_action.MapObjectContainer);
                    _action.MapObjectContainer = Instantiate(_action.OriginalState, _action.sectionManager.transform);
                    _action.MapObjectContainer.SetActive(true);
                    _action.MapObject = _action.MapObjectContainer.transform.Find("New Game Object").gameObject;
                    _action.line = _action.MapObject.GetComponent<LineRenderer>();
                    Destroy(_action.OriginalState);
                }
            }
            else if (!_action.IsValid)
            {
                _action.KillAction();
            }
        }
        sectionUITools.HideValidationButtons();
    }

    public void SendPendingActions()
    {
        bool HasAnythingChanged = false;
        if (!Selected) return;
        foreach (var _action in ActionsList)
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

    public void WaitAndDestroy(List<GameMission> list, GameMission element)
    {
        StartCoroutine(DestroyCoroutine(list, element));
    }

    private IEnumerator<WaitForEndOfFrame> DestroyCoroutine(List<GameMission> list, GameMission element)
    {
        yield return new WaitForEndOfFrame();
        list.Remove(element);
    }

    public class GameMission
    {
        public SectionUITools sectionUITools;
        public SectionManager sectionManager;
        public GameObject Triangle;

        public string Name;
        public MissionTypes Type;
        public int Number;
        public int StartingTurn;
        public int EndingTurn;

        public List<Vector3> points = new List<Vector3>();
        public LineRenderer line;
        public LineRenderer SectorLine1;
        public LineRenderer SectorLine2;
        public GameObject MapObject;
        public GameObject MapObjectContainer;
        public GameObject TimelineObject;
        public GameObject OriginalState;
        public int ValidIndex;

        public bool ActionRecon = false;
        public bool ActionAttack = false;
        public bool ActionElement = false;
        public bool ActionOverwatch = false;
        public int State = 0;
        public bool Scaling = false;
        public bool Passed = false;
        public bool IsValid = false;
        public bool HasChanged = false;

        public GameMission(int _Number, string _Name, int _Start, int _End, Transform _Panel, SectionUITools _Parent)
        {
            Number = _Number;
            TimelineObject = Instantiate(_Parent.sectionManager.OverlayPrefab);
            StartingTurn = _Start;
            EndingTurn = _End;
            sectionUITools = _Parent;
            sectionManager = _Parent.sectionManager;
            Name = _Name;
            ValidIndex = 0;
            SetStartingAttributes(_Panel);
        }


        private void SetStartingAttributes(Transform Panel)
        {
            TimelineObject.transform.SetParent(Panel.transform);
            TimelineObject.GetComponent<RectTransform>().sizeDelta = new Vector2((EndingTurn - StartingTurn) * sectionUITools.CellWidth, 30);
            TimelineObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(StartingTurn * sectionUITools.CellWidth, -15, 0);
            TimelineObject.GetComponentInChildren<Text>().text = Name;
            var i = TimelineObject.GetComponent<Image>();
            i.color = new Color(0.5f, 0.5f, 0.5f, i.color.a);
            sectionUITools.LastTurnWithAction = EndingTurn;
            Triangle = GameObject.Find("Triangle");
            CreateBrokenLine();
            Type = MissionNameToEnum(Name);

            switch (Type)
            {
                case MissionTypes.Reconnaître: StartActionRecon(); break;
                case MissionTypes.Attaquer: StartActionAttack(); break;
                case MissionTypes.Appuyer: StartActionSupport(); break;
                case MissionTypes.Surveiller: StartActionOverWatch(); break;
                default: throw new System.Exception("Erreur lors de la définition du type de Mission");
            }
        }

        private void CreateBrokenLine()
        {
            if (!sectionManager.Selected) return;

            sectionManager.Performing = true;
            MapObject = new GameObject
            {
                layer = 3,
                tag = "Movable"
            };
            MapObjectContainer = new GameObject();
            MapObjectContainer.transform.SetParent(sectionUITools.gameObject.transform);
            MapObjectContainer.name = Name;
            MapObject.transform.SetParent(MapObjectContainer.transform);

            var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.transform.SetParent(MapObject.transform);
            circle.transform.position = Vector3.zero;
            circle.name = "Circle";
            circle.layer = 3;
            circle.GetComponent<MeshRenderer>().material.shader = sectionManager.TransparentShader;
            circle.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.3f);

            if (Name != "Surveiller") circle.transform.localScale = new Vector3(2, 0.1f, 2);
            else circle.transform.localScale = new Vector3(4, 0.1f, 4);
            line = MapObject.AddComponent<LineRenderer>();
            line.startWidth = 2f;
            line.endWidth = 2f;
            line.material = sectionManager.gameManager.BaseMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (sectionManager.ActionsList.Count <= 0) points.Add(sectionManager.gameObject.transform.position);
            else
            {
                int pos = sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].line.positionCount - 1;
                points.Add(sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].line.GetPosition(pos));
            }

        }


        private void StartActionRecon()
        {
            ActionRecon = true;
            Color c = Palette.reconGreen;
            c.a = 0.3f;
            MapObject.GetComponentInChildren<MeshRenderer>().material.color = c;
            line.material.color = Palette.reconGreen;
        }

        private void StartActionAttack()
        {
            ActionAttack = true;
            Color c = Palette.attackRed;
            c.a = 0.3f;
            MapObject.GetComponentInChildren<MeshRenderer>().material.color = c;
            line.material.color = Palette.attackRed;
        }

        private void StartActionOverWatch()
        {
            ActionOverwatch = true;
            Color c = Palette.overwatchTeal;
            c.a = 0.3f;
            MapObject.GetComponentInChildren<MeshRenderer>().material.color = c;
            line.material.color = Palette.overwatchTeal;
            MapObject.AddComponent<MeshFilter>().mesh = new Mesh(); ;

            var renderer = MapObject.AddComponent<MeshRenderer>();
            renderer.gameObject.layer = 3;
            renderer.material = new Material(sectionManager.TransparentShader)
            {
                color = c
            };
        }

        private void StartActionSupport()
        {
            var _Sects = sectionManager.gameManager.SectionList;
            for (int i = 0; i < _Sects.Count; i++)
            {
                if (!_Sects[i].Selected)
                {
                    _Sects[i].transform.Find("SectionLeader").Find("Cube").GetComponent<MeshRenderer>().material.color = Palette.selectedGreen;
                }
            }
            ActionElement = true;
            Color c = Palette.supportBlue;
            c.a = 0.3f;
            MapObject.GetComponentInChildren<MeshRenderer>().material.color = c;
            line.material.color = Palette.supportBlue;
        }


        public void TakeMissionAttack(RaycastHit hit)
        {
            if (!Scaling)
            {
                MapObject.transform.position = hit.point;
                int i = 0;
                line.positionCount = 1;
                foreach (Vector3 point in points)
                {
                    line.positionCount++;
                    line.SetPosition(i, point);
                    i++;
                }
                line.SetPosition(i, hit.point);
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    points.Add(hit.point);
                }
            }
            else if (Input.GetMouseButton(0))
            {
                Scaling = true;
                MapObject.transform.localScale = new Vector3(
                    Mathf.Clamp(Vector3.Distance(MapObject.transform.position, hit.point), 1f, 15f),
                    0.1f,
                    Mathf.Clamp(Vector3.Distance(MapObject.transform.position, hit.point), 1f, 15f)
                );
            }
            else if (Input.GetMouseButtonUp(0))
            {
                MapObject.transform.SetAsLastSibling();
                ActionAttack = false;
                Scaling = false;
                points = new List<Vector3>();
                sectionManager.Performing = false;
                sectionUITools.DisplayValidationButtons();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                KillAction();
                sectionUITools.DisplayValidationButtons();
            }
        }

        public void TakeMissionRecon(RaycastHit hit)
        {
            MapObject.transform.position = hit.point;
            int i = 0;
            line.positionCount = 1;
            foreach (Vector3 point in points)
            {
                line.positionCount++;
                line.SetPosition(i, point);
                i++;
            }
            line.SetPosition(i, hit.point);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    GameObject newCircle = Instantiate(MapObject);
                    Destroy(newCircle.GetComponent<LineRenderer>());
                    newCircle.transform.SetParent(MapObject.transform.parent);
                    newCircle.transform.position = hit.point;
                    points.Add(hit.point);
                }
            }

            else if (Input.GetMouseButtonDown(0))
            {
                MapObject.transform.SetAsLastSibling();
                ActionRecon = false;
                points = new List<Vector3>();
                sectionManager.Performing = false;
                sectionUITools.DisplayValidationButtons();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                KillAction();
                sectionUITools.DisplayValidationButtons();
            }
        }

        public void TakeMissionOverwatch(RaycastHit hit)
        {
            Vector3 origin = line.GetPosition(line.positionCount - 1);
            if (State == 1 && hit.point != origin)
            {
                Vector3 endPoint1 = hit.point;
                SectorLine1.SetPositions(new Vector3[2] { origin, endPoint1 });
                if (Input.GetMouseButtonDown(0))
                {
                    State = 2;
                    sectionUITools.DisplayValidationButtons();
                }
            }

            else if (State == 2)
            {
                float dist = Vector3.Distance(origin, SectorLine1.GetPosition(1)) / Vector3.Distance(origin, hit.point);
                Vector3 endPoint2 = new Vector3(origin.x + ((hit.point.x - origin.x) * dist), 1f, origin.z + ((hit.point.z - origin.z) * dist));
                SectorLine2.SetPositions(new Vector3[2] { origin, endPoint2 });

                Mesh mesh = MapObject.GetComponent<MeshFilter>().mesh;
                mesh.vertices = new Vector3[3]
                {
                    Vector3.zero,
                    SectorLine1.GetPosition(1) - origin,
                    endPoint2 - origin
                };
                mesh.triangles = new int[6] { 0, 1, 2, 0, 2, 1 };
                mesh.RecalculateBounds();

                if (Input.GetMouseButtonDown(0))
                {
                    State = 0;
                    ActionOverwatch = false;
                    sectionManager.Performing = false;
                }
            }

            else
            {
                MapObject.transform.position = hit.point;
                int i = 0;
                line.positionCount = 1;
                foreach (Vector3 point in points)
                {
                    line.positionCount++;
                    line.SetPosition(i, point);
                    i++;
                }
                line.SetPosition(i, hit.point);


                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        GameObject newImg = Instantiate(MapObject);
                        Destroy(newImg.GetComponent<LineRenderer>());
                        newImg.transform.SetParent(MapObject.transform.parent);
                        newImg.transform.position = Input.mousePosition;
                        points.Add(hit.point);
                    }
                }

                else if (Input.GetMouseButtonDown(0) && State == 0)
                {
                    State = 1;

                    SectorLine1 = new GameObject().AddComponent<LineRenderer>();
                    SectorLine1.transform.SetParent(MapObject.transform);

                    SectorLine2 = new GameObject().AddComponent<LineRenderer>();
                    SectorLine2.transform.SetParent(MapObject.transform);

                    SetLineProperties(SectorLine1, Color.white, sectionManager.gameManager.BaseMaterial, 1, 3);
                    SetLineProperties(SectorLine2, Color.white, sectionManager.gameManager.BaseMaterial, 1, 3);
                }

                else if (Input.GetMouseButtonDown(1))
                {
                    KillAction();
                    sectionUITools.DisplayValidationButtons();
                }
            }
        }

        public void TakeMissionSupport(RaycastHit hit)
        {
            MapObject.transform.position = hit.point;
            int i = 0;
            line.positionCount = 1;
            foreach (Vector3 point in points)
            {
                line.positionCount++;
                line.SetPosition(i, new Vector3(point.x, 1, point.z));
                i++;
            }
            line.SetPosition(i, hit.point);

            if (Input.GetMouseButtonDown(0))
            {
                if (!hit.collider.CompareTag("Leader")) return;
                line.SetPosition(1, hit.collider.transform.position);
                MapObject.transform.position = hit.point;
                sectionManager.Performing = false;
                ActionElement = false;
                points = new List<Vector3>();
                RemoveInteractableIndicator();
                sectionUITools.DisplayValidationButtons();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                KillAction();
                RemoveInteractableIndicator();
                sectionUITools.DisplayValidationButtons();
            }

        }


        private void RemoveInteractableIndicator()
        {
            var _Sects = sectionManager.gameManager.SectionList;
            for (int _i = 0; _i < _Sects.Count; _i++)
            {
                _Sects[_i].transform.Find("SectionLeader").Find("Cube").GetComponent<MeshRenderer>().material.color = Color.blue;
            }
        }

        public void SetTemporary()
        {
            IsValid = false;
            var c = TimelineObject.GetComponent<Image>().color;
            TimelineObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, c.a); 
            sectionUITools.DisplayValidationButtons();
        }

        public void SetValid()
        {
            IsValid = true;
            Color c = sectionManager.OverlayPrefab.GetComponent<Image>().color;
            TimelineObject.GetComponent<Image>().color = c;
            sectionUITools.HideValidationButtons();
        }

        public void KillAction()
        {
            ActionAttack = false;
            ActionRecon = false;
            ActionOverwatch = false;
            ActionElement = false;
            points = new List<Vector3>();
            sectionManager.Performing = false;
            Destroy(TimelineObject);
            Destroy(MapObjectContainer);
            Destroy(OriginalState);
            sectionUITools.LastTurnWithAction = sectionUITools.LastTurnWithAction >= StartingTurn
                ? StartingTurn
                : sectionUITools.LastTurnWithAction;
            sectionManager.WaitAndDestroy(sectionManager.ActionsList, this);

            if (sectionManager.ActionsList.Count - 1 == 0 || sectionManager.ActionsList[sectionManager.ActionsList.Count - 2].IsValid)
            {
                sectionUITools.HideValidationButtons();
            }
        }

        public override string ToString() => $"({Name}, {Number}, {StartingTurn}, {EndingTurn})";
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

    public class SectionUITools
    {
        public SectionUITools(SectionManager _Parent)
        {
            TotalTurns = _Parent.gameManager.NumberOfTurns;
            timelineAsset = Instantiate(_Parent.TimelinePrefab);
            gameObject = _Parent.gameObject;
            sectionManager = _Parent;
            gameManager = _Parent.gameManager;
            StartUI();
        }

        public float CellWidth;
        public int TotalTurns;
        public int LastTurnWithAction;
        public bool Displacing = false;

        public GameObject timelineAsset;
        public GameObject gameObject;
        public GameManager gameManager;
        public SectionManager sectionManager;
        private GameMission modifiedMission;

        private void StartUI()
        {
            var Timeline = GameObject.Find("Timeline").transform.Find("Container");
            timelineAsset.GetComponentInChildren<Text>().text = gameObject.name;
            timelineAsset.GetComponentInChildren<Button>().onClick.AddListener(() => gameManager.MapViewSelectionControl(new Ray(), gameObject.transform.Find("SectionLeader").gameObject.transform.Find("Cube").gameObject));
            timelineAsset.transform.SetParent(Timeline.transform);
            TotalTurns = GameObject.Find("GameManager").GetComponent<GameManager>().NumberOfTurns;

            for (int i = 0; i < TotalTurns; i++)
            {
                GameObject Cell = Instantiate(sectionManager.CellPrefab);
                Cell.GetComponentInChildren<Text>().text = (i + 1).ToString();
                Cell.transform.SetParent(timelineAsset.GetComponentInChildren<HorizontalLayoutGroup>().transform);
            }

            var progressBar = sectionManager.gameManager.ProgressBar;
            progressBar.GetComponent<RectTransform>().position = new Vector2(125 + gameManager.TurnCellWidth / 2, 75);

            Button[] ButtonList;
            var Triangle = sectionManager.gameManager.Triangle;
            ButtonList = Triangle.GetComponentsInChildren<Button>(true);
            foreach (Button button in ButtonList)
            {
                button.onClick.AddListener(() => CreateAction(3, button.GetComponentInChildren<Text>().text));
            }
        }
        public void CreateAction(int TurnSpan, string ActionName)
        {
            if (!sectionManager.Selected) return;
            else if (TurnSpan + LastTurnWithAction > TotalTurns)
            {
                TurnSpan = TotalTurns - LastTurnWithAction;
                if (TurnSpan == 0) return;
            };

            if (sectionManager.ActionsList.Count != 0 && sectionManager.Performing)
            {
                sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].KillAction();
                sectionManager.ActionsList.RemoveAt(sectionManager.ActionsList.Count - 1);
            }

            var Panel = timelineAsset.transform.Find("Panel");
            CellWidth = Mathf.Abs(Panel.GetComponent<RectTransform>().rect.width / TotalTurns);

            var _start = LastTurnWithAction >= sectionManager.gameManager.CurrentTurn - 1
                   ? LastTurnWithAction
                   : sectionManager.gameManager.CurrentTurn - 1;
            var _action = new GameMission(
                sectionManager.ActionsList.Count,
                ActionName,
                _start,
                TurnSpan + _start,
                Panel,
                this
                );

            if (sectionManager.ActionsList.Count == 0) sectionManager.CurrentMission = _action;

            sectionManager.ActionsList.Add(_action);
            HideValidationButtons();
        }


        public void DisplaceTimelineElement()
        {
            if (sectionManager.ActionsList.Count <= 0) return;
            GraphicRaycaster Raycaster = timelineAsset.transform.parent.GetComponentInParent<GraphicRaycaster>();
            List<RaycastResult> results = new List<RaycastResult>();

            PointerEventData pointer = new PointerEventData(sectionManager.m_EventSystem)
            {
                position = Input.mousePosition
            };
            Raycaster.Raycast(pointer, results);

            if (results.Count > 0)
            {
                //Turn a Raycasted Cell into an int from it's displayed text.
                int HoveredCell = 0;
                RaycastResult CellInResults = results.Find(raycastedObject => raycastedObject.gameObject.name.Contains("Cell"));

                if (CellInResults.isValid)
                {
                    HoveredCell = int.Parse(CellInResults.gameObject.GetComponentInChildren<Text>().text);
                }
                else return;

                bool ExeccuteMainFunction = false;
                bool IsTemporary = !Displacing || !modifiedMission.IsValid;

                if (Input.GetMouseButtonUp(0) && Displacing)
                {
                    var _mr = modifiedMission.MapObject.GetComponentInChildren<MeshRenderer>();
                    var c = _mr.material.color;
                    c.a = 0.3f;
                    _mr.material.color = c;

                    var _ov = sectionManager.ActionsList[modifiedMission.Number];
                    _ov.EndingTurn = HoveredCell;
                    sectionManager.ActionsList[modifiedMission.Number] = _ov;
                    if (IsTemporary && HoveredCell == modifiedMission.ValidIndex)
                    {
                        modifiedMission.SetValid();
                    }

                    Displacing = false;
                }


                if (Displacing)
                {
                    bool NotBackwards = HoveredCell > modifiedMission.StartingTurn;
                    bool AtEdgeOfTimeline = sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].EndingTurn + (HoveredCell - modifiedMission.EndingTurn) > TotalTurns;
                    bool DownComing = HoveredCell < gameManager.CurrentTurn;
                    bool NotNull = HoveredCell > 0;

                    ExeccuteMainFunction = NotNull && NotBackwards && !AtEdgeOfTimeline && !DownComing;
                }

                if (ExeccuteMainFunction)
                {
                    int PreviousEndingTurn = modifiedMission.EndingTurn;

                    int OverlaySize = HoveredCell - modifiedMission.StartingTurn;
                    modifiedMission.TimelineObject.GetComponent<RectTransform>().sizeDelta = new Vector2(OverlaySize * CellWidth, 30);

                    if (!IsTemporary && HoveredCell != modifiedMission.ValidIndex)
                    {
                        modifiedMission.SetTemporary();
                        if (modifiedMission.ValidIndex !=0) modifiedMission.HasChanged = true;
                    }
                    else if (IsTemporary && HoveredCell == modifiedMission.ValidIndex) 
                    { 
                        modifiedMission.SetValid();
                        if (modifiedMission.ValidIndex != 0) modifiedMission.HasChanged = false;
                    }
                   
                    if (sectionManager.ActionsList.Count <= 0 || HoveredCell < gameManager.CurrentTurn) return;
                    else if (modifiedMission.Number == sectionManager.ActionsList.Count - 1) LastTurnWithAction = HoveredCell;

                    modifiedMission.EndingTurn = HoveredCell;

                    foreach (GameMission _mission in sectionManager.ActionsList.ToArray())
                    {
                        int _endingTurn = _mission.EndingTurn;
                        int _startingTurn = _mission.StartingTurn;
                        int _turnSpan = _endingTurn - _startingTurn;

                        if (_mission.Number <= modifiedMission.Number) continue; //Move only the elements that comes After.
                        else if (modifiedMission.EndingTurn != PreviousEndingTurn)
                        {
                            if (_mission.IsValid)
                            {
                                _mission.SetTemporary();
                                if (_mission.ValidIndex != 0) _mission.HasChanged = true;
                            }
                            _mission.StartingTurn = sectionManager.ActionsList[_mission.Number - 1].EndingTurn;
                            _mission.EndingTurn = _mission.StartingTurn + _turnSpan;
                            if (_mission.StartingTurn < gameManager.CurrentTurn)
                            {
                                _mission.StartingTurn = _startingTurn;
                                _mission.EndingTurn = _endingTurn;
                            }
                            if (!_mission.IsValid && _mission.EndingTurn == _mission.ValidIndex)
                            {
                                _mission.SetValid();
                                _mission.HasChanged = false;
                            }

                        }
                        _mission.TimelineObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(_mission.StartingTurn * CellWidth, -15);
                        sectionManager.ActionsList[_mission.Number] = _mission;

                    }
                    LastTurnWithAction = sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].EndingTurn;

                }

                if (results[0].gameObject.name.StartsWith("Edge"))
                {

                    if (Input.GetMouseButtonDown(0))
                    {
                        var ValidAction = sectionManager.ActionsList.Find(o => o.TimelineObject == results[0].gameObject.transform.parent.gameObject);
                        modifiedMission = ValidAction;
                        if (modifiedMission == null) return;
                        if (modifiedMission.Passed) return;
                        if (modifiedMission is object)
                        {
                            Displacing = true;
                            var c = modifiedMission.MapObject.GetComponentInChildren<MeshRenderer>().material.color;
                            c.a = 1;
                            modifiedMission.MapObject.GetComponentInChildren<MeshRenderer>().material.color = c;
                        }
                    }
                }
                var checkOwnership = results.Find(o =>
                {
                    var res = o.gameObject.transform.parent.parent.gameObject;
                    return res == timelineAsset;
                });

                if (Input.GetMouseButtonDown(1) && sectionManager.ActionsList.Count > 0 && results.Contains(checkOwnership))
                {
                    sectionManager.ActionsList[sectionManager.ActionsList.Count - 1].KillAction();
                }
            }
        }

        public void DrawMission()
        {
            if (sectionManager.ActionsList.Count <= 0) return;
            if (!sectionManager.Selected) return;

            GameMission LastRecordedAction = sectionManager.ActionsList[sectionManager.ActionsList.Count - 1];
            Ray ray = gameManager.MapView.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit _hit, Mathf.Infinity, gameManager.MapRayCastLayer))
            {
                _hit.point += Vector3.up;
                if (LastRecordedAction.ActionRecon) LastRecordedAction.TakeMissionRecon(_hit);
                else if (LastRecordedAction.ActionAttack) LastRecordedAction.TakeMissionAttack(_hit);
                else if (LastRecordedAction.ActionElement) LastRecordedAction.TakeMissionSupport(_hit);
                else if (LastRecordedAction.ActionOverwatch) LastRecordedAction.TakeMissionOverwatch(_hit);
            }
        }


        public void DisplayValidationButtons()
        {
            var cancelOrSend = sectionManager.gameManager.SendButtonsContainer;
            cancelOrSend.SetActive(true);
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.AddListener(() => sectionManager.CancelPendingActions(false));
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.AddListener(() => sectionManager.SendPendingActions());
        }

        public void HideValidationButtons()
        {
            var cancelOrSend = sectionManager.gameManager.SendButtonsContainer;
            cancelOrSend.SetActive(false);
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.RemoveAllListeners();
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.RemoveAllListeners();
        }

    }


    #endregion
}
