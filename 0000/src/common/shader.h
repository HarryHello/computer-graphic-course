// 极简着色器工具：从字符串编译 / 链接着色器程序，并设置常用 uniform。
// 着色器源码直接内嵌在各章节的 main.cpp 中，避免运行期依赖文件路径。
#pragma once

#include <glad/gl.h>
#include <glm/gtc/type_ptr.hpp>
#include <glm/mat4x4.hpp>

#include <cstdio>

class Shader {
public:
    unsigned int id = 0;

    Shader() = default;
    Shader(const char* vsSource, const char* fsSource) { build(vsSource, fsSource); }

    void build(const char* vsSource, const char* fsSource) {
        unsigned int vs = compile(GL_VERTEX_SHADER, vsSource);
        unsigned int fs = compile(GL_FRAGMENT_SHADER, fsSource);
        id = glCreateProgram();
        glAttachShader(id, vs);
        glAttachShader(id, fs);
        glLinkProgram(id);
        checkLink();
        glDeleteShader(vs);
        glDeleteShader(fs);
    }

    void use() const { glUseProgram(id); }

    void setInt(const char* name, int value) const {
        glUniform1i(glGetUniformLocation(id, name), value);
    }
    void setFloat(const char* name, float value) const {
        glUniform1f(glGetUniformLocation(id, name), value);
    }
    void setVec3(const char* name, float x, float y, float z) const {
        glUniform3f(glGetUniformLocation(id, name), x, y, z);
    }
    void setMat4(const char* name, const glm::mat4& m) const {
        glUniformMatrix4fv(glGetUniformLocation(id, name), 1, GL_FALSE, glm::value_ptr(m));
    }

private:
    static unsigned int compile(unsigned int type, const char* source) {
        unsigned int shader = glCreateShader(type);
        glShaderSource(shader, 1, &source, nullptr);
        glCompileShader(shader);
        int ok = 0;
        glGetShaderiv(shader, GL_COMPILE_STATUS, &ok);
        if (!ok) {
            char log[1024];
            glGetShaderInfoLog(shader, sizeof(log), nullptr, log);
            fprintf(stderr, "着色器编译失败:\n%s\n", log);
        }
        return shader;
    }

    void checkLink() const {
        int ok = 0;
        glGetProgramiv(id, GL_LINK_STATUS, &ok);
        if (!ok) {
            char log[1024];
            glGetProgramInfoLog(id, sizeof(log), nullptr, log);
            fprintf(stderr, "着色器程序链接失败:\n%s\n", log);
        }
    }
};
