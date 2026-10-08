// 摄像机（对应教程“摄像机”章节）：欧拉角 + 滚轮缩放，供 07_camera 使用。
#pragma once

#include <glad/gl.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>

#include <vector>

class Camera {
public:
    glm::vec3 position;
    glm::vec3 front{0.0f, 0.0f, -1.0f};
    glm::vec3 up{0.0f, 1.0f, 0.0f};
    glm::vec3 worldUp{0.0f, 1.0f, 0.0f};

    float yaw = -90.0f;    // 偏航角，-90 朝向 -Z
    float pitch = 0.0f;    // 俯仰角
    float speed = 2.5f;    // 移动速度（单位/秒）
    float sensitivity = 0.1f;
    float zoom = 45.0f;    // 视野 FoV

    Camera(glm::vec3 pos = glm::vec3(0.0f, 0.0f, 3.0f)) : position(pos) { updateVectors(); }

    glm::mat4 getViewMatrix() const {
        return glm::lookAt(position, position + front, up);
    }

    // WASD 平移；deltaTime 保证不同帧率下速度一致
    void processKeyboard(int direction, float deltaTime) {
        float v = speed * deltaTime;
        glm::vec3 right = glm::normalize(glm::cross(front, up));
        switch (direction) {
            case GLFW_KEY_W: position += v * front; break;
            case GLFW_KEY_S: position -= v * front; break;
            case GLFW_KEY_A: position -= v * right; break;
            case GLFW_KEY_D: position += v * right; break;
        }
    }

    // 鼠标移动 -> 更新偏航 / 俯仰
    void processMouse(float xoffset, float yoffset) {
        yaw += xoffset * sensitivity;
        pitch += yoffset * sensitivity;
        if (pitch > 89.0f) pitch = 89.0f;
        if (pitch < -89.0f) pitch = -89.0f;
        updateVectors();
    }

    // 滚轮 -> 缩放（改变 FoV）
    void processScroll(float yoffset) {
        zoom -= yoffset;
        if (zoom < 1.0f) zoom = 1.0f;
        if (zoom > 45.0f) zoom = 45.0f;
    }

private:
    void updateVectors() {
        glm::vec3 f;
        f.x = cos(glm::radians(yaw)) * cos(glm::radians(pitch));
        f.y = sin(glm::radians(pitch));
        f.z = sin(glm::radians(yaw)) * cos(glm::radians(pitch));
        front = glm::normalize(f);
        up = glm::normalize(glm::cross(glm::normalize(glm::cross(front, worldUp)), front));
    }
};
