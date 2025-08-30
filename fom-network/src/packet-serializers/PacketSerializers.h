#pragma once

#include <raknet/BitStream.h>
#include <fom-network/FOMPacket.h>

/**
 * Interface for packet ID serializers.
 *
 * Each packet ID has its own serializer class that handles
 * serialization and deserialization of that specific type
 * into an associated packet structure.
 */
template <typename T>
class IPacketSerializer {
public:
	/// Serialize a packet into a BitStream.
	virtual RakNet::BitStream Serialize(const T& packet) = 0;

	/// Deserialize a packet from a BitStream.
	virtual T Deserialize(RakNet::BitStream& bitstream) = 0;
};

class ExamplePacketSerializer : public IPacketSerializer<ExamplePacket> {};
