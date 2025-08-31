#include <unordered_map>
#include "FOMPacketSerializer.h"

FOMPacketSerializer::FOMPacketSerializer() {
    FOMPacketSerializer::FOMPacketSerializer() {
    auto registerSerializer = [this](PacketIdentifier id, IPacketIDSerializer* s) {
        this->packetSerializers[id] = s;
    };

    registerSerializer(ID_USER_PACKET_ENUM, new ExamplePacketSerializer());
}

FOMPacketSerializer::~FOMPacketSerializer() {
    for (auto& entry : this->packetSerializers) {
        delete entry.second;
    }
}

RakNet::BitStream FOMPacketSerializer::Serialize(const FOMPacket& p) const {
    const auto* serializer = GetSerializer(p.ID);
    if (!serializer) {
        return RakNet::BitStream{};
    }

    RakNet::BitStream bs;

    // The first byte in the BitStream will always be the packet ID.
    bs.Write(p.ID); 

    return serializer->Serialize(bs, p);
}

FOMPacket FOMPacketSerializer::Deserialize(RakNet::BitStream& bs) const {
    // The first byte in the BitStream will always be the packet ID.
    PacketIdentifier id;
    if (!bs.Read(id)) {
        return INVALID_PACKET;
    }

    const auto* serializer = GetSerializer(id);
    if (!serializer) {
        return INVALID_PACKET;
    }

    return serializer->Deserialize(bs);
}

const IPacketSerializer* FOMPacketSerializer::GetSerializer(PacketIdentifier id) const {
    auto it = this->packetSerializers.find(id);
    if (it == this->packetSerializers.end()) {
        return NULL;
    }
    return it->second;
}
