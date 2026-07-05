using System.Runtime.InteropServices;

namespace FOMServer.Shared.Core.Packets.Types
{
    /// <summary>
    /// One placed world object (the 28-byte client record decoded by
    /// Object.lto FUN_100dc250). Wire order is Id, Type, State, Extra, Position.
    /// <see cref="Type"/> is an ItemType whose definition supplies the model.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldObject
    {
        public uint Id;
        public ushort Type;
        public byte State;
        public uint Extra;
        public PositionRotation Position;
    }
}
