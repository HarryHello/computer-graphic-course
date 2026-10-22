############################################################
# Computer Graphics Assignment Configuration
# Tested on Windows 10 and Ubuntu 16.04.1
# By Wencong Yang, yangwc3@mail2.sysu.edu.cn
############################################################
cmake_minimum_required(VERSION 3.5)

# You'd better build it in release mode for performance
set(CMAKE_BUILD_TYPE "Release" CACHE STRING "" FORCE)

project(CGAssignment1 VERSION "1.0")

# C++ 11 is required
set(CMAKE_CXX_STANDARD 11)

############################################################
# Windows / Linux / macOS options
############################################################
IF (CMAKE_SYSTEM_NAME MATCHES "Windows")
	link_directories(${PROJECT_SOURCE_DIR}/libs)
	include_directories(${PROJECT_SOURCE_DIR}/include)
ELSEIF (CMAKE_SYSTEM_NAME MATCHES "Linux")
	find_package(SDL2 REQUIRED)

	# check if SDL2 was found
	if(SDL2_FOUND)
	    message ("SDL2 found")
	else()
	    message (FATAL_ERROR "Cannot find SDL2")
	endif()
	include_directories(${PROJECT_SOURCE_DIR}/include)
ELSEIF (CMAKE_SYSTEM_NAME MATCHES "Darwin")
	# macOS: SDL2 via Homebrew (cmake -DCMAKE_PREFIX_PATH=/opt/homebrew ..)
	find_package(SDL2 REQUIRED)

	if(SDL2_FOUND)
	    message ("SDL2 found")
	else()
	    message (FATAL_ERROR "Cannot find SDL2")
	endif()

	# The bundled include/SDL2 holds old Windows headers that do not compile with recent macOS toolchains;
	# put the Homebrew SDL2 include dir first in the search order, while glm / tiny_obj_loader still come from include/
	get_target_property(SDL2_INCLUDE_DIRS SDL2::SDL2 INTERFACE_INCLUDE_DIRECTORIES)
	set_property(DIRECTORY PROPERTY INCLUDE_DIRECTORIES
		${SDL2_INCLUDE_DIRS}
		${PROJECT_SOURCE_DIR}/include)
ENDIF()

############################################################

############################################################
# If SDL2 was not found, please install SDL2 according to
# the following terminal commands:
# sudo apt-get update
# sudo apt-get install libsdl2-2.0
# sudo apt-get install libsdl2-dev
# sdl2-config --version
###########################################################


############################################################
# Create an executable
############################################################

# Project headers in include/TinyRenderer/, implementations in src/
file(GLOB PROJECT_HEADERS ./include/TinyRenderer/*.h)
source_group("Header Files" FILES ${PROJECT_HEADERS})
aux_source_directory(./src/ DIR_SRCS)

# Add an executable with the above sources
add_executable(${PROJECT_NAME} ${DIR_SRCS} ${PROJECT_HEADERS})

# Link SDL2. On macOS the imported targets' headers must be searched via -I (not -isystem)
# to take precedence over the bundled old headers
IF (CMAKE_SYSTEM_NAME MATCHES "Darwin")
	set_target_properties(${PROJECT_NAME} PROPERTIES NO_SYSTEM_FROM_IMPORTED TRUE)
	target_link_libraries( ${PROJECT_NAME}
	    PRIVATE
	        SDL2::SDL2
		SDL2::SDL2main
	)
ELSE()
	target_link_libraries( ${PROJECT_NAME}
	    PRIVATE
	        SDL2
		SDL2main
	)
ENDIF()
