using System;
using System.IO;
using System.Text;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using Missions;
using static Actions.ActionTools;
using static UnityEngine.Object;

namespace Actions
{
    public static class ActionsData
    {
        private static string observerContent = File.ReadAllText("./Equilibrage/ModificateursDeAction/Observer.json", Encoding.UTF8);
        private static string tirerContent = File.ReadAllText("./Equilibrage/ModificateursDeAction/Tirer.json", Encoding.UTF8);
        private static string assautContent = File.ReadAllText("./Equilibrage/ModificateursDeAction/Assaut.json", Encoding.UTF8);
        private static string embarkContent = File.ReadAllText("./Equilibrage/ModificateursDeAction/Embarquer.json", Encoding.UTF8);

        public static ActionModifiers observer = JsonUtility.FromJson<ActionModifiers>(observerContent);
        public static ActionModifiers tirer = JsonUtility.FromJson<ActionModifiers>(tirerContent);
        public static ActionModifiers assaut = JsonUtility.FromJson<ActionModifiers>(assautContent);
        public static ActionModifiers embarquer = JsonUtility.FromJson<ActionModifiers>(embarkContent);
    }

    public static class ActionTools
    {
        public enum ActionTypes
        {
            Rapide,
            Eclairer,
            Tirer,
            Assaut,
            Brèche,
            Disperser,
            Placer,
            Embarquer,
            Soutien,
            Observer,
            Fumigène,
            None
        }

        public static ActionTypes ActionNameToEnum(string Name)
        {
            return Name switch
            {
                "Rapide" => ActionTypes.Rapide,
                "Tirer" => ActionTypes.Tirer,
                "Assaut" => ActionTypes.Assaut,
                "Observer" => ActionTypes.Observer,
                "Embarquer" => ActionTypes.Embarquer,
                "Brèche" => ActionTypes.Brèche,
                _ => throw new Exception("Erreur lors de la définition du type de Mission"),
            };
        }

        public static IAction ActionTypeToObject(ActionTypes Type, UnitAction caller)
        {
            return Type switch
            {
                ActionTypes.Tirer => new Shoot(caller),
                ActionTypes.Assaut => new Movement(caller),
                ActionTypes.Rapide => new Movement(caller),
                ActionTypes.Observer => new Overwatch(caller),
                ActionTypes.Brèche => new Movement(caller),
                ActionTypes.Embarquer => new Embark(caller),
                ActionTypes.None => new Wait(caller),
                _ => throw new Exception("Wrong ActionType"),
            };
        }

        public static int CalculateActionPoints(ActionTypes type)
        {
            switch (type)
            {
                case ActionTypes.Observer:
                    return 30;
                default: return 0;
            }

        }

        public static int CalculateActionPoints(ActionTypes type, MovementParameters _params)
        {
            switch (type)
            {
                case ActionTypes.Rapide:
                    float Dist = Vector3.Distance(_params.Path[1], _params.Path[0])/2;
                    return (int)Mathf.Floor(Dist);
                default: return 0;
            }

        }

        public static UnitAction CreateEmbarkment(PlayerUnit unit, TransportManager vehicle)
        {
            var vehicleUnit = vehicle.GetComponent<PlayerUnit>();

            int totalPointsForUnit = 0;
            int totalPointsForVehicle = 0;

            foreach (var action in vehicleUnit.ActionArray)
            {
                totalPointsForVehicle += action.actionPointsCost;
            }
            foreach (var action in unit.ActionArray)
            {
                totalPointsForUnit += action.actionPointsCost;
            }

            if (totalPointsForVehicle > totalPointsForUnit)
            {
                new UnitAction(ActionTypes.None, unit) { actionPointsCost = totalPointsForVehicle - totalPointsForUnit };
            }
            else
            {
                new UnitAction(ActionTypes.None, vehicleUnit) { actionPointsCost = totalPointsForUnit - totalPointsForVehicle };
            }


            UnitAction output = new UnitAction(ActionTypes.Embarquer, unit);
            Embark inheritedAction = output.elements as Embark;
            inheritedAction.Parameters = new EmbarkParameters()
            {
                UnitController = unit,
                Vehicle = vehicle
            };



            vehicleUnit.AddAction(output);

            if (vehicleUnit.ActionArray.Length == 0)
            {
                output.RemoteActionBarSlice = vehicleUnit.actionBar.transform.Find("Image").gameObject;
            }
            else
            {
                output.RemoteActionBarSlice = Instantiate(vehicleUnit.actionBar.transform.Find("Image").gameObject, vehicleUnit.actionBar);
            }

            return output;
        }


        public static int CheckDistToLeader(Transform Leader, Transform Unit)
        {
            int ComsRange;
            float distToLeader = Vector3.Distance(Unit.position, Leader.position);
            if (distToLeader > 200) ComsRange = 0;
            else if (distToLeader > 100) ComsRange = 1;
            else if (distToLeader > 50) ComsRange = 2;
            else ComsRange = 3;
            return ComsRange;
        }
        public static void SetLineProperties(LineRenderer line, Color color, Material material, float Width, int Layer)
        {
            line.gameObject.layer = Layer;
            line.positionCount = 2;
            line.startWidth = Width;
            line.endWidth = Width;
            line.material = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material.color = color;
        }
        public static void RunDetectionTest(PlayerUnit PlayerUnit, MissionModifiers missionModifiers, ActionModifiers actionModifiers)
        {
            Collider[] hitColliders = Physics.OverlapSphere(PlayerUnit.transform.position, 200, LayerMask.GetMask(new string[] { "Hidden", "Default" }));
            float AngleMod = 0;
            foreach (Collider hitCollider in hitColliders)
            {
                if (!hitCollider.CompareTag("EnemyUnit")) continue;
                EnemyUnitManager enemyManager = hitCollider.name.Contains("Multi")
                           ? hitCollider.transform.parent.GetComponent<EnemyUnitManager>()
                           : hitCollider.GetComponent<EnemyUnitManager>();

                UnitAction _action = PlayerUnit.ActionArray.Length != 0
                    ? PlayerUnit.ActionArray[0]
                    : null;

                if (_action != null && _action.Type == ActionTypes.Observer)
                {
                    Overwatch OwAction = (Overwatch)_action.elements;
                    var _params = OwAction.Parameters;
                    MeshRenderer sectorMesh = _params.Sector != null
                        ? _params.Sector.GetComponent<MeshRenderer>()
                        : null;

                    if (sectorMesh != null)
                    {
                        Vector3 colliderPosition = hitCollider.transform.position;

                        if (GenericTools.IsInsideTriangle(_params.Origin, _params.Corner1, _params.Corner2, colliderPosition))
                        {
                            float Angle = GenericTools.GetAngleFromPoints(_params.Origin, _params.Corner1, _params.Corner2);
                            AngleMod = 1 - (Angle / 180);
                        }
                    }
                }
                float apparentSurface = CastSurface(PlayerUnit.gameObject, hitCollider.gameObject, enemyManager.transform);

                float roll = UnityEngine.Random.Range(0f, 1f);
                double modsTotal = 1 + (missionModifiers.Observation + actionModifiers.Observation + AngleMod);
                float dist = (Vector3.Distance(PlayerUnit.transform.position, hitCollider.transform.position) / PlayerUnit.gameManager.DistanceMultiplier);
                float detection = (float)(modsTotal * apparentSurface * enemyManager.Indiscrétion) / dist;

                string test = detection + roll >= 1 ? "Réussi" : "Raté";

                GameObject.Find("DetectionInfo").GetComponent<Text>().text += (
                    "RESULTATS DE LA DETECTION POUR L'UNITE " + PlayerUnit.name + "\n\n"
                    + "Surface apparente: " + apparentSurface * 100 + "%\n"
                    + "Modificateur d'Angle: " + Mathf.Round(AngleMod * 100) + "%\n"
                    + "Modificateur d'Action: Observation " + actionModifiers.Observation * 100 + "%\n"
                    + "Modificateur de Mission: Observation " + missionModifiers.Observation * 100 + "%\n\n"
                    + "Pourcentage des modificateurs: " + Mathf.Round((float)modsTotal * 100) + "%\n"
                    + "Indiscrétion: " + enemyManager.Indiscrétion + " mètres\n"
                    + "Distance: " + dist + " mètres\n"
                    + $"Calcul: ({Mathf.Round((float)modsTotal * 100) / 100} x {apparentSurface} x {enemyManager.Indiscrétion}) / {dist}\n"
                    + "Chances de détecter: " + detection * 100 + "%\n"
                    + "Jet de Test: " + Mathf.Round(roll * 100) + "\n"
                    + "Résultat: " + test + "(Niveau d'information: " + Mathf.FloorToInt(detection + roll) + ")\n\n"
                );

                detection += roll;

                if (PlayerUnit.detectedUnits.Exists(d => d.Enemy == enemyManager))
                {
                    DetectedUnit detectedUnit = PlayerUnit.detectedUnits.Find(d => d.Enemy == enemyManager);
                    if (detectedUnit.DetectionValue < detection)
                    {
                        PlayerUnit.detectedUnits.Remove(detectedUnit);
                        PlayerUnit.detectedUnits.Add(new DetectedUnit()
                        {
                            Enemy = enemyManager,
                            DetectionValue = Mathf.FloorToInt((float)detection),
                            InSight = true
                        });
                        ResolveDetection(detection, enemyManager);
                    }
                }
                else
                {

                    PlayerUnit.detectedUnits.Add(new DetectedUnit()
                    {
                        Enemy = enemyManager,
                        DetectionValue = Mathf.FloorToInt((float)detection),
                        InSight = true
                    });

                }
            }
        }
        public static void ResolveDetection(float detectionValue, EnemyUnitManager enemy)
        {

            if (detectionValue >= 2)
            {
                var Icon = enemy.transform.Find("Icon");
                if (Icon != null) GameObject.Destroy(Icon.gameObject);
                GenericTools.SetLayerRecursively(enemy.gameObject, 0);
                enemy.InfoLevel = Mathf.FloorToInt(detectionValue);
            }

            else if (detectionValue >= 1)
            {
                if (enemy.transform.Find("Icon") || enemy.gameObject.layer == 0) return;
                var icon = GameObject.Instantiate(enemy.iconPrefab, enemy.transform);
                icon.name = "Icon";
                icon.transform.position = enemy.transform.position + Vector3.down * 0.9f;
                icon.layer = 11;
            }

        }
        public static float CastSurface(GameObject castingUnit, GameObject castedUnit, Transform MainTransform)
        {
            var rotation = MainTransform.rotation;
            MainTransform.LookAt(castingUnit.transform);

            Vector3 Position = castingUnit.transform.position;
            Vector3 Center = castedUnit.transform.position;
            float Width = MainTransform.localScale.x / 2f - 0.1f;
            float Height = MainTransform.localScale.y - 0.1f;

            Vector3 LeftBound = new Vector3(Center.x + Width, Center.y, Center.z);
            Vector3 RightBound = new Vector3(Center.x - Width, Center.y, Center.z);
            Vector3 UpBound = new Vector3(Center.x, Center.y + Height, Center.z);
            Vector3 DownBound = new Vector3(Center.x, Center.y - Height, Center.z);


            RaycastHit[] Hits = new RaycastHit[5];
            Physics.Linecast(Position, Center, out Hits[0]);
            Physics.Linecast(Position, LeftBound, out Hits[1]);
            Physics.Linecast(Position, RightBound, out Hits[2]);
            Physics.Linecast(Position, UpBound, out Hits[3]);
            Physics.Linecast(Position, DownBound, out Hits[4]);

            MainTransform.rotation = rotation;

            int checker = 0;
            for (int i = 0; i < 5; i++)
            {
                if (Hits[i].collider == null) continue;
                if (!Hits[i].collider.gameObject != castedUnit)
                {
                    checker++;
                }
            }
            return (float)(5 - checker) / 5;
        }
    }

    public class ActionPointsParams
    {
        public bool isMovement;
        public Vector3[] Movement;
        public bool isShoot;
        public Vector3 Target;
    }

    public static class ActionUIManager
    {
        public static GameManager gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();



        /*public static void CreatePointUI(PointAtParameters _params)
        {
            _params.Circle = GameObject.Instantiate(gameManager.CirclePrefab);
            //_params.Circle.transform.SetParent(button.transform.parent.parent.parent);
            _params.Circle.layer = 5;
            _params.Circle.transform.Rotate(new Vector3(90, 0, 0));
            _params.Circle.transform.localScale = new Vector3(1f, 0.01f, 1f);
        }

        public static void UpdatePointUI(PointAtParameters _params, Vector3 targetPoint)
        {
            if (!_params.Circle) return;

            _params.Circle.transform.position = targetPoint + new Vector3(0, 0.1f, 0);
            _params.Circle.GetComponent<MeshRenderer>().material.color = Color.red;
        }*/

        public static void CreateMovementUI(MovementParameters _params, GameObject Parent)
        {
            _params.Circle = GameObject.Instantiate(gameManager.CirclePrefab);
            _params.Circle.layer = 5;
            _params.Circle.transform.Rotate(new Vector3(90, 0, 0));
            _params.Circle.transform.localScale = new Vector3(1f, 0.01f, 1f);
            _params.Line = _params.Circle.AddComponent<LineRenderer>();
            _params.Line.generateLightingData = false;
            _params.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _params.Line.receiveShadows = false;
            _params.Line.alignment = LineAlignment.View;
            _params.Line.startWidth = _params.Line.endWidth = 0.5f;
            _params.Line.material = gameManager.BaseMaterial;
            _params.Line.material.color = Color.blue;
        }
        public static void UpdateMovementUI(MovementParameters _params, int ActionPoints)
        {
            Vector3 targetPoint = _params.TargetPosition;

            if (!_params.Circle) return;
            if (_params.PathLengthInMeters <= ActionPoints)
            {
                _params.Line.SetPositions(_params.Path);
                _params.Circle.transform.position = targetPoint + new Vector3(0, 0.1f, 0);
                _params.Circle.GetComponent<MeshRenderer>().material.color = Color.blue;
                _params.Line.enabled = true;
            }
            else
            {
                _params.Circle.transform.position = targetPoint + new Vector3(0, 0.1f, 0);
                _params.Circle.GetComponent<MeshRenderer>().material.color = Color.red;
                _params.Line.enabled = false;
            }
        }

        public static void CreateSectorUI(SectorParameters _params)
        {
            _params.Sector = new GameObject("Sector");
            //_params.Sector.transform.SetParent(button.transform.parent.parent.parent);
            _params.SectorLine1 = new GameObject("Line1").AddComponent<LineRenderer>();
            _params.SectorLine2 = new GameObject("Line2").AddComponent<LineRenderer>();
            _params.SectorLine1.transform.SetParent(_params.Sector.transform);
            _params.SectorLine2.transform.SetParent(_params.Sector.transform);
            SetLineProperties(_params.SectorLine1, Color.white, gameManager.BaseMaterial, 0.1f, 11);
            SetLineProperties(_params.SectorLine2, Color.white, gameManager.BaseMaterial, 0.1f, 11);

            _params.Sector.layer = 5;
            _params.Sector.AddComponent<MeshFilter>().mesh = _params.SectorMesh;
            var comp = _params.Sector.AddComponent<MeshRenderer>();
            comp.material = gameManager.BaseMaterial;
            comp.material.color = new Color(1, 0, 0, 0.15f);
        }
        public static int UpdateSectorUI(SectorParameters _params, Vector3 targetPoint, int state)
        {
            Vector3 origin = _params.Origin;

            if (state == 1)
            {
                _params.SectorLine1.SetPositions(new Vector3[] { origin + Vector3.up * 1f, targetPoint + Vector3.up * 1f });
                if (Input.GetMouseButtonDown(0))
                {
                    state++;
                    _params.SectorMesh = new Mesh();
                    _params.Sector.GetComponent<MeshFilter>().mesh = _params.SectorMesh;
                }
            }

            else if (state == 2)
            {
                float dist = Vector3.Distance(origin, _params.SectorLine1.GetPosition(1)) / Vector3.Distance(origin, targetPoint);
                Vector3 endPoint = new Vector3(origin.x + ((targetPoint.x - origin.x) * dist), 1f, origin.z + ((targetPoint.z - origin.z) * dist));
                _params.SectorLine2.SetPositions(new Vector3[]
                {
                    origin + Vector3.up*1f,
                    endPoint
                });

                _params.SectorMesh.vertices = new Vector3[]
                {
                    origin + Vector3.up*1f,
                    _params.SectorLine1.GetPosition(1),
                    _params.SectorLine2.GetPosition(1),
                };
                _params.SectorMesh.triangles = new int[]
                {
                    0, 1, 2, 0 , 2, 1
                };
                _params.SectorMesh.RecalculateBounds();

                if (Input.GetMouseButtonDown(0)) state = 0;
            }
            return state;
        }

        public static void DeleteUI(GameObject UIContainer)
        {
            GameObject.Destroy(UIContainer);
        }
    }

    public class ActionModifiers
    {
        public ActionModifiers(ActionTools.ActionTypes Type)
        {
            switch (Type)
            {
                case ActionTools.ActionTypes.Observer:
                    Observation = ActionsData.observer.Observation;
                    break;

                case ActionTools.ActionTypes.Tirer:
                    Observation = ActionsData.tirer.Observation;
                    break;

                case ActionTools.ActionTypes.Assaut:
                    Observation = ActionsData.assaut.Observation;
                    break;

                case ActionTools.ActionTypes.Embarquer:
                    Observation = ActionsData.embarquer.Observation;
                    break;

                default:
                    Observation = 0;
                    break;
            }
        }

        public ActionModifiers() { }


        public double Observation;

    }
}
