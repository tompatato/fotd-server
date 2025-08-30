#include <unordered_map>
#include "FOMPacketSerializer.h"

void FOMPacketSerializer::FOMPacketSerializer() {
    auto registerSerializer = [this](PacketID id, IPacketIDSerializer<FOMPacket>* s) {
        this->packetSerializers[this->GetSerializerIndex(id)] = s;
    };

    registerSerializer(ID_USER_PACKET_ENUM, new ExamplePacketSerializer());
}

void FOMPacketSerializer::~FOMPacketSerializer() {
    for (auto& serializer : this->packetSerializers) {
        delete serializer;
    }
}

RakNet::BitStream FOMPacketSerializer::Serialize(const FOMPacket& packet) {
    const IPacketIDSerializer<FOMPacket>* serializer = GetSerializer(packet.ID);
    if (!serializer) {
        return RakNet::BitStream{};
    }

    return serializer->Serialize(packet);
}

FOMPacket FOMPacketSerializer::Deserialize(RakNet::BitStream& bs) {
    const IPacketIDSerializer<FOMPacket>* serializer = GetSerializer(packet.ID);
    if (!serializer) {
        return INVALID_PACKET;
    }

    return serializer->Deserialize(bs);
}
