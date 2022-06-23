
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using static Actions.ActionTools;
using static Missions.MissionsTools;
using static SectionManager;

public class Mission : MonoBehaviour
{
    public SectionUITools sectionUITools;
    public SectionManager sectionManager;
    public GameObject Triangle;

    public MissionTypes Type;
    public IMissionMethods Process;
    public int Number;
    public int StartingTurn;
    public int EndingTurn;

    public List<Vector3> points = new List<Vector3>();
    public LineRenderer Line;
    public GameObject MapObject;
    public GameObject TimelineObject;
    public GameObject OriginalState;
    public GameObject Circle;
    public int ValidIndex = 0;

    public int State = 0;
    public bool Passed = false;
    public bool IsValid = false;
    public bool HasChanged = false;

    public void SetStartingAttributes(Transform Panel)
    {
        TimelineObject.transform.SetParent(Panel.transform);
        TimelineObject.GetComponent<RectTransform>().sizeDelta = new Vector2((EndingTurn - StartingTurn) * sectionUITools.CellWidth, 30);
        TimelineObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(StartingTurn * sectionUITools.CellWidth, -15, 0);
        TimelineObject.GetComponentInChildren<Text>().text = name;
        var i = TimelineObject.GetComponent<Image>();
        i.color = new Color(0.5f, 0.5f, 0.5f, i.color.a);
        sectionUITools.LastTurnWithAction = EndingTurn;
        Triangle = GameObject.Find("Triangle");
        Type = MissionNameToEnum(name);
        Process = Type switch
        {
            MissionTypes.Reconnaître => new MissionRecon(this),
            MissionTypes.Attaquer    => new MissionAttack(this),
            MissionTypes.Appuyer     => new MissionSupport(this),
            MissionTypes.Surveiller  => new MissionOverwatch(this),

            _ => throw new System.Exception("Erreur lors de la définition du type de Mission"),
        };
        transform.SetParent(sectionUITools.gameObject.transform);

        CreateBrokenLine();
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
        MapObject.transform.SetParent(transform);

        Circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Circle.transform.SetParent(MapObject.transform);
        Circle.transform.position = Vector3.zero;
        Circle.name = "Circle";
        Circle.layer = 3;
        Circle.GetComponent<MeshRenderer>().material.shader = sectionManager.TransparentShader;
        Circle.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.3f);

        if (name != "Surveiller") Circle.transform.localScale = new Vector3(2, 0.1f, 2);
        else Circle.transform.localScale = new Vector3(4, 0.1f, 4);
        Line = MapObject.AddComponent<LineRenderer>();
        Line.startWidth = 2f;
        Line.endWidth = 2f;
        Line.material = sectionManager.gameManager.BaseMaterial;
        Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Line.receiveShadows = false;
        if (sectionManager.MissionsList.Count <= 0) points.Add(sectionManager.gameObject.transform.position);
        else
        {
            int pos = sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].Line.positionCount - 1;
            points.Add(sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].Line.GetPosition(pos));
        }

        Process.Start();
    }
    public void RemoveInteractableIndicator()
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

    public void KillMission()
    {
        points = new List<Vector3>();
        sectionManager.Performing = false;
        Destroy(TimelineObject);
        Destroy(OriginalState);
        sectionUITools.LastTurnWithAction = sectionUITools.LastTurnWithAction >= StartingTurn
            ? StartingTurn
            : sectionUITools.LastTurnWithAction;
        StartCoroutine(WaitAndDestroy(sectionManager.MissionsList, gameObject));

        if (sectionManager.MissionsList.Count - 1 == 0 || sectionManager.MissionsList[sectionManager.MissionsList.Count - 2].IsValid)
        {
            sectionUITools.HideValidationButtons();
        }

    }

    private IEnumerator<WaitForEndOfFrame> WaitAndDestroy(List<Mission> list, GameObject element)
    {
        yield return new WaitForEndOfFrame();
        Destroy(element);
        list.Remove(element.GetComponent<Mission>());
    }

    public override string ToString() => $"({name}, {Number}, {StartingTurn}, {EndingTurn})";
}

public interface IMissionMethods
{
    Mission Mission { get; }

    void Start();
    void Draw(RaycastHit hit);
}

public class MissionOverwatch : IMissionMethods
{
    public Mission Mission { get; }
    public LineRenderer SectorLine1;
    public LineRenderer SectorLine2;
    public MeshRenderer renderer;
    public Mesh mesh;
    public MissionOverwatch(Mission _mission)
    {
        Mission = _mission;
    }

    public void Start()
    {
        Color c = Palette.overwatchTeal;
        c.a = 0.3f;
        Mission.Circle.GetComponent<MeshRenderer>().material.color = c;
        Mission.Line.material.color = Palette.overwatchTeal;
        mesh = Mission.MapObject.AddComponent<MeshFilter>().mesh = new Mesh(); ;

        renderer = Mission.MapObject.AddComponent<MeshRenderer>();
        renderer.gameObject.layer = 3;
        renderer.material = new Material(Mission.sectionManager.TransparentShader)
        {
            color = c
        };

    }

    public void Draw(RaycastHit hit)
    {
        Vector3 origin = Mission.Line.GetPosition(Mission.Line.positionCount - 1);
        if (Mission.State == 1 && hit.point != origin)
        {
            Vector3 endPoint1 = hit.point;
            SectorLine1.SetPositions(new Vector3[2] { origin, endPoint1 });
            if (Input.GetMouseButtonDown(0))
            {
                Mission.State = 2;
                Mission.sectionUITools.DisplayValidationButtons();
            }
        }

        else if (Mission.State == 2)
        {
            float dist = Vector3.Distance(origin, SectorLine1.GetPosition(1)) / Vector3.Distance(origin, hit.point);
            Vector3 endPoint2 = new Vector3(origin.x + ((hit.point.x - origin.x) * dist), 1f, origin.z + ((hit.point.z - origin.z) * dist));
            SectorLine2.SetPositions(new Vector3[2] { origin, endPoint2 });

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
                Mission.State = 0;
                Mission.sectionManager.Performing = false;
            }
        }

        else
        {
            Mission.MapObject.transform.position = hit.point;
            int i = 0;
            Mission.Line.positionCount = 1;
            foreach (Vector3 point in Mission.points)
            {
                Mission.Line.positionCount++;
                Mission.Line.SetPosition(i, point);
                i++;
            }
            Mission.Line.SetPosition(i, hit.point);


            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    GameObject newImg = Object.Instantiate(Mission.MapObject);
                    Object.Destroy(newImg.GetComponent<LineRenderer>());
                    newImg.transform.SetParent(Mission.MapObject.transform.parent);
                    newImg.transform.position = Input.mousePosition;
                    Mission.points.Add(hit.point);
                }
            }

            else if (Input.GetMouseButtonDown(0) && Mission.State == 0)
            {
                Mission.State = 1;

                SectorLine1 = new GameObject().AddComponent<LineRenderer>();
                SectorLine1.transform.SetParent(Mission.MapObject.transform);

                SectorLine2 = new GameObject().AddComponent<LineRenderer>();
                SectorLine2.transform.SetParent(Mission.MapObject.transform);

                SetLineProperties(SectorLine1, Color.white, Mission.sectionManager.gameManager.BaseMaterial, 1, 3);
                SetLineProperties(SectorLine2, Color.white, Mission.sectionManager.gameManager.BaseMaterial, 1, 3);
            }

            else if (Input.GetMouseButtonDown(1))
            {
                Mission.KillMission();
                Mission.sectionUITools.DisplayValidationButtons();
            }

        }
    }
}

public class MissionRecon : IMissionMethods
{
    public Mission Mission { get; }

    public MissionRecon(Mission _mission)
    {
        Mission = _mission;
    }

    public void Start()
    {
        Color c = Palette.reconGreen;
        c.a = 0.3f;
        Mission.Circle.GetComponent<MeshRenderer>().material.color = c;
        Mission.Line.material.color = Palette.reconGreen;
    }

    public void Draw(RaycastHit hit)
    {
        Mission.MapObject.transform.position = hit.point;
        int i = 0;
        Mission.Line.positionCount = 1;
        foreach (Vector3 point in Mission.points)
        {
            Mission.Line.positionCount++;
            Mission.Line.SetPosition(i, point);
            i++;
        }
        Mission.Line.SetPosition(i, hit.point);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameObject newCircle = Object.Instantiate(Mission.MapObject);
                Object.Destroy(newCircle.GetComponent<LineRenderer>());
                newCircle.transform.SetParent(Mission.MapObject.transform.parent);
                newCircle.transform.position = hit.point;
                Mission.points.Add(hit.point);
            }
        }

        else if (Input.GetMouseButtonDown(0))
        {
            Mission.MapObject.transform.SetAsLastSibling();
            Mission.points = new List<Vector3>();
            Mission.sectionManager.Performing = false;
            Mission.sectionUITools.DisplayValidationButtons();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Mission.KillMission();
            Mission.sectionUITools.DisplayValidationButtons();
        }

    }
}

public class MissionAttack : IMissionMethods
{

    public bool Scaling = false;
    public Mission Mission { get; }

    public MissionAttack(Mission _mission)
    {
        Mission = _mission;
    }

    public void Start()
    {
        Color c = Palette.attackRed;
        c.a = 0.3f;
        Mission.Circle.GetComponent<MeshRenderer>().material.color = c;
        Mission.Line.material.color = Palette.attackRed;
    }

    public void Draw(RaycastHit hit)
    {
        if (!Scaling)
        {
            Mission.MapObject.transform.position = hit.point;
            int i = 0;
            Mission.Line.positionCount = 1;
            foreach (Vector3 point in Mission.points)
            {
                Mission.Line.positionCount++;
                Mission.Line.SetPosition(i, point);
                i++;
            }
            Mission.Line.SetPosition(i, hit.point);
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetMouseButtonDown(0))
            {
                Mission.points.Add(hit.point);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Scaling = true;
            Mission.MapObject.transform.localScale = new Vector3(
                Mathf.Clamp(Vector3.Distance(Mission.MapObject.transform.position, hit.point), 1f, 15f),
                0.1f,
                Mathf.Clamp(Vector3.Distance(Mission.MapObject.transform.position, hit.point), 1f, 15f)
            );
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Mission.MapObject.transform.SetAsLastSibling();
            Scaling = false;
            Mission.points = new List<Vector3>();
            Mission.sectionManager.Performing = false;
            Mission.sectionUITools.DisplayValidationButtons();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Mission.KillMission();
            Mission.sectionUITools.DisplayValidationButtons();
        }

    }


}

public class MissionSupport : IMissionMethods
{
    public Mission Mission { get; }

    public MissionSupport(Mission _mission)
    {
        Mission = _mission;
    }

    public void Start()
    {
        var _Sects = Mission.sectionManager.gameManager.SectionList;
        for (int i = 0; i < _Sects.Count; i++)
        {
            if (!_Sects[i].Selected)
            {
                _Sects[i].transform.Find("SectionLeader").Find("Cube").GetComponent<MeshRenderer>().material.color = Palette.selectedGreen;
            }
        }
        Color c = Palette.supportBlue;
        c.a = 0.3f;
        Mission.Circle.GetComponent<MeshRenderer>().material.color = c;
        Mission.Line.material.color = Palette.supportBlue;
    }

    public void Draw(RaycastHit hit)
    {
        Mission.MapObject.transform.position = hit.point;
        int i = 0;
        Mission.Line.positionCount = 1;
        foreach (Vector3 point in Mission.points)
        {
            Mission.Line.positionCount++;
            Mission.Line.SetPosition(i, new Vector3(point.x, 1, point.z));
            i++;
        }
        Mission.Line.SetPosition(i, hit.point);

        if (Input.GetMouseButtonDown(0))
        {
            if (!hit.collider.CompareTag("Leader")) return;
            Mission.Line.SetPosition(1, hit.collider.transform.position);
            Mission.MapObject.transform.position = hit.point;
            Mission.sectionManager.Performing = false;
            Mission.points = new List<Vector3>();
            Mission.RemoveInteractableIndicator();
            Mission.sectionUITools.DisplayValidationButtons();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Mission.KillMission();
            Mission.RemoveInteractableIndicator();
            Mission.sectionUITools.DisplayValidationButtons();
        }
    }
}
