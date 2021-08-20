using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMapLibrary
{
    public enum InteractableKind
    {
        none = 0,
        Primary = 1 << 0,
        Chest = 1 << 1,
        Utility = 1 << 2,
        Teleporter = 1 << 3,
        Shrine = 1 << 4,
        Special = 1 << 5,
        Player = 1 << 6,
        Drone = 1 << 7,
        Barrel = 1 << 8,
        Enemy = 1 << 9,
        All = 0b_1111_1
    }
}
