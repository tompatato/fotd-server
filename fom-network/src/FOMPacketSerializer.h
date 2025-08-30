#pragma once

#include <fom-network/FOMPacket.h>
#include <raknet/BitStream.h>
#include "packet-serializers/PacketSerializers.h"

/**
 * Handles packet serialization and deserialization based on packet ID.
 */
class FOMPacketSerializer {
public:
	FOMPacketSerializer();
	~FOMPacketSerializer();

	/**
	* Serializes a packet structure into a bitstream buffer.
	*
	* @param packet The packet to serialize.
	*/
	RakNet::BitStream Serialize(const FOMPacket& packet);

	/**
	* Deserializes a bitstream buffer into a packet structure.
	*
	* @param bitstream The bitstream to deserialize.
	*/
	FOMPacket Deserialize(RakNet::BitStream& bitstream);

private:

    /**
     * Gets the index of the serializer for a given packet ID.
     *
     * @param id The packet ID to get the serializer index for.
     */
    const uint32_t GetSerializerIndex(PacketIdentifier id) const {
        if (id > 255) {
            return -1;
        }
            
        const index = ID_USER_PACKET_ENUM - id;
        if (index < 0 || id >= 255) {
            return -1;
        }
        return index;
    }

	/**
	* Fetches the serializer for a given packet ID.
	*
	* @param id The packet ID to fetch the serializer for.
	*/
	const IPacketIDSerializer<FOMPacket>* GetSerializer(PacketIdentifier id) const {
        const index = this->GetSerializerIndex(id);
        if ( index == -1 ) {
            return NULL;
        }
        
        return serializers[index];
    }

	/**
	* An array of packet serializers to use depending on the packet ID.
	*
	* @note We only create serializers for user-defined packets. 
	*/
	const IPacketIDSerializer<FOMPacket>* serializers[256 - ID_USER_PACKET_ENUM];
};
