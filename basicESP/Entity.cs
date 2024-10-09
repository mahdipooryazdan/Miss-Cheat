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
        public  Vector2 position2D { get; set; }
        public  Vector2 viewPosition2D { get; set; }
        public int team { get; set; }
        public string  name { get; set; }
        public int health { get; set; }

    }
}
