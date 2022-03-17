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
        Chest = 1 << 0,
        Utility = 1 << 1,
        Teleporter = 1 << 2,
        Shrine = 1 << 3,
        Special = 1 << 4,
        Player = 1 << 5,
        Drone = 1 << 6,
        Barrel = 1 << 7,
        Enemy = 1 << 8,
        EnemyVoid = 1 << 9,
        Ally = 1 << 10,
        Printer = 1 << 11,
        LunarPod = 1 << 12,
        Shop = 1 << 13,
        Equipment =  1 << 14,
        All = 0xFFFF
    }
}
