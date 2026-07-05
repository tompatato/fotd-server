namespace FOMServer.Shared.Core.Enums
{
    /// <summary>
    /// Sub-type discriminator carried by <see cref="PacketIdentifier.ID_VORTEX_GATE"/>.
    /// The vortex terminal multiplexes several operations onto one packet id; only
    /// the world-travel request/approve pair is handled server-side today.
    /// </summary>
    /// <remarks>
    /// Other observed values: 2 = item list, 3 = message-box confirm variant,
    /// 5 = destination-list request (client to master),
    /// 6 = destination-list data (master to client).
    /// </remarks>
    public enum VortexGateType : byte
    {
        Invalid = 0, // VORTEX_GATE_TYPE_INVALID
        TravelApprove = 4, // VORTEX_GATE_TYPE_TRAVEL_APPROVE
        TravelRequest = 7, // VORTEX_GATE_TYPE_TRAVEL_REQUEST
    }
}
