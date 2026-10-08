cmake_minimum_required(VERSION 3.20)
project(assignment0_opengl CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Task 5 要求窗口标题为学号：构建时用 cmake -DSTUDENT_ID=19214044 .. 覆盖
if(NOT STUDENT_ID)
    set(STUDENT_ID "YOUR_STUDENT_ID")
endif()
add_compile_definitions(STUDENT_ID="${STUDENT_ID}")

# GLFW / GLM 通过 FetchContent 从 GitHub 拉取源码构建（首次配置需联网，约 1-2 分钟）
include(FetchContent)

set(GLFW_BUILD_DOCS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(GLFW_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(GLFW_INSTALL OFF CACHE BOOL "" FORCE)
FetchContent_Declare(glfw
    GIT_REPOSITORY https://github.com/glfw/glfw.git
    GIT_TAG 3.4
    GIT_SHALLOW TRUE)
FetchContent_MakeAvailable(glfw)

FetchContent_Declare(glm
    GIT_REPOSITORY https://github.com/g-truc/glm.git
    GIT_TAG 1.0.1
    GIT_SHALLOW TRUE)
FetchContent_MakeAvailable(glm)

# 公共库：GLAD 加载器 + stb 图像读写
add_library(common STATIC
    deps/glad/src/gl.c
    src/common/stb_impl.cpp)
target_include_directories(common PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/deps/glad/include
    ${CMAKE_CURRENT_SOURCE_DIR}/deps
    ${CMAKE_CURRENT_SOURCE_DIR}/src/common)
target_link_libraries(common PUBLIC glfw)

function(add_chapter name)
    add_executable(${name} src/${name}/main.cpp)
    target_link_libraries(${name} PRIVATE common glm::glm)
endfunction()

# 按教程顺序，一个章节一个可执行程序
add_chapter(01_hello_window)
add_chapter(02_hello_triangle)
add_chapter(03_shaders)
add_chapter(04_textures)
add_chapter(05_transformations)
add_chapter(06_coordinate_systems)
add_chapter(07_camera)
