using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tools;
using Actions;

public class TransportManager : MonoBehaviour
{
    public UnitController[] Seats = new UnitController[4];
    public Transform SeatsIndicator;
    public GameObject ActionIndicator;
    public PlayerUnit PlayerUnit;

    private void Start()
    {
        ActionIndicator.SetActive(false);
        PlayerUnit = GetComponent<PlayerUnit>();
        name = "Transport1";
        PlayerUnit.numberInOrder = 1;
        StartCoroutine(LateStart()); 
    }

    private IEnumerator<WaitForSeconds> LateStart()
    {
        yield return new WaitForSeconds(0.5f);
        PlayerUnit.IsOwned = true;
        PlayerUnit.ReferenceObjectPosition = transform.position;
        PlayerUnit.SectionManager = GameObject.Find("Section1").GetComponent<SectionManager>();
    }

    protected void Update()
    {
        //SeatsIndicator.LookAt(SeatsIndicator.position + GameObject.Find("Camera").transform.forward);
        SeatsIndicator.parent.LookAt(SeatsIndicator.parent.position + GameObject.Find("Camera").transform.forward);
    }

    public bool Embark(UnitController unit)
    {
        int actions = 0;
        for (int i = 0; i < Seats.Length; i++)
        {
            if (Seats[i] == null)
            {
                if (unit.transform.Find("Multi1"))
                {
                    try 
                    {
                        Seats[i] = Seats[i + 1] = Seats[i + 2] = unit;
                        actions += 3;
                    }
                    catch
                    {
                        return false;
                    }
                    SeatsIndicator.GetChild(i).GetComponent<Image>().color = Palette.playerUnit;
                    SeatsIndicator.GetChild(i+1).GetComponent<Image>().color = Palette.playerUnit;
                    SeatsIndicator.GetChild(i+2).GetComponent<Image>().color = Palette.playerUnit;
                }
                else
                {
                    Seats[i] = unit;
                    SeatsIndicator.GetChild(i).GetComponent<Image>().color = Palette.playerUnit;
                    actions ++;
                }
                return true;
            }
            else continue;
        }

        return false;
    }
}
