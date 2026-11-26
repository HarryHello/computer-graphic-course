cmake_minimum_required(VERSION 3.16)
project(CGAssignment3 LANGUAGES CXX)

find_package(SDL2 REQUIRED CONFIG)
find_package(Threads REQUIRED)

add_executable(CGAssignment3
    src/main.cpp
    src/WindowsApp.cpp
    src/Renderer.cpp
    src/Scenes.cpp
    src/Texture.cpp
    src/Image.cpp
    include/RayTracer/WindowsApp.h
    include/RayTracer/vec3.h
)
target_compile_features(CGAssignment3 PRIVATE cxx_std_17)
target_include_directories(CGAssignment3 PRIVATE include/RayTracer)
target_include_directories(CGAssignment3 SYSTEM PRIVATE ../0000/deps)
target_compile_definitions(CGAssignment3 PRIVATE ASSET_DIR="${CMAKE_CURRENT_SOURCE_DIR}/assets")
if(TARGET SDL2::SDL2main)
    target_link_libraries(CGAssignment3 PRIVATE SDL2::SDL2main)
endif()
target_link_libraries(CGAssignment3 PRIVATE SDL2::SDL2 Threads::Threads)
