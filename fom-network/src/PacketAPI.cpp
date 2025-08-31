#include <vector>
#include <fom-network/PacketAPI.h>
#include "FOMPacketSerializer.h"

/**
 * The maximum number of packets that can be received each tick.
 * Since we buffer the packets to return to the consumer, we
 * don't want to stall the thread permanently when there are
 * more packets being received than pulled off the queue.
 */
#define MAX_BUFFERED_PACKETS 256;

ReceivedPackets FOMNetwork_ReceivePackets(RakPeerInterface* peer) {
	if (!peer) {
        return { NULL, 0 };
    }

    // Limit the number of packets 
    std::vector<const Packet*> receiveBuffer;
    receiveBuffer.reserve(MAX_BUFFERED_PACKETS)
    while (receiveBuffer.size() < MAX_BUFFERED_PACKETS) {
        const Packet* packet = peer->Receive();
        if (!packet) {
            break;
        }

        receiveBuffer.push(packet);
    }

    ReceivedPackets received;

    // We're going to pass an array of packets back to the consumer.
    // The array and the packets contained must all be deallocated
    // in FOMNetwork_ProcessPackets so that we don't leak memory!
    received.count = receiveBuffer.size();
    received.packets = new Packet*[receiveBuffer.size()];
    std::copy(receiveBuffer.begin(), receiveBuffer.end(), received.packets);

    return received;
}

int8_t FOMNetwork_ProcessPackets(RakPeerInterface* peer, ReceivedPackets received, const FOMPacket* packetBuffer, uint32_t packetBufferLen) {
    if (!peer || !received.packets || received.count == 0) {
        return 0;
    }

	if (!packetBuffer || packetBufferLen != received.count) {
        return -1;
    }

    for (uint32_t i = 0; i < received.count; i++) {
        const Packet* p = received.packets[i];
        if (!p) {
            continue;
        }

        RakNet::BitStream bs(p->data, p->length, false);
        packetBuffer[i] = FOMPacketSerializer::Deserialize(bs);

        // Release the packet back to RakNet.
        peer->DeallocatePacket(const_cast<Packet*>(p));
    }

    // Now that we're done we don't need the receive packet buffer anymore.
    delete[] received.packets;

    return 0;
}

void FOMNetwork_Send(RakPeerInterface* peer, const SendPacket* packets, uint32_t count) {
	if (!peer || !packets || count == 0) {
        return;
    }

    for (uint32_t i = 0; i < count; i++) {
        const SendPacket& s = packets[i];
        RakNet::BitStream bs = FOMPacketSerializer::Serialize(s.data);
        if (s.broadcast) {
            peer->Send(&bs, s.priority, s.reliability, s.orderingChannel, UNASSIGNED_SYSTEM_ADDRESS, true);
        } else {
            peer->Send(&bs, s.priority, s.reliability, s.orderingChannel, s.address.systemAddress, false);
        }
    }
}
