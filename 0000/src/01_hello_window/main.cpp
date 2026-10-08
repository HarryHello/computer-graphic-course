// 教程章节：创建窗口 + 你好，窗口
// 内容：初始化 GLFW，创建 OpenGL 3.3 Core 窗口，用纯色清屏。
#include "app.h"

int main() {
    GLFWwindow* window = glapp::open({"你好，窗口"});

    while (!glfwWindowShouldClose(window)) {
        if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
            glfwSetWindowShouldClose(window, GLFW_TRUE);

        // 清屏颜色：教程中的深青绿色
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        glapp::capture(window);
        glfwSwapBuffers(window);
        glfwPollEvents();
    }
    glapp::close(window);
    return 0;
}
