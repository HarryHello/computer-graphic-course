#include "Renderer.h"

#include <algorithm>
#include <stdexcept>

#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image_write.h"

void saveImage(const std::string& filename, const Canvas& canvas)
{
    const int height = int(canvas.size()), width = int(canvas.front().size());
    std::vector<unsigned char> pixels;
    pixels.reserve(std::size_t(width) * height * 3);
    for (auto row = canvas.rbegin(); row != canvas.rend(); ++row)
        for (const auto& pixel : *row)
            for (int channel = 0; channel < 3; ++channel)
                pixels.push_back(static_cast<unsigned char>(256 * std::clamp(pixel[channel], 0.0, 0.999)));
    if (!stbi_write_png(filename.c_str(), width, height, 3, pixels.data(), width * 3))
        throw std::runtime_error("Could not save image: " + filename);
}
