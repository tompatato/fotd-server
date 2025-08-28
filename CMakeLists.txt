cmake_minimum_required(VERSION 3.28)

project(FOMServer)

add_subdirectory(extern/raknet)
add_subdirectory(fom-network)

# Testing Setup
include(CTest)
enable_testing()

add_subdirectory(fom-network/tests)
