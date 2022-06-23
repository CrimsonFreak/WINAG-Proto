using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;
using static Missions.MissionsTools;

namespace Tools
{

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
            AssignListenerToMissionButtons(sectionManager);
        }

        public float CellWidth;
        public int TotalTurns;
        public int LastTurnWithAction;
        public bool Displacing = false;

        public GameObject timelineAsset;
        public GameObject gameObject;
        public GameManager gameManager;
        public SectionManager sectionManager;
        private Mission modifiedMission;

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

            
        }


        public void DisplaceTimelineElement()
        {
            if (sectionManager.MissionsList.Count <= 0) return;
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

                    var _ov = sectionManager.MissionsList[modifiedMission.Number];
                    _ov.EndingTurn = HoveredCell;
                    sectionManager.MissionsList[modifiedMission.Number] = _ov;
                    if (IsTemporary && HoveredCell == modifiedMission.ValidIndex)
                    {
                        modifiedMission.SetValid();
                    }

                    Displacing = false;
                }


                if (Displacing)
                {
                    bool NotBackwards = HoveredCell > modifiedMission.StartingTurn;
                    bool AtEdgeOfTimeline = sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].EndingTurn + (HoveredCell - modifiedMission.EndingTurn) > TotalTurns;
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
                        if (modifiedMission.ValidIndex != 0) modifiedMission.HasChanged = true;
                    }
                    else if (IsTemporary && HoveredCell == modifiedMission.ValidIndex)
                    {
                        modifiedMission.SetValid();
                        if (modifiedMission.ValidIndex != 0) modifiedMission.HasChanged = false;
                    }

                    if (sectionManager.MissionsList.Count <= 0 || HoveredCell < gameManager.CurrentTurn) return;
                    else if (modifiedMission.Number == sectionManager.MissionsList.Count - 1) LastTurnWithAction = HoveredCell;

                    modifiedMission.EndingTurn = HoveredCell;

                    foreach (Mission _mission in sectionManager.MissionsList.ToArray())
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
                            _mission.StartingTurn = sectionManager.MissionsList[_mission.Number - 1].EndingTurn;
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
                        sectionManager.MissionsList[_mission.Number] = _mission;

                    }
                    LastTurnWithAction = sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].EndingTurn;

                }

                if (results[0].gameObject.name.StartsWith("Edge"))
                {

                    if (Input.GetMouseButtonDown(0))
                    {
                        var ValidAction = sectionManager.MissionsList.Find(o => o.TimelineObject == results[0].gameObject.transform.parent.gameObject);
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

                if (Input.GetMouseButtonDown(1) && sectionManager.MissionsList.Count > 0 && results.Contains(checkOwnership))
                {
                    sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].KillMission();
                }
            }
        }

        public void DrawMission()
        {
            if (sectionManager.MissionsList.Count <= 0 || !sectionManager.Selected) return;

            Mission lastRecordedMission = sectionManager.MissionsList[sectionManager.MissionsList.Count - 1];
            Ray ray = gameManager.MapView.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gameManager.MapRayCastLayer))
            {
                hit.point += Vector3.up;
                lastRecordedMission.Process.Draw(hit);
            }
        }


        public void DisplayValidationButtons()
        {
            var cancelOrSend = sectionManager.gameManager.SendButtonsContainer;
            cancelOrSend.SetActive(true);
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.AddListener(() => sectionManager.CancelPendingMissions(false));
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.AddListener(() => sectionManager.SendPendingMissions());
        }

        public void HideValidationButtons()
        {
            var cancelOrSend = sectionManager.gameManager.SendButtonsContainer;
            cancelOrSend.SetActive(false);
            cancelOrSend.GetComponentsInChildren<Button>()[0].onClick.RemoveAllListeners();
            cancelOrSend.GetComponentsInChildren<Button>()[1].onClick.RemoveAllListeners();
        }

    }

    public static class Palette
    {
        public static readonly Color playerUnit = new Color(42f / 255f, 122f / 255f, 252f / 255f);
        public static readonly Color enemyUnit = new Color(185f / 255f, 0, 0);
        public static readonly Color selectedGreen = new Color(0, 200f / 255f, 0);

        public static readonly Color attackRed = new Color(1, 37f / 255f, 37f / 255f);
        public static readonly Color reconGreen = new Color(11f / 255f, 136f / 255f, 0);
        public static readonly Color supportBlue = new Color(0 / 255f, 152f / 255f, 221f / 255f);
        public static readonly Color overwatchTeal = new Color(0, 225f / 255f, 180f / 255f);

        public static readonly Color fullGreen = new Color(58f / 255f, 1, 0);
        public static readonly Color midOrange = new Color(226f / 255f, 145f / 255f, 0);
        public static readonly Color lowRed = new Color(157f / 255f, 0, 0);

        public static readonly Color emptyGrey = new Color(144f / 255f, 144f / 255f, 144f / 255f);
    }

    public static class DefineChildButtons
    {

        private static string[] Move = new string[]
        {
            "Rapide",
            "Eclairer"
        };
        private static string[] Combat = new string[]
        {
            "Tirer",
            "Assaut"
        };
        private static string[] Distance = new string[]
        {
            "Brèche",
            "Disperser"
        };
        private static string[] Contact = new string[]
        {
            "Brèche",
            "Disperser",
            "Placer"
        };

        private static string[] Allié = new string[]
        {
            "Embarquer",
            "Soutien"
        };

        private static string[] Garde = new string[]
        {
            "Observer",
            "Fumigène"
        };

        public static string[] SetChilds(string Name)
        {
            switch (Name)
            {
                case "Déplacer": return Move;
                case "Garde": return Garde;
                case "Distance": return Distance;
                case "Contact": return Contact;
                case "Allié": return Allié;
                case "Combat": return Combat;
                default:
                    Debug.LogError("Famille d'action non reconnue");
                    return new string[0];
            }
        }

    }

    public class DetectedUnit
    {
        public EnemyUnitManager Enemy;
        public int DetectionValue;
        public bool InSight;
    }

    public static class GenericTools
    {

        public static int IndexFromMask(int mask)
        {
            for (int i = 0; i < 32; ++i)
            {
                if ((1 << i & mask) != 0)
                {
                    return i;
                }
            }
            return -1;
        }
        public static Vector3 CalculateTriangleCentroid(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = new Vector3
            {
                x = (a.x + b.x + c.x) / 3,
                y = 0,
                z = (a.z + b.z + c.z) / 3
            };

            return center;
        }

        public static Vector2 CalculatePointOnCircle(Vector2 Origin, Vector2 Point, float radAngle)
        {
            float x = ((Point.x - Origin.x) * Mathf.Cos(radAngle) - (Point.y - Origin.y) * Mathf.Sin(radAngle)) + Origin.x;
            float y = ((Point.x - Origin.x) * Mathf.Sin(radAngle) + (Point.y - Origin.y) * Mathf.Cos(radAngle)) + Origin.y;
            return new Vector2(x, y);
        }

        public static float GetAngleFromPoints(Vector3 center, Vector3 a, Vector3 b)
        {
            var ang = (Mathf.Atan2(b.z - center.z, b.x - center.x) - Mathf.Atan2(a.z - center.z, a.x - center.x)) * Mathf.Rad2Deg;
            return Mathf.Abs(ang);
        }

        public static double GetTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Math.Abs((a.x * (b.z - c.z) +
                             b.x * (c.z - a.z) +
                             c.x * (a.z - b.z)) / 2.0);
        }

        public static bool IsInsideTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            double Total = GetTriangleArea(a, b, c);
            double A1 = GetTriangleArea(a, b, p);
            double A2 = GetTriangleArea(a, c, p);
            double A3 = GetTriangleArea(b, c, p);

            return (Total == A1 + A2 + A3);
        }


        public static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;

            foreach (Transform child in obj.transform)
            {
                if (child.name == "Icon") continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

    }

    public static class RealtimeDuelsManager
    {
        public static List<Duel> Duels = new List<Duel>();

        public static void ResolveDuels()
        {
            foreach (Duel duel in Duels)
            {
                float pmods = GetCombatMods(true, duel);
                float emods = GetCombatMods(false, duel);
                float cv = duel.playerUnit.CombatValue * pmods;
                float cve = duel.enemy.CombatValue * emods;
                Debug.Log(cv / (cv + cve));
                float playerDamage = cv / (cv + cve);
                float enemyDamage = cve / (cv + cve);
                bool pcrit = Mathf.FloorToInt(UnityEngine.Random.Range(0,10)) + 1 == 10;
                bool ecrit = Mathf.FloorToInt(UnityEngine.Random.Range(0, 10)) + 1 == 10;
                int pdmg = Mathf.FloorToInt(UnityEngine.Random.Range(0, playerDamage * 100));
                int edmg = Mathf.FloorToInt(UnityEngine.Random.Range(0, enemyDamage * 100));
                pdmg += pcrit ? pdmg : 0;
                edmg += ecrit ? edmg : 0;
                Debug.Log($"Enemy: {duel.enemy.name} deals {edmg} (crit: {ecrit}) \n" +
                    $"Base damage: {enemyDamage} as combat value  = {cve/emods} * {emods}(mods)\n\n" +
                    $"Player: {duel.playerUnit.name} deals {pdmg} (crit: {pcrit})\n" +
                    $"Base damage: {playerDamage} as combat value  = {cv/pmods} * {pmods}(mods)");
                duel.playerUnit.OperationalValue -= edmg;
            }
        }

        public static float GetCombatMods(bool IsPlayer, Duel duel)
        {
            float mods = 1;
            if (IsPlayer)
            {
                mods += duel.playerUnit.ComsRange != 0 ? 0.2f: 0;
                mods += duel.playerTargetMod;
            }
            else
            {
                mods += duel.enemy.IsInComsRange ? 0.2f : 0;
                mods += duel.enemyTargetMod;
            }
            return mods;
        }
    }

    public class Duel
    {
        public EnemyUnitManager enemy;
        public PlayerUnit playerUnit;
        public float playerTargetMod;
        public float enemyTargetMod = 0;

        public Duel(EnemyUnitManager _enemy, PlayerUnit _playerUnit)
        {
            enemy = _enemy;
            playerUnit = _playerUnit;
        }
    }

}

