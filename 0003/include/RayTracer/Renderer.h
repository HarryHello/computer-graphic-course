#pragma once

#include "Geometry.h"

#include <atomic>
#include <functional>
#include <string>

struct Camera
{
    point3 position{13, 2, 3}, target{0, 0, 0};
    double fov = 20, aperture = 0, focus = 10, shutter = 0;
    Ray ray(double u, double v, double aspect, Sampler& random) const;
};

struct Scene
{
    HittableList world;
    Camera camera;
    color background{0, 0, 0};
    bool sky = true;
    bool normalPreview = false;
};

struct RenderOptions
{
    std::string scene = "random", output;
    int width = 640, height = 360, samples = 64, depth = 32, threads = 0;
    std::uint64_t seed = 2026;
    bool bvh = true;
    double aperture = -1, shutter = -1;
};

using Canvas = std::vector<std::vector<color>>;

struct RenderResult
{
    Canvas canvas;
    double buildSeconds = 0, renderSeconds = 0;
    std::size_t objects = 0;
};

Scene makeScene(const std::string& name);
RenderResult render(const RenderOptions& options,
    const std::function<void(int, const std::vector<color>&)>& publishRow = {},
    const std::atomic<bool>* cancel = nullptr);
void saveImage(const std::string& filename, const Canvas& canvas);
