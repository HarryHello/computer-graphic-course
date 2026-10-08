// 教程章节：变换
// 内容：用 GLM 构建模型矩阵，让箱子在右下角自转并缩小。
#include "app.h"
#include "shader.h"
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>

static const char* kVS = R"(#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
uniform mat4 model;
out vec2 TexCoord;
void main() {
    gl_Position = model * vec4(aPos, 1.0);
    TexCoord = aTexCoord;
})";

static const char* kFS = R"(#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D texContainer;
uniform sampler2D texFace;
void main() {
    FragColor = mix(texture(texContainer, TexCoord), texture(texFace, TexCoord), 0.4f);
})";

static float kVertices[] = {
    // 位置              // 纹理坐标
    -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,
     0.5f, -0.5f, 0.0f,  1.0f, 0.0f,
     0.5f,  0.5f, 0.0f,  1.0f, 1.0f,
    -0.5f,  0.5f, 0.0f,  0.0f, 1.0f,
};
static unsigned int kIndices[] = {0, 1, 2, 0, 2, 3};

int main() {
    GLFWwindow* window = glapp::open({"变换"});

    unsigned int vao, vbo, ebo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glGenBuffers(1, &ebo);
    glBindVertexArray(vao);
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(kVertices), kVertices, GL_STATIC_DRAW);
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, ebo);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(kIndices), kIndices, GL_STATIC_DRAW);
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    unsigned int texContainer, texFace;
    {
        stbi_set_flip_vertically_on_load(true);
        int w, h, n;
        unsigned char* data = stbi_load("assets/container.jpg", &w, &h, &n, 0);
        glGenTextures(1, &texContainer);
        glBindTexture(GL_TEXTURE_2D, texContainer);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, w, h, 0, GL_RGB, GL_UNSIGNED_BYTE, data);
        glGenerateMipmap(GL_TEXTURE_2D);
        stbi_image_free(data);
        data = stbi_load("assets/awesomeface.png", &w, &h, &n, 0);
        glGenTextures(1, &texFace);
        glBindTexture(GL_TEXTURE_2D, texFace);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, w, h, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
        glGenerateMipmap(GL_TEXTURE_2D);
        stbi_image_free(data);
    }

    Shader shader(kVS, kFS);
    shader.use();
    shader.setInt("texContainer", 0);
    shader.setInt("texFace", 1);

    while (!glfwWindowShouldClose(window)) {
        if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
            glfwSetWindowShouldClose(window, GLFW_TRUE);

        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D, texContainer);
        glActiveTexture(GL_TEXTURE1);
        glBindTexture(GL_TEXTURE_2D, texFace);

        // 平移到右下角 -> 绕 Z 轴旋转 -> 缩小为原来的一半
        glm::mat4 model = glm::mat4(1.0f);
        model = glm::translate(model, glm::vec3(0.5f, -0.5f, 0.0f));
        model = glm::rotate(model, (float)glfwGetTime(), glm::vec3(0.0f, 0.0f, 1.0f));
        model = glm::scale(model, glm::vec3(0.5f, 0.5f, 0.5f));

        shader.use();
        shader.setMat4("model", model);
        glBindVertexArray(vao);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);

        glapp::capture(window);
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    glDeleteVertexArrays(1, &vao);
    glDeleteBuffers(1, &vbo);
    glDeleteBuffers(1, &ebo);
    glapp::close(window);
    return 0;
}
