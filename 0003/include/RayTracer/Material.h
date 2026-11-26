#pragma once

#include "Texture.h"

#include <algorithm>

class Material;

struct Hit
{
    point3 p;
    vec3 normal;
    double t = 0, u = 0, v = 0;
    bool frontFace = true;
    std::shared_ptr<Material> material;

    void orient(const Ray& ray, const vec3& outward)
    {
        frontFace = dot(ray.direction, outward) < 0;
        normal = frontFace ? outward : -outward;
    }
};

class Material
{
public:
    virtual ~Material() = default;
    virtual bool scatter(const Ray&, const Hit&, color&, Ray&, Sampler&) const { return false; }
    virtual color emitted(double, double, const point3&) const { return {0, 0, 0}; }
};

class Lambertian final : public Material
{
public:
    explicit Lambertian(color c) : texture(std::make_shared<SolidTexture>(c)) {}
    explicit Lambertian(std::shared_ptr<Texture> t) : texture(std::move(t)) {}
    bool scatter(const Ray& ray, const Hit& hit, color& attenuation, Ray& next, Sampler& random) const override
    {
        vec3 direction = hit.normal + random.unitVector();
        if (direction.length_squared() < 1e-16)
            direction = hit.normal;
        next = {hit.p, direction, ray.time};
        attenuation = texture->value(hit.u, hit.v, hit.p);
        return true;
    }

private:
    std::shared_ptr<Texture> texture;
};

class Metal final : public Material
{
public:
    Metal(color c, double fuzz) : albedo(c), fuzz(std::clamp(fuzz, 0.0, 1.0)) {}
    bool scatter(const Ray& ray, const Hit& hit, color& attenuation, Ray& next, Sampler& random) const override
    {
        next = {hit.p, reflect(unit_vector(ray.direction), hit.normal) + fuzz * random.unitVector(), ray.time};
        attenuation = albedo;
        return dot(next.direction, hit.normal) > 0;
    }

private:
    color albedo;
    double fuzz;
};

class Dielectric final : public Material
{
public:
    explicit Dielectric(double index) : index(index) {}
    bool scatter(const Ray& ray, const Hit& hit, color& attenuation, Ray& next, Sampler& random) const override
    {
        attenuation = {1, 1, 1};
        const double ratio = hit.frontFace ? 1 / index : index;
        const vec3 direction = unit_vector(ray.direction);
        const double cosine = std::fmin(dot(-direction, hit.normal), 1.0);
        const double sine = std::sqrt(1 - cosine * cosine);
        double r0 = (1 - ratio) / (1 + ratio);
        r0 *= r0;
        const double reflectance = r0 + (1 - r0) * std::pow(1 - cosine, 5);
        const vec3 outgoing = ratio * sine > 1 || random.uniform() < reflectance
                            ? reflect(direction, hit.normal) : refract(direction, hit.normal, ratio);
        next = {hit.p, outgoing, ray.time};
        return true;
    }

private:
    double index;
};

class DiffuseLight final : public Material
{
public:
    explicit DiffuseLight(color c) : intensity(c) {}
    color emitted(double, double, const point3&) const override { return intensity; }

private:
    color intensity;
};

class Isotropic final : public Material
{
public:
    explicit Isotropic(color c) : albedo(c) {}
    bool scatter(const Ray& ray, const Hit& hit, color& attenuation, Ray& next, Sampler& random) const override
    {
        attenuation = albedo;
        next = {hit.p, random.unitVector(), ray.time};
        return true;
    }

private:
    color albedo;
};
