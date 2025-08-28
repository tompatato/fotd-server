cmake_minimum_required(VERSION 3.28)

project(FOMNetwork)

add_library(FOMNetwork SHARED
	src/PacketServer.cpp
)

target_include_directories(FOMNetwork
	PUBLIC
	${PROJECT_SOURCE_DIR}/include
)

target_link_libraries(FOMNetwork
	PRIVATE
	RakNet
)

# Windows-specific
if(WIN32)
	set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
	target_link_libraries(FOMNetwork
		PRIVATE
		wsock32 ws2_32
	)
endif()

install(TARGETS FOMNetwork
	EXPORT FOMNetworkTargets
	LIBRARY DESTINATION lib
	RUNTIME DESTINATION lib
	INCLUDES DESTINATION include
)
install(DIRECTORY include/ DESTINATION include)
