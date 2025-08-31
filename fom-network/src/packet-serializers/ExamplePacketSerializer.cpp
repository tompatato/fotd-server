#include <raknet/BitStream.h>
#include <fom-network/FOMPacket.h>
#include "PacketSerializers.h"

bool ExamplePacketSerializer::Serialize(RakNet::BitStream& bs, const ExamplePacket& p) override {
	bs.Write(p.exampleField1);
	return bs;
}

ExamplePacket ExamplePacketSerializer::Deserialize(RakNet::BitStream& bs) override {
	ExamplePacket p;
	bs.Read(p.exampleField1);
	return p;
}

