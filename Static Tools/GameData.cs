using UnityEngine;

namespace Data
{
    public static class GameData
    {
        public static Camp[] Camps = new Camp[1]
        {
            new Camp()
            {
                Sections = new Section[]
                {
                    new Section()
                    {
                        Nom = "Section1"
                    }
                }
            }
        };
        public static int NombreDeTours = 10;

    }

    public class Camp
    {
        public int Numéro;
        public string Nom;
        public Objectif[] Objectifs;
        public Section[] Sections;
        public Section SectionDeCommandement;
    }

    public class Section
    {
        public int Numéro;
        public string Nom;
        public Joueur Joueur;
        public Camp Camp;
        public int Moral;
        public GameObject gameObject;
        public Pion[] Pions;
        public SectionManager sectionManager;

        public void Create()
        {

        }

        public void SetControl(Joueur NouveauJoueur)
        {
            Joueur = NouveauJoueur;
        }

        public void Merge(Section Cible)
        {

        }
    }

    public class Pion
    {
        private int _ID = SetID();
        public TypeDePion Type;
        [Range(0, 100)]
        public int Santé;
        public string Nom;
        public Section Section;

        public int ID
        {
            get { return _ID; }
        }

        private static int SetID()
        {
            return 30;
        }
    }

    public class TypeDePion
    {

    }

    public struct Objectif
    {
        public int ID;
        public string Titre;
        public string Description;
        public byte CodeObjectif;
    }

    public struct Joueur
    {
        public int ID;
        public string Name;
    }

    public struct UnitInfo
    {
        public string Name;
        public string Type;
        public string Side;
        public string[] _infoString;
        public string State;
    }
}
