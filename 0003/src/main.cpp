/*The MIT License (MIT)

Copyright (c) 2021-Present, Wencong Yang (yangwc3@mail2.sysu.edu.cn).

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#include "Renderer.h"
#include "WindowsApp.h"

#include <atomic>
#include <exception>
#include <iomanip>
#include <iostream>
#include <mutex>
#include <stdexcept>
#include <thread>

static void usage()
{
    std::cout << "CGAssignment3 [--scene name] [--output file.png]\n"
              << "Scenes: sky sphere materials defocus random motion textures noise image light cornell smoke final\n"
              << "Options: --width N --height N --samples N --depth N --seed N --threads N\n"
              << "         --no-bvh --aperture X --shutter X (0..1)\n";
}

static RenderOptions parse(int argc, char** argv)
{
    RenderOptions options;
    bool explicitHeight = false;
    for (int i = 1; i < argc; ++i)
    {
        const std::string name = argv[i];
        if (name == "--no-bvh")
        {
            options.bvh = false;
            continue;
        }
        if (i + 1 == argc)
            throw std::invalid_argument("Missing value for " + name);
        const std::string value = argv[++i];
        if (name == "--scene") options.scene = value;
        else if (name == "--output")
        {
            if (value.empty())
                throw std::invalid_argument("Output filename must not be empty");
            options.output = value;
        }
        else
        {
            std::size_t end = 0;
            if (name == "--aperture" || name == "--shutter")
            {
                const double n = std::stod(value, &end);
                if (end != value.size() || !std::isfinite(n) || n < 0 ||
                    (name == "--shutter" && n > 1))
                    throw std::invalid_argument("Invalid value for " + name);
                if (name == "--aperture") options.aperture = n;
                else options.shutter = n;
                continue;
            }
            const auto n = std::stoll(value, &end);
            if (end != value.size() || n < 0)
                throw std::invalid_argument("Invalid value for " + name);
            if (name == "--seed") options.seed = std::uint64_t(n);
            else
            {
                if (n > 100000)
                    throw std::invalid_argument("Value too large for " + name);
                if (name == "--width") options.width = int(n);
                else if (name == "--height") { options.height = int(n); explicitHeight = true; }
                else if (name == "--samples") options.samples = int(n);
                else if (name == "--depth") options.depth = int(n);
                else if (name == "--threads") options.threads = int(n);
                else throw std::invalid_argument("Unknown option: " + name);
            }
        }
    }
    if (!explicitHeight)
        options.height = (options.scene == "cornell" || options.scene == "smoke" || options.scene == "final")
                       ? options.width : std::max(2, options.width * 9 / 16);
    if (options.width < 2 || options.height < 2 || options.width > 4096 || options.height > 4096 ||
        options.samples < 1 || options.depth < 1 || options.depth > 256 || options.threads > 256)
        throw std::invalid_argument("Use dimensions 2..4096, samples 1..100000, depth 1..256, threads 0..256");
    return options;
}

int main(int argc, char** argv)
{
    try
    {
        for (int i = 1; i < argc; ++i)
            if (std::string(argv[i]) == "--help")
            {
                usage();
                return 0;
            }
        const auto options = parse(argc, argv);
        RenderResult result;
        if (!options.output.empty())
        {
            result = render(options);
            saveImage(options.output, result.canvas);
        }
        else
        {
            auto app = WindowsApp::getInstance(options.width, options.height, "CGAssignment3: Ray Tracing");
            if (!app)
                return 1;
            Canvas display(options.height, std::vector<color>(options.width));
            std::mutex canvasMutex;
            std::atomic<bool> cancel{false};
            std::atomic<bool> finished{false};
            std::exception_ptr failure;
            std::thread worker([&] {
                try
                {
                    result = render(options, [&](int y, const std::vector<color>& row) {
                        std::lock_guard<std::mutex> lock(canvasMutex);
                        display[y] = row;
                    }, &cancel);
                }
                catch (...) { failure = std::current_exception(); }
                finished = true;
            });
            while (!app->shouldWindowClose())
            {
                app->processEvent();
                if (finished.load() && failure)
                    break;
                {
                    std::lock_guard<std::mutex> lock(canvasMutex);
                    app->updateScreenSurface(display);
                }
                SDL_Delay(16);
            }
            cancel = true;
            worker.join();
            if (failure) std::rethrow_exception(failure);
        }
        std::cout << std::fixed << std::setprecision(6)
                  << "{\"scene\":\"" << options.scene << "\",\"width\":" << options.width
                  << ",\"height\":" << options.height << ",\"samples\":" << options.samples
                  << ",\"depth\":" << options.depth << ",\"seed\":" << options.seed
                  << ",\"bvh\":" << (options.bvh ? "true" : "false")
                  << ",\"objects\":" << result.objects << ",\"build_s\":" << result.buildSeconds
                  << ",\"render_s\":" << result.renderSeconds << "}\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        return 1;
    }
}
