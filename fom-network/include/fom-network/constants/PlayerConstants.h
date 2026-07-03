#pragma once

#include <fom-network/InteropTypes.h>

namespace FOMNetwork {
namespace Constants {

constexpr uint32_t NUM_WEAPON_SLOTS = 3;
constexpr uint32_t NUM_UNKNOWN_ITEM_SLOTS = 6;
constexpr uint32_t NUM_QUICK_SLOTS = 4;

// Item-slot capacity reported for a container (the ItemList "capacity" field the
// client checks for free space, ItemList +0x14). Per-container capacity isn't
// modelled yet; a fixed generous value keeps the client from rejecting item
// moves with "not enough space" (a zero here means zero free slots).
constexpr uint32_t DEFAULT_ITEM_LIST_CAPACITY = 100;

}  // namespace Constants
}  // namespace FOMNetwork
