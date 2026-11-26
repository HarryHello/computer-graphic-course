#pragma once

#include "Ray.h"

#include <array>
#include <memory>
#include <string>
#include <vector>

class Texture
{
public:
    virtual ~Texture() = default;
    virtual color value(double u, double v, const point3& p) const = 0;
};

class SolidTexture final : public Texture
{
public:
    explicit SolidTexture(color c) : tint(c) {}
    color value(double, double, const point3&) const override { return tint; }

private:
    color tint;
};

class CheckerTexture final : public Texture
{
public:
    CheckerTexture(color a, color b, double scale = 3) : even(a), odd(b), scale(scale) {}
    color value(double, double, const point3& p) const override
    {
        const double s = std::sin(scale * p.x()) * std::sin(scale * p.y()) * std::sin(scale * p.z());
        return s < 0 ? odd : even;
    }

private:
    color even, odd;
    double scale;
};

class NoiseTexture final : public Texture
{
public:
    explicit NoiseTexture(double scale);
    color value(double, double, const point3& p) const override;

private:
    double noise(const point3& p) const;
    double turbulence(point3 p) const;
    double scale;
    std::array<vec3, 256> gradients;
    std::array<int, 256> permX, permY, permZ;
};

class ImageTexture final : public Texture
{
public:
    explicit ImageTexture(const std::string& filename);
    color value(double u, double v, const point3&) const override;

private:
    int width = 0, height = 0;
    std::vector<unsigned char> pixels;
};
