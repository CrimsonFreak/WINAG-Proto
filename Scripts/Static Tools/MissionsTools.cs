
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

[Serializable]
public static class MissionsData
{
    private static string scontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Surveiller.json", Encoding.UTF8);
    private static string apcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Appuyer.json", Encoding.UTF8);
    private static string atcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Attaquer.json", Encoding.UTF8);
    private static string rcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Reconna�tre.json", Encoding.UTF8);

    public static MissionModifiers surveiller = JsonUtility.FromJson<MissionModifiers>(scontent);
    public static MissionModifiers appuyer = JsonUtility.FromJson<MissionModifiers>(apcontent);
    public static MissionModifiers attaquer = JsonUtility.FromJson<MissionModifiers>(atcontent);
    public static MissionModifiers reconna�tre = JsonUtility.FromJson<MissionModifiers>(rcontent);
}

public static class MissionsTools
{
    public enum MissionTypes
    {
        Reconna�tre,
        Appuyer,
        Attaquer,
        Surveiller,
        None
    }

    public static MissionTypes MissionNameToEnum(string Name) 
    {
        switch (Name)
        {
            case "Reconna�tre": return MissionTypes.Reconna�tre;
            case "Appuyer": return MissionTypes.Appuyer;
            case "Attaquer": return MissionTypes.Attaquer;
            case "Surveiller": return MissionTypes.Surveiller;
            default: throw new Exception("Erreur lors de la d�finition du type de Mission");
        }
    }
}


public class MissionModifiers
{   
    public MissionModifiers(MissionsTools.MissionTypes Type)
    {
        switch (Type)
        {
            case MissionsTools.MissionTypes.Attaquer: 
                Observation = MissionsData.attaquer.Observation;
                break;

            case MissionsTools.MissionTypes.Appuyer:
                Observation = MissionsData.appuyer.Observation;
                break;

            case MissionsTools.MissionTypes.Surveiller:
                Observation = MissionsData.surveiller.Observation;
                break;

            case MissionsTools.MissionTypes.Reconna�tre:
                Observation = MissionsData.reconna�tre.Observation;
                break;

            default:
                Observation = 0;
                break; 
        }
    }

    public MissionModifiers() { }


    public double Observation;

}
