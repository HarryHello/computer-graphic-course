// 实验公共骨架：GLFW / OpenGL 初始化、窗口创建、截图支持。
// 设置环境变量 SCREENSHOT=<路径>.png 时，程序渲染一帧后把该帧写入文件并退出，
// 用于无交互地批量生成汇报所需的运行截图。
#pragma once

#include <glad/gl.h>
#include <GLFW/glfw3.h>
#include <stb_image.h>
#include <stb_image_write.h>

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

namespace glapp {

struct Options {
    const char* chapter;  // 章节名，显示在窗口标题中
    int width = 800;
    int height = 600;
    int samples = 0;  // MSAA 倍数，0 表示不开
};

inline void framebufferSizeCallback(GLFWwindow*, int width, int height) {
    glViewport(0, 0, width, height);
}

// 窗口标题 = “学号 - 章节名”（Task 5 要求标题中包含自己的学号）
inline GLFWwindow* open(const Options& opt) {
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
#ifdef __APPLE__
    glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GLFW_TRUE);
#endif
    if (opt.samples) glfwWindowHint(GLFW_SAMPLES, opt.samples);

    std::string title = std::string(STUDENT_ID) + " - " + opt.chapter;
    GLFWwindow* window = glfwCreateWindow(opt.width, opt.height, title.c_str(), nullptr, nullptr);
    if (!window) {
        fprintf(stderr, "创建窗口失败\n");
        glfwTerminate();
        exit(1);
    }
    glfwMakeContextCurrent(window);
    glfwSetFramebufferSizeCallback(window, framebufferSizeCallback);

    if (gladLoadGL(glfwGetProcAddress) == 0) {
        fprintf(stderr, "初始化 GLAD 失败\n");
        exit(1);
    }
    printf("OpenGL 版本: %s\n", glGetString(GL_VERSION));
    return window;
}

// 渲染完成后（swap 之前）调用：若设置了 SCREENSHOT 则保存当前帧并请求退出
inline void capture(GLFWwindow* window) {
    const char* path = getenv("SCREENSHOT");
    if (!path || !*path) return;

    int w, h;
    glfwGetFramebufferSize(window, &w, &h);
    std::vector<unsigned char> pixels(size_t(w) * h * 4);
    glPixelStorei(GL_PACK_ALIGNMENT, 1);
    glReadPixels(0, 0, w, h, GL_RGBA, GL_UNSIGNED_BYTE, pixels.data());

    // OpenGL 帧缓冲原点在左下角，PNG 原点在左上角，需要逐行上下翻转
    std::vector<unsigned char> flipped(pixels.size());
    size_t row = size_t(w) * 4;
    for (int y = 0; y < h; ++y)
        memcpy(&flipped[size_t(y) * row], &pixels[size_t(h - 1 - y) * row], row);
    stbi_write_png(path, w, h, 4, flipped.data(), w * 4);
    printf("截图已保存: %s (%dx%d)\n", path, w, h);
    glfwSetWindowShouldClose(window, GLFW_TRUE);
}

inline void close(GLFWwindow* window) {
    glfwDestroyWindow(window);
    glfwTerminate();
}

}  // namespace glapp
