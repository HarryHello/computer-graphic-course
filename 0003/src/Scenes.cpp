#include "Renderer.h"

#include <stdexcept>

#ifndef ASSET_DIR
#define ASSET_DIR "assets"
#endif

using std::make_shared;

static void sphere(Scene& scene, point3 p, double radius, std::shared_ptr<Material> material)
{
    scene.world.add(make_shared<Sphere>(p, radius, std::move(material)));
}

static std::shared_ptr<Lambertian> diffuse(color c)
{
    return make_shared<Lambertian>(c);
}

static Scene randomSpheres(bool moving)
{
    Scene scene;
    Sampler random(2026);
    scene.camera.aperture = 0.1;
    scene.camera.shutter = moving ? 1 : 0;
    sphere(scene, {0, -1000, 0}, 1000,
        make_shared<Lambertian>(make_shared<CheckerTexture>(color(0.2, 0.3, 0.1), color(0.9, 0.9, 0.9))));
    for (int a = -11; a < 11; ++a)
        for (int b = -11; b < 11; ++b)
        {
            const double choice = random.uniform();
            const point3 center(a + 0.9 * random.uniform(), 0.2, b + 0.9 * random.uniform());
            if ((center - point3(4, 0.2, 0)).length() <= 0.9)
                continue;
            if (choice < 0.8)
            {
                const color first = random.vector(), second = random.vector();
                const point3 end = moving ? center + vec3(0, random.uniform(0, 0.5), 0) : center;
                scene.world.add(make_shared<Sphere>(center, end, 0.2, diffuse(first * second)));
            }
            else if (choice < 0.95)
            {
                const color albedo = random.vector(0.5, 1);
                const double fuzz = random.uniform(0, 0.5);
                sphere(scene, center, 0.2, make_shared<Metal>(albedo, fuzz));
            }
            else
                sphere(scene, center, 0.2, make_shared<Dielectric>(1.5));
        }
    sphere(scene, {0, 1, 0}, 1, make_shared<Dielectric>(1.5));
    sphere(scene, {-4, 1, 0}, 1, diffuse({0.4, 0.2, 0.1}));
    sphere(scene, {4, 1, 0}, 1, make_shared<Metal>(color(0.7, 0.6, 0.5), 0));
    return scene;
}

static Scene cornell(bool smoke)
{
    Scene scene;
    scene.sky = false;
    scene.camera.position = {278, 278, -800};
    scene.camera.target = {278, 278, 0};
    scene.camera.fov = 40;
    const auto white = diffuse({0.73, 0.73, 0.73});
    scene.world.add(make_shared<Quad>(point3(555, 0, 0), vec3(0, 555, 0), vec3(0, 0, 555), diffuse({0.12, 0.45, 0.15})));
    scene.world.add(make_shared<Quad>(point3(0, 0, 555), vec3(0, 555, 0), vec3(0, 0, -555), diffuse({0.65, 0.05, 0.05})));
    scene.world.add(make_shared<Quad>(point3(0, 0, 0), vec3(555, 0, 0), vec3(0, 0, 555), white));
    scene.world.add(make_shared<Quad>(point3(0, 555, 555), vec3(555, 0, 0), vec3(0, 0, -555), white));
    scene.world.add(make_shared<Quad>(point3(0, 0, 555), vec3(555, 0, 0), vec3(0, 555, 0), white));
    if (smoke)
        scene.world.add(make_shared<Quad>(point3(113, 554, 127), vec3(330, 0, 0), vec3(0, 0, 305),
                                         make_shared<DiffuseLight>(color(7, 7, 7))));
    else
        scene.world.add(make_shared<Quad>(point3(213, 554, 227), vec3(130, 0, 0), vec3(0, 0, 105),
                                         make_shared<DiffuseLight>(color(15, 15, 15))));
    Object tall = make_shared<Transform>(makeBox({0, 0, 0}, {165, 330, 165}, white), 15, vec3(265, 0, 295));
    Object shortBox = make_shared<Transform>(makeBox({0, 0, 0}, {165, 165, 165}, white), -18, vec3(130, 0, 65));
    scene.world.add(smoke ? Object(make_shared<ConstantMedium>(tall, 0.01, color(0, 0, 0))) : tall);
    scene.world.add(smoke ? Object(make_shared<ConstantMedium>(shortBox, 0.01, color(1, 1, 1))) : shortBox);
    return scene;
}

static Scene finalScene()
{
    Scene scene;
    scene.sky = false;
    scene.camera.position = {478, 278, -600};
    scene.camera.target = {278, 278, 0};
    scene.camera.fov = 40;
    scene.camera.shutter = 1;
    Sampler random(519);
    HittableList ground;
    const auto green = diffuse({0.48, 0.83, 0.53});
    for (int i = 0; i < 20; ++i)
        for (int j = 0; j < 20; ++j)
        {
            const double x = -1000 + i * 100, z = -1000 + j * 100;
            ground.add(makeBox({x, 0, z}, {x + 100, random.uniform(1, 101), z + 100}, green));
        }
    scene.world.add(make_shared<BVH>(ground.objects));
    scene.world.add(make_shared<Quad>(point3(123, 554, 147), vec3(300, 0, 0), vec3(0, 0, 265),
                                     make_shared<DiffuseLight>(color(7, 7, 7))));
    scene.world.add(make_shared<Sphere>(point3(400, 400, 200), point3(430, 400, 200), 50, diffuse({0.7, 0.3, 0.1})));
    sphere(scene, {260, 150, 45}, 50, make_shared<Dielectric>(1.5));
    sphere(scene, {0, 150, 145}, 50, make_shared<Metal>(color(0.8, 0.8, 0.9), 0.35));
    const auto boundary = make_shared<Sphere>(point3(360, 150, 145), 70, make_shared<Dielectric>(1.5));
    scene.world.add(boundary);
    scene.world.add(make_shared<ConstantMedium>(boundary, 0.2, color(0.2, 0.4, 0.9)));
    const auto atmosphere = make_shared<Sphere>(point3(0, 0, 0), 5000, make_shared<Dielectric>(1.5));
    scene.world.add(make_shared<ConstantMedium>(atmosphere, 0.0001, color(1, 1, 1)));
    sphere(scene, {400, 200, 400}, 100, make_shared<Lambertian>(make_shared<ImageTexture>(ASSET_DIR "/uv-map.ppm")));
    sphere(scene, {220, 280, 300}, 80, make_shared<Lambertian>(make_shared<NoiseTexture>(0.1)));
    HittableList cluster;
    const auto white = diffuse({0.73, 0.73, 0.73});
    for (int i = 0; i < 1000; ++i)
        cluster.add(make_shared<Sphere>(random.vector(0, 165), 10, white));
    scene.world.add(make_shared<Transform>(make_shared<BVH>(cluster.objects), 15, vec3(-100, 270, 395)));
    return scene;
}

Scene makeScene(const std::string& name)
{
    if (name == "random" || name == "motion")
        return randomSpheres(name == "motion");
    if (name == "cornell" || name == "smoke")
        return cornell(name == "smoke");
    if (name == "final")
        return finalScene();

    Scene scene;
    if (name == "sky" || name == "sphere")
    {
        scene.camera.position = {0, 0, 0};
        scene.camera.target = {0, 0, -1};
        scene.camera.fov = 90;
        scene.camera.focus = 1;
        if (name == "sphere")
        {
            scene.normalPreview = true;
            sphere(scene, {0, 0, -1}, 0.5, diffuse({0.7, 0.3, 0.3}));
            sphere(scene, {0, -100.5, -1}, 100, diffuse({0.8, 0.8, 0}));
        }
    }
    else if (name == "materials" || name == "defocus")
    {
        scene.camera.position = {3, 1.2, 2};
        scene.camera.target = {0, 0, -1};
        scene.camera.fov = 35;
        scene.camera.focus = (scene.camera.position - scene.camera.target).length();
        scene.camera.aperture = name == "defocus" ? 0.4 : 0;
        sphere(scene, {0, -100.5, -1}, 100, diffuse({0.8, 0.8, 0}));
        sphere(scene, {0, 0, -1}, 0.5, diffuse({0.1, 0.2, 0.5}));
        sphere(scene, {-1, 0, -1}, 0.5, make_shared<Dielectric>(1.5));
        sphere(scene, {-1, 0, -1}, 0.45, make_shared<Dielectric>(1 / 1.5));
        sphere(scene, {1, 0, -1}, 0.5, make_shared<Metal>(color(0.8, 0.6, 0.2), 0.1));
    }
    else if (name == "textures")
    {
        const auto checker = make_shared<Lambertian>(make_shared<CheckerTexture>(color(0.2, 0.3, 0.1), color(0.9, 0.9, 0.9)));
        sphere(scene, {0, -10, 0}, 10, checker);
        sphere(scene, {0, 10, 0}, 10, checker);
    }
    else if (name == "noise" || name == "image" || name == "light")
    {
        scene.camera.target = {0, 2, 0};
        const auto noise = make_shared<Lambertian>(make_shared<NoiseTexture>(4));
        sphere(scene, {0, -1000, 0}, 1000, noise);
        sphere(scene, {0, 2, 0}, 2, name == "image"
            ? make_shared<Lambertian>(make_shared<ImageTexture>(ASSET_DIR "/uv-map.ppm")) : noise);
        if (name == "light")
        {
            scene.sky = false;
            scene.camera.position = {26, 3, 6};
            scene.camera.target = {0, 2, 0};
            const auto light = make_shared<DiffuseLight>(color(4, 4, 4));
            sphere(scene, {0, 7, 0}, 2, light);
            scene.world.add(make_shared<Quad>(point3(3, 1, -2), vec3(2, 0, 0), vec3(0, 2, 0), light));
        }
    }
    else
        throw std::invalid_argument("Unknown scene: " + name);
    return scene;
}
