// 教程章节：着色器 + Task 2
// 内容：用顶点属性（颜色）插值绘制三角形。
// Task 2：运行参数 --flip 时改用“上下颠倒”的顶点着色器——在 gl_Position 赋值后
//         把 y 分量取反，三角形即绕 X 轴翻转。
#include "app.h"
#include "shader.h"

// 默认顶点着色器
static const char* kVSNormal = R"(#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
out vec3 ourColor;
void main() {
    gl_Position = vec4(aPos, 1.0);
    ourColor = aColor;
})";

// Task 2：上下颠倒的顶点着色器——仅多一行 gl_Position.y = -gl_Position.y;
static const char* kVSFlipped = R"(#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
out vec3 ourColor;
void main() {
    gl_Position = vec4(aPos, 1.0);
    gl_Position.y = -gl_Position.y;  // Task 2: y 取反，三角形上下颠倒
    ourColor = aColor;
})";

static const char* kFS = R"(#version 330 core
in vec3 ourColor;
out vec4 FragColor;
void main() {
    FragColor = vec4(ourColor, 1.0);
})";

int main(int argc, char** argv) {
    GLFWwindow* window = glapp::open({"着色器"});

    bool flip = false;
    for (int i = 1; i < argc; ++i)
        if (strcmp(argv[i], "--flip") == 0) flip = true;

    // 位置 + 颜色 交错数据；红色在下、蓝色在上，颠倒后可以明显看出变化
    float vertices[] = {
        // 位置              // 颜色
        -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f,  // 左下：红
         0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,  // 右下：绿
         0.0f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f,  // 顶上：蓝
    };

    unsigned int vao, vbo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glBindVertexArray(vao);
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 6 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 6 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    Shader shader(flip ? kVSFlipped : kVSNormal, kFS);

    while (!glfwWindowShouldClose(window)) {
        if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
            glfwSetWindowShouldClose(window, GLFW_TRUE);

        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        shader.use();
        glBindVertexArray(vao);
        glDrawArrays(GL_TRIANGLES, 0, 3);

        glapp::capture(window);
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    glDeleteVertexArrays(1, &vao);
    glDeleteBuffers(1, &vbo);
    glapp::close(window);
    return 0;
}
