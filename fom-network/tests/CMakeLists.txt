cmake_minimum_required(VERSION 3.28)

project(FOMNetworkTests)

# GoogleTest requires at least C++17
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)
FetchContent_Declare(
  googletest
  URL https://github.com/google/googletest/archive/52eb8108c5bdec04579160ae17225d66034bd723.zip
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
)

target_link_libraries(FOMNetworkTests
	FOMNetwork
	GTest::gtest_main
)

include(GoogleTest)
gtest_discover_tests(FOMNetworkTests)
