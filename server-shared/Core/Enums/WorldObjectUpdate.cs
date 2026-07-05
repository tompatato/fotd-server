namespace FOMServer.Shared.Core.Enums
{
    /// <summary>
    /// Sub-type discriminator carried by <see cref="PacketIdentifier.ID_WORLD_OBJECTS"/>.
    /// The packet multiplexes several object operations onto one id; the server
    /// only emits <see cref="Snapshot"/> / <see cref="Category"/> today.
    /// </summary>
    /// <remarks>
    /// Other observed values: 3 = set a state flag on a live object,
    /// 4 = per-object detail update.
    /// </remarks>
    public enum WorldObjectUpdate : byte
    {
        Invalid = 0, // WORLD_OBJECT_UPDATE_INVALID
        Snapshot = 1, // WORLD_OBJECT_UPDATE_SNAPSHOT
        Category = 2, // WORLD_OBJECT_UPDATE_CATEGORY
        State = 3, // WORLD_OBJECT_UPDATE_STATE
        Detail = 4, // WORLD_OBJECT_UPDATE_DETAIL
    }
}
