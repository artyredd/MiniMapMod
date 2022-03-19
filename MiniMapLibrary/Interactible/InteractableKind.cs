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
        Shrine = 1 << 2,
        Special = 1 << 3,
        Drone = 1 << 4,
        Barrel = 1 << 5,
        Printer = 1 << 6,
        LunarPod = 1 << 7,
        Shop = 1 << 8,
        Equipment =  1 << 9,
        Teleporter = 1 << 10,
        EnemyMonster = 1 << 11,
        EnemyLunar = 1 << 12,
        EnemyVoid = 1 << 13,
        Minion = 1 << 14,
        Player = 1 << 15,
        Item =  1 << 16,
        Portal = 1 << 17,
        Totem = 1 << 18,
        Neutral = 1 << 19,
        All = int.MaxValue
    }
}
