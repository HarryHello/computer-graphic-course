#include "Triangle.hpp"

void Triangle::setVertex(int index, glm::vec3 position)
{
    v[index] = position;
}

void Triangle::setColor(int index, glm::vec3 rgb)
{
    color[index] = rgb;
}
