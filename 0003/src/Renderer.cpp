#include "Renderer.h"

#include <atomic>
#include <chrono>
#include <thread>

Ray Camera::ray(double u, double v, double aspect, Sampler& random) const
{
    const vec3 w = unit_vector(position - target);
    const vec3 right = unit_vector(cross(vec3(0, 1, 0), w));
    const vec3 up = cross(w, right);
    const double halfHeight = std::tan(fov * pi / 360);
    const vec3 horizontal = 2 * aspect * halfHeight * focus * right;
    const vec3 vertical = 2 * halfHeight * focus * up;
    const vec3 lens = 0.5 * aperture * random.inDisk();
    const vec3 offset = right * lens.x() + up * lens.y();
    const vec3 direction = -focus * w + (u - 0.5) * horizontal + (v - 0.5) * vertical - offset;
    return {position + offset, direction, shutter * random.uniform()};
}

static color trace(Ray ray, const Hittable& world, const Scene& scene, int depth, Sampler& random)
{
    color throughput(1, 1, 1), radiance(0, 0, 0);
    for (int bounce = 0; bounce < depth; ++bounce)
    {
        Hit hit;
        if (!world.hit(ray, 0.001, infinity, hit, random))
        {
            color background = scene.background;
            if (scene.sky)
            {
                const double t = 0.5 * (unit_vector(ray.direction).y() + 1);
                background = (1 - t) * color(1, 1, 1) + t * color(0.5, 0.7, 1);
            }
            radiance += throughput * background;
            break;
        }
        if (scene.normalPreview)
        {
            radiance += throughput * 0.5 * (hit.normal + color(1, 1, 1));
            break;
        }
        radiance += throughput * hit.material->emitted(hit.u, hit.v, hit.p);
        color attenuation;
        Ray scattered;
        if (!hit.material->scatter(ray, hit, attenuation, scattered, random))
            break;
        throughput = throughput * attenuation;
        ray = scattered;
    }
    return radiance;
}

RenderResult render(const RenderOptions& options,
    const std::function<void(int, const std::vector<color>&)>& publishRow, const std::atomic<bool>* cancel)
{
    using Clock = std::chrono::steady_clock;
    const auto buildStart = Clock::now();
    Scene scene = makeScene(options.scene);
    if (options.aperture >= 0) scene.camera.aperture = options.aperture;
    if (options.shutter >= 0) scene.camera.shutter = options.shutter;
    Object root = options.bvh && !scene.world.objects.empty() ? Object(std::make_shared<BVH>(scene.world.objects))
                              : Object(std::make_shared<HittableList>(scene.world));
    RenderResult result;
    result.objects = scene.world.objects.size();
    result.buildSeconds = std::chrono::duration<double>(Clock::now() - buildStart).count();
    result.canvas.resize(options.height, std::vector<color>(options.width));
    const auto renderStart = Clock::now();
    std::atomic<int> nextRow{0};
    const int requestedThreads = options.threads > 0 ? options.threads : int(std::thread::hardware_concurrency());
    const int threadCount = std::min(options.height, std::max(1, requestedThreads));
    std::vector<std::thread> workers;
    for (int worker = 0; worker < threadCount; ++worker)
    {
        workers.emplace_back([&] {
            for (int y = nextRow.fetch_add(1); y < options.height; y = nextRow.fetch_add(1))
            {
                if (cancel && cancel->load()) break;
                auto& row = result.canvas[y];
                for (int x = 0; x < options.width; ++x)
                {
                    if (cancel && cancel->load()) break;
                    const auto pixel = std::uint64_t(y) * options.width + x;
                    Sampler random(options.seed ^ (pixel * 0xd6e8feb86659fd93ULL));
                    color sum;
                    for (int sample = 0; sample < options.samples; ++sample)
                    {
                        const double u = (x + random.uniform()) / options.width;
                        const double v = (y + random.uniform()) / options.height;
                        sum += trace(scene.camera.ray(u, v, double(options.width) / options.height, random),
                                     *root, scene, options.depth, random);
                    }
                    for (int channel = 0; channel < 3; ++channel)
                        row[x][channel] = std::sqrt(std::max(0.0, sum[channel] / options.samples));
                }
                if (publishRow) publishRow(y, row);
            }
        });
    }
    for (auto& worker : workers)
        worker.join();
    result.renderSeconds = std::chrono::duration<double>(Clock::now() - renderStart).count();
    return result;
}
