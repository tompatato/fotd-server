# GoogleTest requires at least C++11
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)
FetchContent_Declare(
  googletest
  URL https://github.com/google/googletest/archive/6a7ed316a5cdc07b6d26362c90770787513822d4.zip
)
# For Windows: Prevent overriding the parent project's compiler/linker settings
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(googletest)

# Disable Microsoft Code Analysis for GoogleTest targets
if (MSVC)
	set_target_properties(gtest PROPERTIES VS_GLOBAL_EnableMicrosoftCodeAnalysis false)
	set_target_properties(gtest_main PROPERTIES VS_GLOBAL_EnableMicrosoftCodeAnalysis false)
endif()

add_executable(FOMNetworkTests
	"test_main.cpp"
	"packet-serializers/ExamplePacketSerializer_test.cpp"
)

target_include_directories(FOMNetworkTests PRIVATE
	${PROJECT_SOURCE_DIR}/include
	${PROJECT_SOURCE_DIR}/src
)

target_link_libraries(FOMNetworkTests
	PRIVATE
	FOMNetwork
	RakNet
	gtest_main
)

include(GoogleTest)
gtest_discover_tests(FOMNetworkTests
  EXTRA_ARGS --gtest_color=yes
)
