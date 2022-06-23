using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Tools;

namespace Missions
{

    public static class MissionsData
    {
        private static string scontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Surveiller.json", Encoding.UTF8);
        private static string apcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Appuyer.json", Encoding.UTF8);
        private static string atcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Attaquer.json", Encoding.UTF8);
        private static string rcontent = File.ReadAllText("./Equilibrage/ModificateursDeMission/Reconnaître.json", Encoding.UTF8);

        public static MissionModifiers surveiller = JsonUtility.FromJson<MissionModifiers>(scontent);
        public static MissionModifiers appuyer = JsonUtility.FromJson<MissionModifiers>(apcontent);
        public static MissionModifiers attaquer = JsonUtility.FromJson<MissionModifiers>(atcontent);
        public static MissionModifiers reconnaître = JsonUtility.FromJson<MissionModifiers>(rcontent);
    }

    public static class MissionsTools
    {
        public enum MissionTypes
        {
            Reconnaître,
            Appuyer,
            Attaquer,
            Surveiller,
            None
        }

        public static MissionTypes MissionNameToEnum(string Name)
        {
            switch (Name)
            {
                case "Reconnaître": return MissionTypes.Reconnaître;
                case "Appuyer": return MissionTypes.Appuyer;
                case "Attaquer": return MissionTypes.Attaquer;
                case "Surveiller": return MissionTypes.Surveiller;
                default: throw new Exception("Erreur lors de la définition du type de Mission");
            }
        }

        public static void AssignListenerToMissionButtons(SectionManager sectionManager)
        {
            Button[] ButtonList;
            var Triangle = sectionManager.gameManager.Triangle;
            ButtonList = Triangle.GetComponentsInChildren<Button>(true);
            foreach (Button button in ButtonList)
            {
                button.onClick.AddListener(() => CreateAction(sectionManager, 3, button.GetComponentInChildren<Text>().text));
            }

        }
        public static void CreateAction(SectionManager sectionManager, int TurnSpan, string ActionName)
        {
            SectionUITools sectionUITools = sectionManager.sectionUITools;
            if (!sectionManager.Selected) return;
            else if (TurnSpan + sectionUITools.LastTurnWithAction > sectionUITools.TotalTurns)
            {
                TurnSpan = sectionUITools.TotalTurns - sectionUITools.LastTurnWithAction;
                if (TurnSpan == 0) return;
            };

            if (sectionManager.MissionsList.Count != 0 && sectionManager.Performing)
            {
                sectionManager.MissionsList[sectionManager.MissionsList.Count - 1].KillMission();
                sectionManager.MissionsList.RemoveAt(sectionManager.MissionsList.Count - 1);
            }

            var Panel = sectionUITools.timelineAsset.transform.Find("Panel");
            sectionUITools.CellWidth = Mathf.Abs(Panel.GetComponent<RectTransform>().rect.width / sectionUITools.TotalTurns);

            var _start = sectionUITools.LastTurnWithAction >= sectionManager.gameManager.CurrentTurn - 1
                   ? sectionUITools.LastTurnWithAction
                   : sectionManager.gameManager.CurrentTurn - 1;

            var _mission = new GameObject().AddComponent<Mission>();
            _mission.Number = sectionManager.MissionsList.Count;
            _mission.sectionManager = sectionManager;
            _mission.sectionUITools = sectionUITools;
            _mission.StartingTurn = _start;
            _mission.EndingTurn = _start + TurnSpan;
            _mission.name = ActionName;
            _mission.TimelineObject = GameObject.Instantiate(sectionManager.OverlayPrefab);
            _mission.SetStartingAttributes(Panel);


            if (sectionManager.MissionsList.Count == 0) sectionManager.CurrentMission = _mission;

            sectionManager.MissionsList.Add(_mission);
            sectionUITools.HideValidationButtons();
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

                case MissionsTools.MissionTypes.Reconnaître:
                    Observation = MissionsData.reconnaître.Observation;
                    break;

                default:
                    Observation = 0;
                    break;
            }
        }

        public MissionModifiers() { }


        public double Observation;

    }
}
