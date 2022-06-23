using Actions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Actions.ActionTools;

namespace Tools
{

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

