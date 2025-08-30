#include <raknet/BitStream.h>
#include <fom-network/FOMPacket.h>
#include "PacketSerializers.h"

class ExamplePacketSerializer : public IPacketIDSerializer<ExamplePacket> {
public:
	RakNet::BitStream Serialize(const ExamplePacket& packet) override {
		RakNet::BitStream bitstream;
		bitstream.Write(packet.exampleField1);
		return bitstream;
	}

	ExamplePacket Deserialize(RakNet::BitStream& bitstream) override {
		ExamplePacket packet;
		bitstream.Read(packet.exampleField1);
		return packet;
	}
};
