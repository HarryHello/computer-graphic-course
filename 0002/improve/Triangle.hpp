#pragma once

#include <glm/vec3.hpp>

class Triangle
{
public:
    glm::vec3 v[3];
    void setVertex(int index, glm::vec3 position);
    void setColor(int index, glm::vec3 rgb);
    glm::vec3 getColor() const { return color[0]; }

private:
    glm::vec3 color[3];
};
