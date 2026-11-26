#include "Texture.h"

#include <algorithm>
#include <numeric>
#include <stdexcept>

#include "stb_image.h"

NoiseTexture::NoiseTexture(double scale) : scale(scale)
{
    Sampler random(731);
    for (auto& gradient : gradients)
        gradient = random.unitVector();
    for (auto* perm : {&permX, &permY, &permZ})
    {
        std::iota(perm->begin(), perm->end(), 0);
        for (int i = 255; i > 0; --i)
            std::swap((*perm)[i], (*perm)[int(random.uniform() * (i + 1))]);
    }
}

double NoiseTexture::noise(const point3& p) const
{
    int cell[3];
    double fraction[3], smooth[3];
    for (int axis = 0; axis < 3; ++axis)
    {
        cell[axis] = int(std::floor(p[axis]));
        fraction[axis] = p[axis] - cell[axis];
        smooth[axis] = fraction[axis] * fraction[axis] * (3 - 2 * fraction[axis]);
    }
    double sum = 0;
    for (int x = 0; x < 2; ++x)
        for (int y = 0; y < 2; ++y)
            for (int z = 0; z < 2; ++z)
            {
                const auto& g = gradients[permX[(cell[0] + x) & 255] ^
                                          permY[(cell[1] + y) & 255] ^
                                          permZ[(cell[2] + z) & 255]];
                const double weight = (x ? smooth[0] : 1 - smooth[0]) *
                                      (y ? smooth[1] : 1 - smooth[1]) *
                                      (z ? smooth[2] : 1 - smooth[2]);
                sum += weight * dot(g, vec3(fraction[0] - x, fraction[1] - y, fraction[2] - z));
            }
    return sum;
}

double NoiseTexture::turbulence(point3 p) const
{
    double sum = 0, weight = 1;
    for (int octave = 0; octave < 7; ++octave)
    {
        sum += weight * noise(p);
        p *= 2;
        weight *= 0.5;
    }
    return std::fabs(sum);
}

color NoiseTexture::value(double, double, const point3& p) const
{
    return color(1, 1, 1) * 0.5 * (1 + std::sin(scale * p.z() + 10 * turbulence(p)));
}

ImageTexture::ImageTexture(const std::string& filename)
{
    int channels;
    unsigned char* data = stbi_load(filename.c_str(), &width, &height, &channels, 3);
    if (!data)
        throw std::runtime_error("Could not load texture: " + filename);
    pixels.assign(data, data + std::size_t(width) * height * 3);
    stbi_image_free(data);
}

color ImageTexture::value(double u, double v, const point3&) const
{
    const int x = std::min(width - 1, int(std::clamp(u, 0.0, 1.0) * width));
    const int y = std::min(height - 1, int((1 - std::clamp(v, 0.0, 1.0)) * height));
    const auto index = (std::size_t(y) * width + x) * 3;
    return color(pixels[index], pixels[index + 1], pixels[index + 2]) / 255;
}
