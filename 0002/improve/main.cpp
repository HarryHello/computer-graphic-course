#include "rasterizer.hpp"
#include "savePng.h"

#include <glm/gtc/matrix_transform.hpp>

#include <cmath>
#include <iostream>
#include <string>

int main(int argc, char** argv)
{
    bool ssaa = false;
    const char* filename = nullptr;
    for (int i = 1; i < argc; ++i)
    {
        const std::string arg = argv[i];
        if (arg == "--ssaa")
            ssaa = true;
        else if (!filename && !arg.empty() && arg[0] != '-')
            filename = argv[i];
        else
        {
            std::cerr << "Usage: CGAssignment2-Improve [--ssaa] [file.png]\n";
            return 1;
        }
    }
    if (!filename)
        filename = ssaa ? "output-ssaa.png" : "output-1.png";

    constexpr int width = 700;
    constexpr int height = 700;
    rst::rasterizer r(width, height, ssaa);
    const auto positions = r.load_positions({
        {2, 0, -2}, {0, 2, -2}, {-2, 0, -2},
        {3.5, -1, -5}, {2.5, 1.5, -5}, {-1, 0.5, -5}
    });
    const auto indices = r.load_indices({{0, 1, 2}, {3, 4, 5}});
    const glm::vec3 nearColor(217, 238, 185);
    const glm::vec3 farColor(185, 217, 238);
    const auto colors = r.load_colors({nearColor, nearColor, nearColor, farColor, farColor, farColor});

    r.set_model(glm::mat4(1.0f));
    r.set_view(glm::translate(glm::mat4(1.0f), glm::vec3(0, 0, -5)));
    r.set_projection(glm::perspective(glm::radians(45.0f), float(width) / height, 0.1f, 50.0f));
    r.clear(rst::Buffers::Color | rst::Buffers::Depth);
    r.draw(positions, indices, colors, rst::Primitive::Triangle);

    std::vector<unsigned char> pixels;
    pixels.reserve(r.frame_buffer().size() * 3);
    for (const auto& color : r.frame_buffer())
    {
        for (int channel = 0; channel < 3; ++channel)
            pixels.push_back(static_cast<unsigned char>(std::lround(color[channel])));
    }
    if (!savePng(filename, width, height, 3, pixels.data()))
    {
        std::cerr << "Could not write " << filename << '\n';
        return 1;
    }
    return 0;
}
