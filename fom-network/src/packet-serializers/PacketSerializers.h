#pragma once

#include <raknet/BitStream.h>
#include <fom-network/FOMPacket.h>

/**
 * Interface for packet serializers.
 */
class IPacketSerializer {
public:
	virtual ~IPacketSerializer() = default;

	// Always work with FOMPacket so all serializers can fit into one array
	virtual bool SerializePacket(RakNet::BitStream& bs, const FOMPacket& p) = 0;
	virtual FOMPacket DeserializePacket(RakNet::BitStream& bs) = 0;
};

/**
 * Template for typed packet serializers.
 */
template <typename T, typename Derived>
class PacketSerializer : public IPacketSerializer {
public:
	bool SerializePacket(RakNet::BitStream& bs, const FOMPacket& p) override {
		return static_cast<Derived*>(this)->Serialize(bs, packet.As<T>());
	}

	FOMPacket DeserializePacket(RakNet::BitStream& bs) override {
		T obj = static_cast<Derived*>(this)->Deserialize(bs);
		return FOMPacket(obj);
	}
};

/**
 * This macro makes it easy to define serializer classes without having to
 * write all of the same declaration boilerplate.
 */
#define DECLARE_SERIALIZER(PacketType)								\
class PacketType##Serializer :										\
	public PacketSerializer<PacketType, PacketType##Serializer> {	\
public:																\
	bool Serialize(RakNet::BitStream& bs, const PacketType& p);		\
	PacketType Deserialize(RakNet::BitStream& bs);					\
};

/**
 * Declare all of the serializers. Keep in mind that they must be:
 * <PacketTypeName>Serializer
 */
DECLARE_SERIALIZER(ExamplePacket)
