using System.Runtime.InteropServices;
using FOMServer.Shared.Core.Enums;
using FOMServer.Shared.Metadata;

namespace FOMServer.Shared.Core.Packets
{
    /// <summary>
    /// Sent by the client to the master when leaving a world — either logging
    /// out to character-select (<see cref="IsChangingWorlds"/> false) or as a
    /// prelude to switching worlds (true). Also forwarded master → world to
    /// instruct the world server to end the player's session.
    /// </summary>
    [PacketId(PacketIdentifier.ID_WORLD_LOGOUT)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WorldLogout
    {
        public uint PlayerId;
        public byte IsChangingWorldsRaw;

        public bool IsChangingWorlds
        {
            readonly get => IsChangingWorldsRaw != 0;
            set => IsChangingWorldsRaw = (byte)(value ? 1 : 0);
        }
    }
}
