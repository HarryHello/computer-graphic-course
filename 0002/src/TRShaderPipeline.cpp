#include "TRShaderPipeline.h"

#include <algorithm>
#include <cstdint>
#include <cstdlib>

namespace TinyRenderer
{
	//----------------------------------------------VertexData----------------------------------------------

	TRShaderPipeline::VertexData TRShaderPipeline::VertexData::lerp(
		const TRShaderPipeline::VertexData &v0,
		const TRShaderPipeline::VertexData &v1,
		float frac)
	{
		//Linear interpolation
		VertexData result;
		result.pos = (1.0f - frac) * v0.pos + frac * v1.pos;
		result.col = (1.0f - frac) * v0.col + frac * v1.col;
		result.nor = (1.0f - frac) * v0.nor + frac * v1.nor;
		result.tex = (1.0f - frac) * v0.tex + frac * v1.tex;
		result.cpos = (1.0f - frac) * v0.cpos + frac * v1.cpos;
		result.spos.x = (1.0f - frac) * v0.spos.x + frac * v1.spos.x;
		result.spos.y = (1.0f - frac) * v0.spos.y + frac * v1.spos.y;

		return result;
	}

	TRShaderPipeline::VertexData TRShaderPipeline::VertexData::barycentricLerp(
		const VertexData &v0,
		const VertexData &v1,
		const VertexData &v2,
		glm::vec3 w)
	{
		VertexData result;
		result.pos = w.x * v0.pos + w.y * v1.pos + w.z * v2.pos;
		result.col = w.x * v0.col + w.y * v1.col + w.z * v2.col;
		result.nor = w.x * v0.nor + w.y * v1.nor + w.z * v2.nor;
		result.tex = w.x * v0.tex + w.y * v1.tex + w.z * v2.tex;
		result.cpos = w.x * v0.cpos + w.y * v1.cpos + w.z * v2.cpos;
		result.spos.x = w.x * v0.spos.x + w.y * v1.spos.x + w.z * v2.spos.x;
		result.spos.y = w.x * v0.spos.y + w.y * v1.spos.y + w.z * v2.spos.y;

		return result;
	}

	void TRShaderPipeline::VertexData::prePerspCorrection(VertexData &v)
	{
		//Perspective correction: the world space properties should be multipy by 1/w before rasterization
		//https://zhuanlan.zhihu.com/p/144331875
		//We use pos.w to store 1/w
		v.pos.w = 1.0f / v.cpos.w;
		v.pos = glm::vec4(v.pos.x * v.pos.w, v.pos.y * v.pos.w, v.pos.z * v.pos.w, v.pos.w);
		v.tex = v.tex * v.pos.w;
		v.nor = v.nor * v.pos.w;
		v.col = v.col * v.pos.w;
	}

	void TRShaderPipeline::VertexData::aftPrespCorrection(VertexData &v)
	{
		//Perspective correction: the world space properties should be multipy by w after rasterization
		//https://zhuanlan.zhihu.com/p/144331875
		//We use pos.w to store 1/w
		float w = 1.0f / v.pos.w;
		v.pos = v.pos * w;
		v.tex = v.tex * w;
		v.nor = v.nor * w;
		v.col = v.col * w;
	}

	//----------------------------------------------TRShaderPipeline----------------------------------------------

	void TRShaderPipeline::rasterize_wire(
		const VertexData &v0,
		const VertexData &v1,
		const VertexData &v2,
		const unsigned int &screen_width,
		const unsigned int &screene_height,
		std::vector<VertexData> &rasterized_points)
	{
		//Draw each line step by step
		rasterize_wire_aux(v0, v1, screen_width, screene_height, rasterized_points);
		rasterize_wire_aux(v1, v2, screen_width, screene_height, rasterized_points);
		rasterize_wire_aux(v0, v2, screen_width, screene_height, rasterized_points);
	}

	void TRShaderPipeline::rasterize_fill_edge_function(
		const VertexData &v0,
		const VertexData &v1,
		const VertexData &v2,
		const unsigned int &screen_width,
		const unsigned int &screene_height,
		std::vector<VertexData> &rasterized_points)
	{
		if (screen_width == 0 || screene_height == 0)
			return;

		const auto edge = [](glm::ivec2 a, glm::ivec2 b, glm::ivec2 p) {
			return (std::int64_t(b.x) - a.x) * (std::int64_t(p.y) - a.y)
			     - (std::int64_t(b.y) - a.y) * (std::int64_t(p.x) - a.x);
		};
		const auto topLeft = [](glm::ivec2 a, glm::ivec2 b) {
			return b.y < a.y || (b.y == a.y && b.x > a.x);
		};

		const VertexData* b = &v1;
		const VertexData* c = &v2;
		auto area = edge(v0.spos, b->spos, c->spos);
		if (area == 0)
			return;
		if (area < 0)
		{
			std::swap(b, c);
			area = -area;
		}

		const glm::ivec2 aPos = v0.spos, bPos = b->spos, cPos = c->spos;
		const int minX = std::max(0, std::min({aPos.x, bPos.x, cPos.x}));
		const int maxX = std::min(int(screen_width) - 1, std::max({aPos.x, bPos.x, cPos.x}));
		const int minY = std::max(0, std::min({aPos.y, bPos.y, cPos.y}));
		const int maxY = std::min(int(screene_height) - 1, std::max({aPos.y, bPos.y, cPos.y}));
		const bool includeA = topLeft(bPos, cPos);
		const bool includeB = topLeft(cPos, aPos);
		const bool includeC = topLeft(aPos, bPos);
		const float invArea = 1.0f / area;

		for (int y = minY; y <= maxY; ++y)
		{
			for (int x = minX; x <= maxX; ++x)
			{
				const glm::ivec2 pixel(x, y);
				const auto wa = edge(bPos, cPos, pixel);
				const auto wb = edge(cPos, aPos, pixel);
				const auto wc = edge(aPos, bPos, pixel);
				if (wa < 0 || (wa == 0 && !includeA) ||
				    wb < 0 || (wb == 0 && !includeB) ||
				    wc < 0 || (wc == 0 && !includeC))
					continue;

				auto point = VertexData::barycentricLerp(v0, *b, *c,
					glm::vec3(wa * invArea, wb * invArea, wc * invArea));
				point.spos = pixel;
				rasterized_points.push_back(point);
			}
		}
	}

	void TRShaderPipeline::rasterize_wire_aux(
		const VertexData &from,
		const VertexData &to,
		const unsigned int &screen_width,
		const unsigned int &screen_height,
		std::vector<VertexData> &rasterized_points)
	{

		glm::ivec2 pixel = from.spos;
		const int dx = std::abs(to.spos.x - pixel.x);
		const int dy = std::abs(to.spos.y - pixel.y);
		const int stepX = pixel.x < to.spos.x ? 1 : -1;
		const int stepY = pixel.y < to.spos.y ? 1 : -1;
		const int steps = std::max(dx, dy);
		int error = dx - dy;
		for (int step = 0; ; ++step)
		{
			if (pixel.x >= 0 && pixel.y >= 0 &&
			    unsigned(pixel.x) < screen_width && unsigned(pixel.y) < screen_height)
			{
				const float weight = steps == 0 ? 0.0f : float(step) / steps;
				auto point = VertexData::lerp(from, to, weight);
				point.spos = pixel;
				rasterized_points.push_back(point);
			}
			if (pixel == to.spos)
				break;

			const int twiceError = 2 * error;
			if (twiceError > -dy)
			{
				error -= dy;
				pixel.x += stepX;
			}
			if (twiceError < dx)
			{
				error += dx;
				pixel.y += stepY;
			}
		}
	}

	void TRDefaultShaderPipeline::vertexShader(VertexData &vertex)
	{
		//Local space -> World space -> Camera space -> Project space
		vertex.pos = m_model_matrix * glm::vec4(vertex.pos.x, vertex.pos.y, vertex.pos.z, 1.0f);
		vertex.cpos = m_view_project_matrix * vertex.pos;
	}

	void TRDefaultShaderPipeline::fragmentShader(const VertexData &data, glm::vec4 &fragColor)
	{
		//Just return the color.
		fragColor = glm::vec4(data.tex, 0.0, 1.0f);
	}
}
