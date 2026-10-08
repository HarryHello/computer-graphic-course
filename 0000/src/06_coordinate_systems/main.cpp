// 教程章节：坐标系统 + Task 4
// 内容：3D 空间中的 10 个贴纹理的箱子，经历模型->观察->投影（MVP）变换。
// Task 4：用运行参数实验 GLM 的 perspective() 参数对透视平截头体的影响：
//   --fov 45     修改竖直方向视野角（默认 45）
//   --aspect 2.0 在真实宽高比基础上乘以系数（默认 1.0）
#include "app.h"
#include "shader.h"
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include "cube_vertices.inc"

static const char* kVS = R"(#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
out vec2 TexCoord;
void main() {
    // 注意矩阵乘法顺序：从右往左作用到顶点上
    gl_Position = projection * view * model * vec4(aPos, 1.0);
    TexCoord = aTexCoord;
})";

static const char* kFS = R"(#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D texContainer;
void main() {
    FragColor = texture(texContainer, TexCoord);
})";

int main(int argc, char** argv) {
    // Task 4：FoV 与 aspect 实验参数
    float fov = 45.0f;
    float aspectScale = 1.0f;
    for (int i = 1; i < argc; ++i) {
        if (strcmp(argv[i], "--fov") == 0 && i + 1 < argc) fov = (float)atof(argv[++i]);
        if (strcmp(argv[i], "--aspect") == 0 && i + 1 < argc) aspectScale = (float)atof(argv[++i]);
    }
    printf("FoV = %.1f°，aspect = 真实宽高比 x %.2f\n", fov, aspectScale);

    GLFWwindow* window = glapp::open({"坐标系统"});

    unsigned int vao, vbo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glBindVertexArray(vao);
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(kCubeVertices), kCubeVertices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    stbi_set_flip_vertically_on_load(true);
    int w, h, n;
    unsigned char* data = stbi_load("assets/container.jpg", &w, &h, &n, 0);
    unsigned int texContainer;
    glGenTextures(1, &texContainer);
    glBindTexture(GL_TEXTURE_2D, texContainer);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, w, h, 0, GL_RGB, GL_UNSIGNED_BYTE, data);
    glGenerateMipmap(GL_TEXTURE_2D);
    stbi_image_free(data);

    glEnable(GL_DEPTH_TEST);

    Shader shader(kVS, kFS);

    while (!glfwWindowShouldClose(window)) {
        if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
            glfwSetWindowShouldClose(window, GLFW_TRUE);

        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        int fbw, fbh;
        glfwGetFramebufferSize(window, &fbw, &fbh);

        // 投影矩阵：透视平截头体由 FoV 与 aspect 决定（Task 4 实验对象）
        glm::mat4 projection = glm::perspective(
            glm::radians(fov), (float)fbw / (float)fbh * aspectScale, 0.1f, 100.0f);
        // 观察矩阵：把场景沿 -Z 推远，才能看到原点处的箱子
        glm::mat4 view = glm::translate(glm::mat4(1.0f), glm::vec3(0.0f, 0.0f, -3.0f));

        shader.use();
        shader.setMat4("view", view);
        shader.setMat4("projection", projection);
        glBindVertexArray(vao);

        for (unsigned int i = 0; i < 10; ++i) {
            glm::mat4 model = glm::mat4(1.0f);
            model = glm::translate(model, kCubePositions[i]);
            float angle = 20.0f * i;
            model = glm::rotate(model, glm::radians(angle), glm::vec3(1.0f, 0.3f, 0.5f));
            shader.setMat4("model", model);
            glDrawArrays(GL_TRIANGLES, 0, 36);
        }

        glapp::capture(window);
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    glDeleteVertexArrays(1, &vao);
    glDeleteBuffers(1, &vbo);
    glapp::close(window);
    return 0;
}
