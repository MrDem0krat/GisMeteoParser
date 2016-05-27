using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gismeteo
{
    namespace City
    {
        public class City
        {
            public int ID { get; set; }
            public string Name { get; set; }

            public City() { }
            public City(int id, string name)
            {
                ID = id;
                Name = name;
            }

            public override string ToString()
            {
                return "{ ID = " + ID + "; Name = " + Name + " }";
            }
            public override bool Equals(object obj)
            {
                return (obj as City).ID == (this as City).ID;
            }
            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }
            public static bool operator ==(City left, City right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(City left, City right)
            {
                return !left.Equals(right);
            }
        } 
    }
}
