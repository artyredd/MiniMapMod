using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMapLibrary
{
    public class Interactable
    {
        public string Name { get; set; }

        public bool Enabled { get; set; } = true;

        public InteractableKind InteractableType { get; set; }

        public Interactable(string name, InteractableKind type)
        {
            this.Name = name;
            this.InteractableType = type;
        }
    }
}
