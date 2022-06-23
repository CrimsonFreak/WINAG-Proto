using Tools;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using static UnityEngine.Object;
using static Actions.ActionTools;
using static Actions.ActionUIManager;

namespace Actions
{
    #region Setup

    public class PointAtParameters
    {
        public GameObject Target { get; set; }
        public Outline CurrentOutline { get; set; }
    }

    public class MovementParameters
    {
        public GameObject Target { get; set; }
        public Vector3 TargetPosition { get; set; }
        public Vector3[] Path { get; set; }
        public Vector3 Origin { get; set; }
        public float PathLengthInMeters { get; set; }
        public LineRenderer Line { get; set; }
        public GameObject Circle { get; set; }
        public Outline CurrentOutline { get; set; }
        public float AgentSpeed { get; set; }
    }

    public class EmbarkParameters
    {
        public TransportManager Vehicle { get; set; }
        public PlayerUnit UnitController { get; set; }
        public float HighestAnchorPos { get; set; }
    }

    public class SectorParameters
    {
        public Mesh SectorMesh { get; set; }
        public LineRenderer SectorLine1 { get; set; }
        public LineRenderer SectorLine2 { get; set; }
        public GameObject Sector { get; set; }
        public Vector3 Origin { get; set; }

        public Vector3 Corner1 { get; set; }
        public Vector3 Corner2 { get; set; }
    }

    public class UnitAction
    {
        public UnitAction(ActionTypes _type, PlayerUnit unit)
        {
            Type = _type;
            PlayerUnit = unit;
            if (unit.ActionArray.Length == 0)
            {
                ActionBarSlice = unit.actionBar.transform.Find("Image").gameObject;
            }
            else
            {
                ActionBarSlice = Object.Instantiate(unit.actionBar.transform.Find("Image").gameObject, unit.actionBar);
            }

            elements = ActionTypeToObject(_type, this);
            unit.AddAction(this);
        }
        public PlayerUnit PlayerUnit;
        public ActionTypes Type;
        public IAction elements;
        public bool active = false;
        public int State { get; set; }
        public bool eventOnEnd = false;
        public int actionPointsCost;
        public GameObject ActionBarSlice;
        public GameObject RemoteActionBarSlice;
        public Color BaseActionColor = Color.green;
    }

    #endregion

    #region Process

    public interface IAction
    {
        UnitAction Caller { get; set; }

        void Draw();
        void Set();
        void Execute();
        void DeleteUI();
    }
    public class Overwatch : IAction
    {
        public SectorParameters Parameters;
        public UnitAction Caller { get; set; }

        public Overwatch(UnitAction _caller)
        {
            Caller = _caller;
            Caller.State = 1;
            Caller.active = true;

            Parameters = new SectorParameters()
            {
                Origin = _caller.PlayerUnit.ReferenceObjectPosition
            };
            CreateSectorUI(Parameters);
            _caller.actionPointsCost = 30;
        }


        public void Draw()
        {
            Ray ray = gameManager.SceneView.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("floor")))
            {
                Vector3 point = hit.point;
                point.y = 0.1f;
                SectorParameters _params = (SectorParameters)Parameters;
                Caller.State = UpdateSectorUI(_params, point, Caller.State);
            }
        }

        public void Set()
        {
            Caller.active = false;
            Parameters.Corner1 = Parameters.SectorLine1.GetPosition(1);
            Parameters.Corner2 = Parameters.SectorLine2.GetPosition(1);
        }

        public void Execute()
        {

        }

        public void DeleteUI()
        {
            Destroy(Parameters.Sector);
            Caller.actionPointsCost = 0;
        }


    }

    public class Movement : IAction
    {

        UnitAction embarking = null;
        public MovementParameters Parameters { get; set; }
        public UnitAction Caller { get; set; }

        public Movement(UnitAction _caller)
        {
            Caller = _caller;
            Caller.active = true;

            Parameters = new MovementParameters()
            {
                Origin = _caller.PlayerUnit.ReferenceObjectPosition,
                //AgentSpeed = Caller.PlayerUnit.GetComponent<UnitController>().Agent.speed / Caller.PlayerUnit.gameManager.DistanceMultiplier
            };
            CreateMovementUI(Parameters as MovementParameters, GameObject.Find("Actions"));
        }

        public void Draw()
        {
            Vector2 mousePosition = Input.mousePosition;
            Ray ray = gameManager.SceneView.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.CompareTag("ground"))
                {
                    if (Parameters.Target != null)
                    {
                        //_params.Target.GetComponentInParent<TransportManager>().ActionIndicator.SetActive(false);
                        Parameters.CurrentOutline.enabled = false;
                        Parameters.CurrentOutline = null;
                        Parameters.Target = null;
                        Destroy(embarking.ActionBarSlice);
                        Destroy(embarking.RemoteActionBarSlice);
                        var em = embarking.elements as Embark;
                        var _paramsv = em.Parameters;
                        var vehicle = _paramsv.Vehicle;
                        Caller.PlayerUnit.RemoveAction(embarking, false);
                        vehicle.GetComponent<PlayerUnit>().RemoveAction(embarking, false);

                        embarking = null;
                    }

                    Parameters.TargetPosition = new Vector3(hit.point.x, 0.1f, hit.point.z);
                    Parameters.Path = new Vector3[]
                    {
                         new Vector3(Caller.PlayerUnit.ReferenceObjectPosition.x, 0.1f, Caller.PlayerUnit.ReferenceObjectPosition.z),
                        Parameters.TargetPosition,
                    };

                    UpdateMovementUI(Parameters, 2);
                    Caller.actionPointsCost = CalculateActionPoints(Caller.Type, Parameters);
                }

                else if (hit.collider.CompareTag("Embarkable") && Caller.PlayerUnit.CanEmbark && Parameters.Target == null)
                {
                    Parameters.Target = hit.collider.gameObject;
                    Parameters.TargetPosition = Parameters.Target.transform.parent.Find("EmbarkingPoint").position;
                    Parameters.CurrentOutline = Parameters.Target.GetComponentInParent<Outline>(true);
                    Parameters.CurrentOutline.enabled = true;
                    Parameters.Path = new Vector3[]
                    {
                         new Vector3(Caller.PlayerUnit.ReferenceObjectPosition.x, 0.1f, Caller.PlayerUnit.ReferenceObjectPosition.z),
                         Parameters.TargetPosition,
                    };
                    var dist = Vector3.Distance(Parameters.Path[0], Parameters.Target.transform.position) / Caller.PlayerUnit.gameManager.DistanceMultiplier;
                    if (dist < 5f)
                    {
                        Caller.actionPointsCost = 0;
                    }
                    else Caller.actionPointsCost = CalculateActionPoints(Caller.Type, Parameters);
                    TransportManager transport;
                    if (Parameters.Target.GetComponentInParent<GhostManager>() == null)
                    {
                        transport = Parameters.Target.GetComponentInParent<TransportManager>();
                        transport.ActionIndicator.SetActive(true);
                    }
                    else
                    {
                        Parameters.Target = Parameters.Target.GetComponentInParent<GhostManager>().PlayerUnit.gameObject;
                        transport = Parameters.Target.GetComponent<TransportManager>();
                        transport.ActionIndicator.SetActive(true);
                    }

                    embarking = CreateEmbarkment(Caller.PlayerUnit, transport);

                    UpdateMovementUI(Parameters, 2);
                }
            }
        }

        public void Set()
        {

            if (Caller.PlayerUnit.Ghost != null) Destroy(Caller.PlayerUnit.Ghost);
            GameObject Ghost = Caller.PlayerUnit.Ghost = Instantiate(Caller.PlayerUnit.GhostPrefab);
            Ghost.GetComponent<GhostManager>().PlayerUnit = Caller.PlayerUnit;
            Ghost.transform.position = Parameters.TargetPosition + Vector3.up;
            Ghost.transform.LookAt(Caller.PlayerUnit.ReferenceObjectPosition);
            var rot = Ghost.transform.eulerAngles;
            Ghost.transform.eulerAngles = new Vector3(0, rot.y - 180, 0);
            if (Caller.PlayerUnit.GetComponent<TransportManager>() != null) Ghost.transform.eulerAngles = new Vector3(0, rot.y, 0);

            if (Parameters.Target != null)
            {
                if (Parameters.Target.CompareTag("Embarkable"))
                {
                    Transform vehicleTransform = Parameters.Target.transform.parent;
                    Transform door = vehicleTransform.Find("Door");

                    door.GetComponent<Animator>().enabled = true;
                    Parameters.CurrentOutline.enabled = false;
                    Parameters.CurrentOutline = null;
                    //_params.Target.GetComponentInParent<TransportManager>().ActionIndicator.SetActive(false);
                    Caller.eventOnEnd = true;
                }
            }

            Caller.active = false;

            Caller.PlayerUnit.ReferenceObjectPosition = Parameters.TargetPosition;
            Caller.PlayerUnit.DisplayActionUI();
        }

        public void Execute()
        {
            NavMeshAgent agent = Caller.PlayerUnit.GetComponent<NavMeshAgent>();
            if (!agent.hasPath && !agent.pathPending) agent.SetDestination(Parameters.TargetPosition);
        }

        public void DeleteUI()
        {
            Destroy(Parameters.Circle);
            Destroy(Caller.PlayerUnit.Ghost);
        }
    }

    public class Shoot : IAction
    {
        public PointAtParameters Parameters;
        public UnitAction Caller { get; set; }

        public Shoot(UnitAction caller)
        {
            Caller = caller;
            Caller.active = true;
            Caller.BaseActionColor = Color.green;
            Caller.actionPointsCost = 30;
            Parameters = new PointAtParameters();
        }

        public void Draw()
        {
            Vector2 mousePosition = Input.mousePosition;
            Ray ray = gameManager.SceneView.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                Transform TargetUnit = hit.collider.transform;
                if (TargetUnit.name.Contains("Multi") || TargetUnit.name.Contains("Infanterie")) TargetUnit = hit.collider.transform.parent;

                if ((TargetUnit.CompareTag("EnemyUnit") || TargetUnit.CompareTag("NeutralUnit")) && Parameters.Target == null)
                {
                    if (TargetUnit.GetComponent<Outline>() == null)
                    {
                        Parameters.CurrentOutline = TargetUnit.gameObject.AddComponent<Outline>();
                        Parameters.CurrentOutline.OutlineWidth = 2;
                    }
                    Parameters.Target = TargetUnit.gameObject;
                }
                else if (!(TargetUnit.CompareTag("EnemyUnit") || TargetUnit.CompareTag("NeutralUnit")) && Parameters.Target != null)
                {
                    Destroy(Parameters.CurrentOutline);
                    Parameters = new PointAtParameters();
                }
            }
        }
        public void Set()
        {
            if (Parameters.Target == null)
            {
                Destroy(Parameters.CurrentOutline);
                Caller.PlayerUnit.RemoveAction(Caller);
            }
            else
            {
                Caller.active = false;
            }
        }
        public void Execute()
        {
            var DetectedUnitsAsTargets =
                from unit in Caller.PlayerUnit.detectedUnits
                where unit.Enemy == Parameters.Target.GetComponent<EnemyUnitManager>()
                select unit;
            foreach (var Target in DetectedUnitsAsTargets)
            {
                bool alreadySetup = RealtimeDuelsManager.Duels.Exists(a => a.enemy == Target.Enemy && a.playerUnit == Caller.PlayerUnit);
                if (!alreadySetup)
                {
                    Duel duel = new Duel(Target.Enemy, Caller.PlayerUnit) { playerTargetMod = 1 + Target.DetectionValue * 0.2f };
                    RealtimeDuelsManager.Duels.Add(duel);
                }
            }
        }
        public void DeleteUI()
        {
            Destroy(Parameters.CurrentOutline);
        }

    }

    public class Wait : IAction
    {
        public EmbarkParameters Parameters;
        public UnitAction Caller { get; set; }

        public Wait(UnitAction caller)
        {
            Caller = caller;
            Caller.BaseActionColor = Color.yellow;
        }

        public void Draw()
        {

        }
        public void Set()
        {

        }
        public void Execute()
        {

        }
        public void DeleteUI()
        {

        }

    }

    public class Embark : IAction
    {
        public EmbarkParameters Parameters;
        public UnitAction Caller { get; set; }

        public Embark(UnitAction caller)
        {
            Caller = caller;
            caller.actionPointsCost = 30;
        }

        public void Draw()
        {

        }
        public void Set()
        {

        }
        public void Execute()
        {

        }
        public void DeleteUI()
        {

        }
    }

    #endregion
}
