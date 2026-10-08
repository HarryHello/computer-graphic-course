// 教程章节：你好，三角形
// 内容：用 VBO/VAO + 着色器绘制一个三角形。
// 运行参数：--wireframe 可查看线框模式（glPolygonMode）。
#include "app.h"
#include "shader.h"

static const char* kVS = R"(#version 330 core
layout (location = 0) in vec3 aPos;
void main() {
    gl_Position = vec4(aPos, 1.0);
})";

static const char* kFS = R"(#version 330 core
out vec4 FragColor;
void main() {
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
})";

int main(int argc, char** argv) {
    GLFWwindow* window = glapp::open({"你好，三角形"});

    bool wireframe = false;
    for (int i = 1; i < argc; ++i)
        if (strcmp(argv[i], "--wireframe") == 0) wireframe = true;
    if (wireframe) glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);

    // 逆时针定义的三个顶点（NDC 坐标）
    float vertices[] = {
        -0.5f, -0.5f, 0.0f,
         0.5f, -0.5f, 0.0f,
         0.0f,  0.5f, 0.0f,
    };

    unsigned int vao, vbo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glBindVertexArray(vao);
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    Shader shader(kVS, kFS);

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
