using Actions;
using Data;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class UnitController : MonoBehaviour
{
    public Pion DATA;

    //Raycast variables
    public LayerMask RayCastLayer;

    //GameObject variables
    public PlayerUnit MainUnitObject;
    public Vector3 ReferenceObjectPosition;
    public NavMeshAgent Agent;
    public bool IsOwned; //replace with compare to DATA.player when DATA.player is ready

    #region MainUnityProcess
    //Start is called before the first frame, after the GameObject is created
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        MainUnitObject = GetComponent<PlayerUnit>();
        MainUnitObject.IsOwned = IsOwned = DATA.Section.sectionManager.IsOwned;
        MainUnitObject.ReferenceObjectPosition = ReferenceObjectPosition = GenericTools.CalculateTriangleCentroid(
            gameObject.transform.position,
            gameObject.transform.Find("Multi1").position,
            gameObject.transform.Find("Multi2").position
            );
        name = DATA.Nom;
        MainUnitObject.CanEmbark = true;
        MainUnitObject.numberInOrder = int.Parse(name.Replace("Groupe", ""));
        MainUnitObject.SectionManager = DATA.Section.sectionManager;
    }

    //Update is called on each frame. Some of it's content might move to Early or Late Update as I go on.
    void Update()
    {

        if (!IsOwned) return;
        UnitAction lastAction = MainUnitObject.ActionArray.Length == 0
            ? null
            : MainUnitObject.ActionArray[0];

        if (lastAction == null) return;

        if (Agent.hasPath && Agent.remainingDistance <= 0.01f)
        {
            if (lastAction.eventOnEnd) OnEmbarkment(lastAction);
            ReferenceObjectPosition = GenericTools.CalculateTriangleCentroid(
                gameObject.transform.position,
                gameObject.transform.Find("Multi1").position,
                gameObject.transform.Find("Multi2").position
            );
        }
    }

    #endregion

    void OnEmbarkment(UnitAction action)
    {
        Embark EmbarkingAction = action.elements as Embark;
        TransportManager vehicle = EmbarkingAction.Parameters.Vehicle;
        if (vehicle.Embark(this))
        {
            transform.SetParent(vehicle.transform.Find("UnitsInside"));
            gameObject.SetActive(false);
        };
    }
}

