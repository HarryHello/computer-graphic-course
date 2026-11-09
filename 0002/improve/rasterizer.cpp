#include "rasterizer.hpp"

#include <algorithm>
#include <cmath>

rst::pos_buf_id rst::rasterizer::load_positions(const std::vector<glm::vec3>& positions)
{
    auto id = next_id++;
    pos_buf.emplace(id, positions);
    return {id};
}

rst::ind_buf_id rst::rasterizer::load_indices(const std::vector<glm::ivec3>& indices)
{
    auto id = next_id++;
    ind_buf.emplace(id, indices);
    return {id};
}

rst::col_buf_id rst::rasterizer::load_colors(const std::vector<glm::vec3>& colors)
{
    auto id = next_id++;
    col_buf.emplace(id, colors);
    return {id};
}

void rst::rasterizer::draw(pos_buf_id pos_buffer, ind_buf_id ind_buffer, col_buf_id col_buffer, Primitive)
{
    const auto& positions = pos_buf.at(pos_buffer.pos_id);
    const auto& indices = ind_buf.at(ind_buffer.ind_id);
    const auto& colors = col_buf.at(col_buffer.col_id);
    const glm::mat4 mvp = projection * view * model;

    for (const auto& face : indices)
    {
        glm::vec4 clip[3] = {
            mvp * glm::vec4(positions[face[0]], 1.0f),
            mvp * glm::vec4(positions[face[1]], 1.0f),
            mvp * glm::vec4(positions[face[2]], 1.0f)
        };
        if (clip[0].w <= 0.0f || clip[1].w <= 0.0f || clip[2].w <= 0.0f)
            continue;

        Triangle t;
        for (int i = 0; i < 3; ++i)
        {
            const glm::vec3 ndc = glm::vec3(clip[i]) / clip[i].w;
            t.setVertex(i, {
                (ndc.x + 1.0f) * 0.5f * width,
                (1.0f - ndc.y) * 0.5f * height,
                (ndc.z + 1.0f) * 0.5f
            });
            t.setColor(i, colors[face[i]]);
        }
        rasterize_triangle(t);
    }

    for (std::size_t pixel = 0; pixel < frame_buf.size(); ++pixel)
    {
        glm::vec3 sum(0.0f);
        for (int s = 0; s < sample_count; ++s)
            sum += samples[pixel * sample_count + s].color;
        frame_buf[pixel] = sum / float(sample_count);
    }
}

void rst::rasterizer::rasterize_triangle(const Triangle& t)
{
    const auto edge = [](glm::dvec2 a, glm::dvec2 b, glm::dvec2 p) {
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    };
    const auto topLeft = [](glm::dvec2 a, glm::dvec2 b) {
        return b.y < a.y || (b.y == a.y && b.x > a.x);
    };

    const glm::dvec3 a(t.v[0]);
    glm::dvec3 b(t.v[1]), c(t.v[2]);
    double area = edge(a, b, c);
    if (area == 0.0)
        return;
    if (area < 0.0)
    {
        std::swap(b, c);
        area = -area;
    }

    const int minX = int(std::clamp(std::floor(std::min({a.x, b.x, c.x})), 0.0, double(width)));
    const int maxX = int(std::clamp(std::ceil(std::max({a.x, b.x, c.x})), 0.0, double(width)));
    const int minY = int(std::clamp(std::floor(std::min({a.y, b.y, c.y})), 0.0, double(height)));
    const int maxY = int(std::clamp(std::ceil(std::max({a.y, b.y, c.y})), 0.0, double(height)));

    const bool includeA = topLeft(b, c);
    const bool includeB = topLeft(c, a);
    const bool includeC = topLeft(a, b);
    const glm::vec3 color = t.getColor();

    for (int y = minY; y < maxY; ++y)
    {
        for (int x = minX; x < maxX; ++x)
        {
            auto pixel = (std::size_t(y) * width + x) * sample_count;
            for (int s = 0; s < sample_count; ++s)
            {
                const glm::dvec2 p(
                    x + (s % samples_per_axis + 0.5) / samples_per_axis,
                    y + (s / samples_per_axis + 0.5) / samples_per_axis);
                const double wa = edge(b, c, p);
                const double wb = edge(c, a, p);
                const double wc = edge(a, b, p);
                if (wa < 0 || (wa == 0 && !includeA) ||
                    wb < 0 || (wb == 0 && !includeB) ||
                    wc < 0 || (wc == 0 && !includeC))
                    continue;

                // Projected depth is affine in screen space.
                const float depth = float((wa * a.z + wb * b.z + wc * c.z) / area);
                Sample& sample = samples[pixel + s];
                if (depth >= 0.0f && depth <= 1.0f && depth < sample.depth)
                    sample = {color, depth};
            }
        }
    }
}

void rst::rasterizer::set_model(const glm::mat4& m)
{
    model = m;
}

void rst::rasterizer::set_view(const glm::mat4& v)
{
    view = v;
}

void rst::rasterizer::set_projection(const glm::mat4& p)
{
    projection = p;
}

void rst::rasterizer::clear(Buffers buff)
{
    if ((buff & Buffers::Color) == Buffers::Color)
    {
        std::fill(frame_buf.begin(), frame_buf.end(), glm::vec3(0.0f));
        for (auto& sample : samples)
            sample.color = glm::vec3(0.0f);
    }
    if ((buff & Buffers::Depth) == Buffers::Depth)
    {
        for (auto& sample : samples)
            sample.depth = std::numeric_limits<float>::infinity();
    }
}

rst::rasterizer::rasterizer(int w, int h, bool ssaa)
    : width(w), height(h), samples_per_axis(ssaa ? 2 : 1),
      sample_count(samples_per_axis * samples_per_axis),
      frame_buf(std::size_t(w) * h, glm::vec3(0.0f)),
      samples(frame_buf.size() * sample_count)
{
}
