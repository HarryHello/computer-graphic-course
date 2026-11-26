#pragma once

#include "vec3.h"

#include <cstdint>
#include <limits>

constexpr double pi = 3.14159265358979323846;
constexpr double infinity = std::numeric_limits<double>::infinity();

class Sampler
{
public:
    explicit Sampler(std::uint64_t seed) : state(seed) {}

    double uniform()
    {
        std::uint64_t z = (state += 0x9e3779b97f4a7c15ULL);
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9ULL;
        z = (z ^ (z >> 27)) * 0x94d049bb133111ebULL;
        return double((z ^ (z >> 31)) >> 11) * (1.0 / 9007199254740992.0);
    }

    double uniform(double low, double high) { return low + (high - low) * uniform(); }
    vec3 vector(double low = 0, double high = 1)
    {
        const double x = uniform(low, high), y = uniform(low, high), z = uniform(low, high);
        return {x, y, z};
    }
    vec3 unitVector()
    {
        const double z = uniform(-1, 1), angle = uniform(0, 2 * pi);
        const double radius = std::sqrt(1 - z * z);
        return {radius * std::cos(angle), radius * std::sin(angle), z};
    }
    vec3 inDisk()
    {
        const double angle = uniform(0, 2 * pi), radius = std::sqrt(uniform());
        return {radius * std::cos(angle), radius * std::sin(angle), 0};
    }

private:
    std::uint64_t state;
};

struct Ray
{
    point3 origin;
    vec3 direction;
    double time = 0;
    point3 at(double t) const { return origin + t * direction; }
};

inline vec3 reflect(const vec3& v, const vec3& normal)
{
    return v - 2 * dot(v, normal) * normal;
}

inline vec3 refract(const vec3& v, const vec3& normal, double ratio)
{
    const double cosine = std::fmin(dot(-v, normal), 1.0);
    const vec3 perpendicular = ratio * (v + cosine * normal);
    return perpendicular - std::sqrt(std::fabs(1 - perpendicular.length_squared())) * normal;
}
