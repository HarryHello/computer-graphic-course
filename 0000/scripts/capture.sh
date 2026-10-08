#!/bin/bash
# 批量生成 REPORT.md 所需的全部运行截图（窗口标题 = 学号 - 章节名）。
# 用法：cd 0000 && cmake -B build && cmake --build build && ./scripts/capture.sh
set -euo pipefail
cd "$(dirname "$0")/.."  # 0000/

shot() {  # shot <输出文件名> <程序名> [参数...] —— 帧缓冲截图（不含标题栏）
    echo "--- $1"
    SCREENSHOT="$PWD/report/images/$1" "build/$2" "${@:3}" >/dev/null
}

# —— Task 5：按教程顺序的基础运行结果 ——
shot 01_hello_window.png        01_hello_window
shot 02_hello_triangle.png      02_hello_triangle
shot 02_triangle_wireframe.png  02_hello_triangle --wireframe
shot 03_shaders.png             03_shaders
shot 04_textures.png            04_textures --range 1 --wrap repeat
shot 05_transformations.png     05_transformations
shot 06_coordinate_systems.png  06_coordinate_systems
shot 07_camera.png              07_camera

# —— Task 2：三角形上下颠倒 ——
shot task2_normal.png   03_shaders
shot task2_flipped.png  03_shaders --flip

# —— Task 3：四种环绕方式（纹理坐标 0.0f~2.0f）——
shot task3_wrap_repeat.png    04_textures --wrap repeat
shot task3_wrap_mirrored.png  04_textures --wrap mirrored
shot task3_wrap_edge.png      04_textures --wrap edge
shot task3_wrap_border.png    04_textures --wrap border

# —— Task 4：FoV / aspect-ratio 参数实验 ——
shot task4_fov_15.png     06_coordinate_systems --fov 15
shot task4_fov_90.png     06_coordinate_systems --fov 90
shot task4_fov_120.png    06_coordinate_systems --fov 120
shot task4_aspect_05.png  06_coordinate_systems --aspect 0.5
shot task4_aspect_20.png  06_coordinate_systems --aspect 2.0

# —— Task 5：带标题栏的整窗截图（标题 = 学号 - 章节名，macOS only）——
if command -v clang >/dev/null 2>&1 && [[ "$(uname)" == Darwin ]]; then
    HELPER=/tmp/cgwindowid
    clang -x c -framework CoreGraphics -framework CoreFoundation -o "$HELPER" - << 'EOF' 2>/dev/null || true
#include <CoreGraphics/CoreGraphics.h>
#include <CoreFoundation/CoreFoundation.h>
#include <stdio.h>
int main(void) {
    CFArrayRef list = CGWindowListCopyWindowInfo(kCGWindowListOptionOnScreenOnly, kCGNullWindowID);
    for (CFIndex i = 0; i < CFArrayGetCount(list); ++i) {
        CFDictionaryRef info = CFArrayGetValueAtIndex(list, i);
        CFNumberRef pidRef = CFDictionaryGetValue(info, kCGWindowOwnerPID);
        CFNumberRef idRef = CFDictionaryGetValue(info, kCGWindowNumber);
        int pid = 0, wid = 0;
        if (pidRef) CFNumberGetValue(pidRef, kCFNumberIntType, &pid);
        if (idRef) CFNumberGetValue(idRef, kCFNumberIntType, &wid);
        printf("%d %d\n", pid, wid);
    }
    return 0;
}
EOF
    if [[ ! -x "$HELPER" ]]; then
        echo "（窗口 ID 辅助工具编译失败，跳过带标题栏的截图）"
        exit 0
    fi
    title_shot() {  # title_shot <输出文件名> <程序名> [参数...]
        echo "--- $1（含标题栏）"
        "build/$2" "${@:3}" >/dev/null 2>&1 &
        local pid=$!
        sleep 2
        local wid
        wid=$("$HELPER" | awk -v p="$pid" '$1==p {print $2; exit}')
        screencapture -o -x -l "$wid" "$PWD/report/images/$1" 2>/dev/null || true
        kill "$pid" 2>/dev/null || true
        wait "$pid" 2>/dev/null || true
    }
    title_shot title_01_hello_window.png        01_hello_window
    title_shot title_02_hello_triangle.png      02_hello_triangle
    title_shot title_03_shaders.png             03_shaders
    title_shot title_04_textures.png            04_textures --range 1 --wrap repeat
    title_shot title_05_transformations.png     05_transformations
    title_shot title_06_coordinate_systems.png  06_coordinate_systems
    title_shot title_07_camera.png              07_camera
fi

echo "全部截图已保存到 report/images/"
