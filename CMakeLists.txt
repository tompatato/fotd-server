cmake_minimum_required(VERSION 3.28)

project(FOMServer)

include(CTest)
enable_testing()

add_subdirectory(extern/raknet)
add_subdirectory(fom-network)
