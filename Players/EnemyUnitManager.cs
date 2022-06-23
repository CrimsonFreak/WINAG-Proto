using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data;

public class EnemyUnitManager : MonoBehaviour
{
    public GameObject iconPrefab;
    public bool IsInComsRange = true;
    public int CombatValue = 8;
    public int Indiscrétion = 20;
    public int InfoLevel = 0;
    public string Name = "Unité ennemie";
    public string Type = "Infanterie";
    public string[] _info = new string[]
    {
        "AK-47",
        "C4"
    };
    public string State = "Complet";


    public UnitInfo GetInfo()
    {
        UnitInfo output = InfoLevel switch
        {
            1 => new UnitInfo()
            {
                Name = Name,
                Type = "???",
                Side = "???",
                _infoString = new string[] { "???" },
                State = "???"
            },
            2 => new UnitInfo()
            {
                Name = Name,
                Type = Type,
                Side = "Enemy",
                _infoString = new string[] { "???" },
                State = "???"
            },
            3 => new UnitInfo()
            {
                Name = Name,
                Type = Type,
                Side = "Enemy",
                _infoString = _info,
                State = "???"
            },
            4 => new UnitInfo()
            {
                Name = Name,
                Type = Type,
                Side = "Enemy",
                _infoString = _info,
                State = State,
            },
            _ => new UnitInfo()
            {
                Name = "???",
                Type = "???",
                Side = "???",
                _infoString = new string[] { "???" },
                State = "???"
            },
        };

        return output;
    }
}
