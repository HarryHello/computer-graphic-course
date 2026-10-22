// Regression check for the four assignment matrices: compare them element-wise against
// glm's reference implementations. glm::lookAt / glm::perspective / glm::ortho are allowed
// HERE as the reference; the renderer itself must not call them.
//
// Build & run (from 0001/, macOS with Homebrew SDL2). The Homebrew include dir comes first
// so that SDL2/SDL.h resolves to the modern headers instead of the bundled old Windows ones:
//   clang++ -std=c++11 -O2 -I/opt/homebrew/include -Iinclude tests/matrix_check.cpp \
//     src/TRRenderer.cpp src/TRFrameBuffer.cpp src/TRDrawableMesh.cpp src/TRShaderPipeline.cpp src/TRWindowsApp.cpp \
//     -L/opt/homebrew/lib -lSDL2 -o /tmp/matrix_check && /tmp/matrix_check

#include <cstdio>
#include <cmath>

#include "TinyRenderer/TRRenderer.h"

#include "glm/gtc/matrix_transform.hpp"

using namespace TinyRenderer;

static bool same(const glm::mat4 &a, const glm::mat4 &b, float eps = 1e-5f)
{
	for (int c = 0; c < 4; ++c)
	{
		for (int r = 0; r < 4; ++r)
		{
			if (std::fabs(a[c][r] - b[c][r]) > eps)
			{
				std::printf("  mismatch at [col %d][row %d]: %g vs %g\n", c, r, a[c][r], b[c][r]);
				return false;
			}
		}
	}
	return true;
}

int main()
{
	int failures = 0;

	glm::vec3 camera(0.5f, -1.2f, 3.7f);
	glm::vec3 target(0.1f, 0.2f, 0.0f);
	glm::vec3 up(0.0f, 1.0f, 0.0f);

	std::printf("calcViewMatrix vs glm::lookAt:\n");
	failures += !same(TRRenderer::calcViewMatrix(camera, target, up),
		glm::lookAt(camera, target, up));

	std::printf("calcPerspProjectMatrix vs glm::perspective:\n");
	failures += !same(TRRenderer::calcPerspProjectMatrix(45.0f, 666.0f / 500.0f, 0.1f, 10.0f),
		glm::perspective(glm::radians(45.0f), 666.0f / 500.0f, 0.1f, 10.0f));

	std::printf("calcOrthoProjectMatrix vs glm::ortho:\n");
	failures += !same(TRRenderer::calcOrthoProjectMatrix(-2.0f, 3.0f, -1.0f, 4.0f, 0.5f, 20.0f),
		glm::ortho(-2.0f, 3.0f, -1.0f, 4.0f, 0.5f, 20.0f));

	//Viewport: scale by half size and translate to the center, y flipped (row 0 is the window top)
	glm::mat4 viewportRef(
		333.0f, 0.0f, 0.0f, 0.0f,
		0.0f, -250.0f, 0.0f, 0.0f,
		0.0f, 0.0f, 1.0f, 0.0f,
		333.0f, 250.0f, 0.0f, 1.0f);
	std::printf("calcViewPortMatrix vs the hand-written reference:\n");
	failures += !same(TRRenderer::calcViewPortMatrix(666, 500), viewportRef);

	if (failures == 0)
	{
		std::printf("all matrix checks passed\n");
		return 0;
	}
	std::printf("%d matrix check(s) failed\n", failures);
	return 1;
}
