cmake_minimum_required(VERSION 3.16)
project(CGAssignment2 LANGUAGES CXX)

find_package(SDL2 REQUIRED CONFIG)

add_library(assignment2_png STATIC src/savePng.cpp)
target_include_directories(assignment2_png PUBLIC include PRIVATE ../0000/deps)
target_compile_features(assignment2_png PRIVATE cxx_std_11)

add_executable(CGAssignment2
    src/main.cpp
    src/TRDrawableMesh.cpp
    src/TRFrameBuffer.cpp
    src/TRRenderer.cpp
    src/TRShaderPipeline.cpp
    src/TRWindowsApp.cpp
)
target_include_directories(CGAssignment2 PRIVATE include/TinyRenderer ../0001/include)
target_compile_features(CGAssignment2 PRIVATE cxx_std_11)
if(TARGET SDL2::SDL2main)
    target_link_libraries(CGAssignment2 PRIVATE SDL2::SDL2main)
endif()
target_link_libraries(CGAssignment2 PRIVATE SDL2::SDL2 assignment2_png)
target_compile_definitions(CGAssignment2 PRIVATE MODEL_DIR="${CMAKE_CURRENT_SOURCE_DIR}/model")

add_executable(CGAssignment2-Improve
    improve/main.cpp
    improve/rasterizer.cpp
    improve/Triangle.cpp
)
target_include_directories(CGAssignment2-Improve PRIVATE ../0001/include)
target_compile_features(CGAssignment2-Improve PRIVATE cxx_std_17)
target_link_libraries(CGAssignment2-Improve PRIVATE assignment2_png)
