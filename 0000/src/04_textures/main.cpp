// 教程章节：纹理 + Task 3
// 内容：在一个四边形上同时贴 container.jpg 和 awesomeface.png。
// Task 3：纹理坐标从教程的 0.0f~1.0f 扩大到 0.0f~2.0f，用 --wrap 切换环绕方式：
//   --wrap repeat    GL_REPEAT           平铺重复
//   --wrap mirrored  GL_MIRRORED_REPEAT  镜像重复（箱子四角出现 4 个完整笑脸）
//   --wrap edge      GL_CLAMP_TO_EDGE    边缘拉伸
//   --wrap border    GL_CLAMP_TO_BORDER  边框颜色填充
#include "app.h"
#include "shader.h"

static const char* kVS = R"(#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main() {
    gl_Position = vec4(aPos, 1.0);
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

// 四边形四个角的纹理坐标设为 0~2（Task 3 要求，而非教程的 0~1）
static float kVertices[] = {
    // 位置（NDC）          // 纹理坐标
    -0.5f, -0.5f, 0.0f,     0.0f, 0.0f,
     0.5f, -0.5f, 0.0f,     2.0f, 0.0f,
     0.5f,  0.5f, 0.0f,     2.0f, 2.0f,
    -0.5f,  0.5f, 0.0f,     0.0f, 2.0f,
};
static unsigned int kIndices[] = {0, 1, 2, 0, 2, 3};

static unsigned int loadTexture(const char* path, bool rgba) {
    stbi_set_flip_vertically_on_load(true);  // 图片原点在左上，OpenGL 在左下
    int w, h, channels;
    unsigned char* data = stbi_load(path, &w, &h, &channels, 0);
    if (!data) {
        fprintf(stderr, "加载纹理失败: %s\n", path);
        exit(1);
    }
    unsigned int tex;
    glGenTextures(1, &tex);
    glBindTexture(GL_TEXTURE_2D, tex);
    glTexImage2D(GL_TEXTURE_2D, 0, rgba ? GL_RGBA : GL_RGB, w, h, 0,
                 rgba ? GL_RGBA : GL_RGB, GL_UNSIGNED_BYTE, data);
    glGenerateMipmap(GL_TEXTURE_2D);
    stbi_image_free(data);
    return tex;
}

int main(int argc, char** argv) {
    GLFWwindow* window = glapp::open({"纹理"});

    // Task 3：默认 mirrored（能摆出 4 个笑脸），可切换其它环绕方式
    // --range 1 时使用教程原始的 0.0f~1.0f 坐标，默认 0.0f~2.0f
    int wrap = GL_MIRRORED_REPEAT;
    float texScale = 1.0f;
    const char* wrapName = "GL_MIRRORED_REPEAT";
    for (int i = 1; i < argc; ++i) {
        if (strcmp(argv[i], "--wrap") == 0 && i + 1 < argc) {
            ++i;
            if (strcmp(argv[i], "repeat") == 0) { wrap = GL_REPEAT; wrapName = "GL_REPEAT"; }
            if (strcmp(argv[i], "mirrored") == 0) { wrap = GL_MIRRORED_REPEAT; wrapName = "GL_MIRRORED_REPEAT"; }
            if (strcmp(argv[i], "edge") == 0) { wrap = GL_CLAMP_TO_EDGE; wrapName = "GL_CLAMP_TO_EDGE"; }
            if (strcmp(argv[i], "border") == 0) { wrap = GL_CLAMP_TO_BORDER; wrapName = "GL_CLAMP_TO_BORDER"; }
        }
        if (strcmp(argv[i], "--range") == 0 && i + 1 < argc) {
            texScale = (float)atof(argv[++i]) / 2.0f;
        }
    }
    printf("环绕方式: %s，纹理坐标 0.0f~%.1ff\n", wrapName, 2.0f * texScale);
    for (int v = 0; v < 4; ++v) {
        kVertices[v * 5 + 3] *= texScale;
        kVertices[v * 5 + 4] *= texScale;
    }

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

    // 路径相对于 CMake 构建后的运行目录（仓库 0000/ 目录）
    unsigned int texContainer = loadTexture("assets/container.jpg", false);
    unsigned int texFace = loadTexture("assets/awesomeface.png", true);

    // 环绕方式对两张纹理同时生效；border 模式额外指定黄色边框便于观察
    unsigned int textures[2] = {texContainer, texFace};
    for (unsigned int tex : textures) {
        glBindTexture(GL_TEXTURE_2D, tex);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap);
        if (wrap == GL_CLAMP_TO_BORDER) {
            float yellow[] = {1.0f, 1.0f, 0.0f, 1.0f};
            glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, yellow);
        }
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

        shader.use();
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
