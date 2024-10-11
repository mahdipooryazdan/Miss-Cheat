using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace basicESP
{
    public class Entity
    {
        public Vector3 position { get; set; }
        public Vector3 viewoffset { get; set; }
        public Vector2 position2D { get; set; }
        public Vector2 viewPosition2D { get; set; }
        public int team { get; set; }
        public string name { get; set; }
        public int health { get; set; }
        public short weaponIndex { get; set; }
        public string currentWeaponName { get; set; }
    }

    public enum Weapon
    { 
    Deagle = 1,
    Elite = 2,
    Fiveseven= 3,
    Glock=4,
    Ak47 =7,
    Aug = 8,
    Awp = 9,
    Famas = 10,
    G3Sg1 = 11,
    M249 = 14,
    M4A4=16,
    Mac10 = 17,
    P90 = 19,
    Ump45=24,
    Xm1014=25,
    Bizon=26,
    Mag7=27,
    Negev=28,
    Swedoff=29,
    Tec9=30,
    Zeus=31,
    P200=32,
    Mp7=33,
    Mp9=34,
    Nova=35,
    P250 =36,
    Scar20=38,
    Sg556 =39,
    Ssg08=40,
    Knife=42,
    Flashbang=43,
    Hagrenade=44,
    SmokeGrenade=45,
    Molotov =46,
    Decoy=47,
    Incgrenade=48,
    C4=49,
    Usps=61,
    M4A1_Silencer=60,
    Cz75A =63,
    Revolver=64,
    knife=59


    }
}
