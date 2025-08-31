#pragma once

#include <unordered_map>
#include <fom-network/FOMPacket.h>
#include <raknet/BitStream.h>
#include "packet-serializers/PacketSerializers.h"

/**
 * Handles packet serialization and deserialization based on packet ID.
 */
class FOMPacketSerializer {
public:
	/**
	* Serializes a packet structure into a bitstream buffer.
	*
	* @param packet The packet to serialize.
	*/
	static RakNet::BitStream Serialize(const FOMPacket& packet) const;

	/**
	* Deserializes a bitstream buffer into a packet structure.
	*
	* @param bitstream The bitstream to deserialize.
	*/
	static FOMPacket Deserialize(RakNet::BitStream& bitstream) const;

private:
    FOMPacketSerializer();
	~FOMPacketSerializer();

	/**
	* Fetches the serializer for a given packet ID.
	*
	* @param id The packet ID to fetch the serializer for.
	*/
	static const IPacketIDSerializer* GetSerializer(PacketIdentifier id) const;

	/**
	* An array of packet serializers to use depending on the packet ID.
	*
	* @note We only create serializers for user-defined packets. 
	*/
	static const std::unordered_map<uint32_t, const IPacketIDSerializer*> packetSerializers;
};
