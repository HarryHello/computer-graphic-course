#pragma once

#include "Material.h"

#include <algorithm>
#include <memory>
#include <stdexcept>
#include <vector>

struct AABB
{
    point3 low{infinity, infinity, infinity};
    point3 high{-infinity, -infinity, -infinity};

    AABB() = default;
    AABB(point3 a, point3 b) : low(a), high(b) {}
    void include(const AABB& other)
    {
        for (int axis = 0; axis < 3; ++axis)
        {
            low[axis] = std::min(low[axis], other.low[axis]);
            high[axis] = std::max(high[axis], other.high[axis]);
        }
    }
    bool hit(const Ray& ray, double min, double max) const
    {
        for (int axis = 0; axis < 3; ++axis)
        {
            if (std::fabs(ray.direction[axis]) < 1e-15)
            {
                if (ray.origin[axis] < low[axis] || ray.origin[axis] > high[axis])
                    return false;
                continue;
            }
            double a = (low[axis] - ray.origin[axis]) / ray.direction[axis];
            double b = (high[axis] - ray.origin[axis]) / ray.direction[axis];
            if (a > b)
                std::swap(a, b);
            min = std::max(min, a);
            max = std::min(max, b);
            if (max < min)
                return false;
        }
        return true;
    }
};

class Hittable
{
public:
    virtual ~Hittable() = default;
    virtual bool hit(const Ray&, double min, double max, Hit&, Sampler&) const = 0;
    virtual AABB bounds() const = 0;
};

using Object = std::shared_ptr<Hittable>;

class HittableList final : public Hittable
{
public:
    std::vector<Object> objects;
    void add(Object object) { objects.push_back(std::move(object)); }
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler& random) const override
    {
        bool found = false;
        Hit candidate;
        for (const auto& object : objects)
        {
            if (object->hit(ray, min, max, candidate, random))
            {
                found = true;
                max = candidate.t;
                result = candidate;
            }
        }
        return found;
    }
    AABB bounds() const override
    {
        AABB box;
        for (const auto& object : objects)
            box.include(object->bounds());
        return box;
    }
};

class Sphere final : public Hittable
{
public:
    Sphere(point3 center, double radius, std::shared_ptr<Material> material)
        : Sphere(center, center, radius, std::move(material)) {}
    Sphere(point3 start, point3 end, double radius, std::shared_ptr<Material> material)
        : start(start), end(end), radius(radius), material(std::move(material)) {}
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler&) const override
    {
        const point3 center = start + ray.time * (end - start);
        const vec3 oc = ray.origin - center;
        const double a = ray.direction.length_squared();
        const double halfB = dot(oc, ray.direction);
        const double c = oc.length_squared() - radius * radius;
        const double discriminant = halfB * halfB - a * c;
        if (discriminant < 0)
            return false;
        const double root = std::sqrt(discriminant);
        double t = (-halfB - root) / a;
        if (t < min || t > max)
        {
            t = (-halfB + root) / a;
            if (t < min || t > max)
                return false;
        }
        result.t = t;
        result.p = ray.at(t);
        const vec3 outward = (result.p - center) / radius;
        result.orient(ray, outward);
        result.u = (std::atan2(-outward.z(), outward.x()) + pi) / (2 * pi);
        result.v = std::acos(std::clamp(-outward.y(), -1.0, 1.0)) / pi;
        result.material = material;
        return true;
    }
    AABB bounds() const override
    {
        const vec3 extent(radius, radius, radius);
        AABB result(start - extent, start + extent);
        result.include(AABB(end - extent, end + extent));
        return result;
    }

private:
    point3 start, end;
    double radius;
    std::shared_ptr<Material> material;
};

class Quad final : public Hittable
{
public:
    Quad(point3 corner, vec3 u, vec3 v, std::shared_ptr<Material> material)
        : corner(corner), u(u), v(v), material(std::move(material))
    {
        const vec3 n = cross(u, v);
        normal = unit_vector(n);
        reciprocalNormal = n / n.length_squared();
        box = AABB(corner, corner);
        for (const point3& p : {corner + u, corner + v, corner + u + v})
            box.include(AABB(p, p));
        for (int axis = 0; axis < 3; ++axis)
        {
            box.low[axis] -= 0.0001;
            box.high[axis] += 0.0001;
        }
    }
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler&) const override
    {
        const double denominator = dot(normal, ray.direction);
        if (std::fabs(denominator) < 1e-12)
            return false;
        const double t = dot(corner - ray.origin, normal) / denominator;
        if (t < min || t > max)
            return false;
        const vec3 planar = ray.at(t) - corner;
        const double alpha = dot(reciprocalNormal, cross(planar, v));
        const double beta = dot(reciprocalNormal, cross(u, planar));
        if (alpha < 0 || alpha > 1 || beta < 0 || beta > 1)
            return false;
        result.t = t;
        result.p = ray.at(t);
        result.u = alpha;
        result.v = beta;
        result.material = material;
        result.orient(ray, normal);
        return true;
    }
    AABB bounds() const override { return box; }

private:
    point3 corner;
    vec3 u, v, normal, reciprocalNormal;
    std::shared_ptr<Material> material;
    AABB box;
};

inline Object makeBox(point3 low, point3 high, std::shared_ptr<Material> material)
{
    auto box = std::make_shared<HittableList>();
    const vec3 x(high.x() - low.x(), 0, 0), y(0, high.y() - low.y(), 0), z(0, 0, high.z() - low.z());
    box->add(std::make_shared<Quad>(point3(low.x(), low.y(), high.z()), x, y, material));
    box->add(std::make_shared<Quad>(point3(high.x(), low.y(), high.z()), -z, y, material));
    box->add(std::make_shared<Quad>(point3(high.x(), low.y(), low.z()), -x, y, material));
    box->add(std::make_shared<Quad>(low, z, y, material));
    box->add(std::make_shared<Quad>(point3(low.x(), high.y(), high.z()), x, -z, material));
    box->add(std::make_shared<Quad>(low, x, z, material));
    return box;
}

class Transform final : public Hittable
{
public:
    Transform(Object object, double angle, vec3 offset)
        : object(std::move(object)), sine(std::sin(angle * pi / 180)),
          cosine(std::cos(angle * pi / 180)), offset(offset)
    {
        const AABB original = this->object->bounds();
        for (int mask = 0; mask < 8; ++mask)
        {
            point3 p;
            for (int axis = 0; axis < 3; ++axis)
                p[axis] = mask & (1 << axis) ? original.high[axis] : original.low[axis];
            p = forward(p) + offset;
            box.include(AABB(p, p));
        }
    }
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler& random) const override
    {
        Ray local{inverse(ray.origin - offset), inverse(ray.direction), ray.time};
        if (!object->hit(local, min, max, result, random))
            return false;
        result.p = forward(result.p) + offset;
        result.normal = forward(result.normal);
        return true;
    }
    AABB bounds() const override { return box; }

private:
    vec3 forward(vec3 p) const { return {cosine * p.x() + sine * p.z(), p.y(), -sine * p.x() + cosine * p.z()}; }
    vec3 inverse(vec3 p) const { return {cosine * p.x() - sine * p.z(), p.y(), sine * p.x() + cosine * p.z()}; }
    Object object;
    double sine, cosine;
    vec3 offset;
    AABB box;
};

class ConstantMedium final : public Hittable
{
public:
    ConstantMedium(Object boundary, double density, color tint)
        : boundary(std::move(boundary)), negativeInverseDensity(-1 / density),
          phase(std::make_shared<Isotropic>(tint)) {}
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler& random) const override
    {
        Hit entry, exit;
        if (!boundary->hit(ray, -infinity, infinity, entry, random) ||
            !boundary->hit(ray, entry.t + 0.0001, infinity, exit, random))
            return false;
        const double start = std::max({entry.t, min, 0.0});
        const double end = std::min(exit.t, max);
        if (start >= end)
            return false;
        const double length = ray.direction.length();
        const double distance = negativeInverseDensity * std::log(std::max(random.uniform(), 1e-15));
        if (distance > (end - start) * length)
            return false;
        result.t = start + distance / length;
        result.p = ray.at(result.t);
        result.normal = {1, 0, 0};
        result.frontFace = true;
        result.u = result.v = 0;
        result.material = phase;
        return true;
    }
    AABB bounds() const override { return boundary->bounds(); }

private:
    Object boundary;
    double negativeInverseDensity;
    std::shared_ptr<Material> phase;
};

class BVH final : public Hittable
{
public:
    explicit BVH(std::vector<Object> objects) : BVH(objects, 0, objects.size()) {}
    bool hit(const Ray& ray, double min, double max, Hit& result, Sampler& random) const override
    {
        if (!box.hit(ray, min, max))
            return false;
        const bool hitLeft = left->hit(ray, min, max, result, random);
        if (!right)
            return hitLeft;
        return right->hit(ray, min, hitLeft ? result.t : max, result, random) || hitLeft;
    }
    AABB bounds() const override { return box; }

private:
    BVH(std::vector<Object>& objects, std::size_t begin, std::size_t end)
    {
        if (begin == end)
            throw std::invalid_argument("Cannot build a BVH from an empty object list");
        for (auto i = begin; i < end; ++i)
            box.include(objects[i]->bounds());
        if (end - begin == 1)
        {
            left = objects[begin];
            return;
        }
        int axis = 0;
        const vec3 extent = box.high - box.low;
        if (extent.y() > extent.x()) axis = 1;
        if (extent.z() > extent[axis]) axis = 2;
        const auto middle = begin + (end - begin) / 2;
        std::nth_element(objects.begin() + begin, objects.begin() + middle, objects.begin() + end,
            [axis](const Object& a, const Object& b) {
                const auto ab = a->bounds(), bb = b->bounds();
                return ab.low[axis] + ab.high[axis] < bb.low[axis] + bb.high[axis];
            });
        left = Object(new BVH(objects, begin, middle));
        right = Object(new BVH(objects, middle, end));
    }
    Object left, right;
    AABB box;
};
