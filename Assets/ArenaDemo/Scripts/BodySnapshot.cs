using System;
using System.Collections.Generic;
namespace PhaseArena
{
    // Separate leases preserve nested explosion/steam-wave iteration during registration changes.
    internal readonly struct BodySnapshot : IDisposable
    {
        static readonly Stack<List<ThermoBody>> pool=new Stack<List<ThermoBody>>();
        public readonly List<ThermoBody> Bodies;
        BodySnapshot(List<ThermoBody> bodies) { Bodies=bodies; }
        public static BodySnapshot Rent(List<ThermoBody> source)
        {
            var list=pool.Count>0 ? pool.Pop() : new List<ThermoBody>(source.Count);
            list.AddRange(source); return new BodySnapshot(list);
        }
        public void Dispose() { Bodies.Clear(); if(pool.Count<16) pool.Push(Bodies); }
    }
}
