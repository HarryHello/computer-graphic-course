#include "savePng.h"

#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image_write.h"

bool savePng(const char* filename, int width, int height, int channels,
             const unsigned char* pixels)
{
    return stbi_write_png(filename, width, height, channels, pixels, width * channels) != 0;
}
