#pragma once

#include "Triangle.hpp"

#include <glm/glm.hpp>

#include <limits>
#include <map>
#include <vector>

namespace rst
{
    enum class Buffers
    {
        Color = 1,
        Depth = 2
    };

    inline Buffers operator|(Buffers a, Buffers b)
    {
        return Buffers(int(a) | int(b));
    }

    inline Buffers operator&(Buffers a, Buffers b)
    {
        return Buffers(int(a) & int(b));
    }

    enum class Primitive { Line, Triangle };

    struct pos_buf_id { int pos_id; };
    struct ind_buf_id { int ind_id; };
    struct col_buf_id { int col_id; };

    class rasterizer
    {
    public:
        rasterizer(int w, int h, bool ssaa = false);
        pos_buf_id load_positions(const std::vector<glm::vec3>& positions);
        ind_buf_id load_indices(const std::vector<glm::ivec3>& indices);
        col_buf_id load_colors(const std::vector<glm::vec3>& colors);

        void set_model(const glm::mat4& m);
        void set_view(const glm::mat4& v);
        void set_projection(const glm::mat4& p);
        void clear(Buffers buff);
        void draw(pos_buf_id pos_buffer, ind_buf_id ind_buffer, col_buf_id col_buffer, Primitive type);

        const std::vector<glm::vec3>& frame_buffer() const { return frame_buf; }

    private:
        void rasterize_triangle(const Triangle& t);

        struct Sample
        {
            glm::vec3 color{0.0f};
            float depth = std::numeric_limits<float>::infinity();
        };

        glm::mat4 model{1.0f};
        glm::mat4 view{1.0f};
        glm::mat4 projection{1.0f};

        std::map<int, std::vector<glm::vec3>> pos_buf;
        std::map<int, std::vector<glm::ivec3>> ind_buf;
        std::map<int, std::vector<glm::vec3>> col_buf;
        int next_id = 0;

        int width, height;
        int samples_per_axis, sample_count;
        std::vector<glm::vec3> frame_buf;
        std::vector<Sample> samples;
    };
}
