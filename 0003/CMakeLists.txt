cmake_minimum_required(VERSION 3.16)
project(CGAssignment3 LANGUAGES CXX)

find_package(SDL2 REQUIRED CONFIG)
find_package(Threads REQUIRED)

add_executable(CGAssignment3
    src/main.cpp
    src/WindowsApp.cpp
    include/RayTracer/WindowsApp.h
    include/RayTracer/vec3.h
)
target_compile_features(CGAssignment3 PRIVATE cxx_std_11)
target_include_directories(CGAssignment3 PRIVATE include/RayTracer)
if(TARGET SDL2::SDL2main)
    target_link_libraries(CGAssignment3 PRIVATE SDL2::SDL2main)
endif()
target_link_libraries(CGAssignment3 PRIVATE SDL2::SDL2 Threads::Threads)
