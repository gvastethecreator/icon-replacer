cmake_minimum_required(VERSION 3.24)

project(IconReplacerShellExtension LANGUAGES CXX)

add_library(IconReplacer.ShellExtension SHARED
    IconReplacer.ShellExtension.cpp
    IconReplacer.ShellExtension.def)

target_compile_features(IconReplacer.ShellExtension PRIVATE cxx_std_20)

target_compile_definitions(IconReplacer.ShellExtension PRIVATE
    UNICODE
    _UNICODE
    WIN32_LEAN_AND_MEAN
    NOMINMAX)

target_compile_options(IconReplacer.ShellExtension PRIVATE
    /W4
    /permissive-
    /EHsc)

target_link_libraries(IconReplacer.ShellExtension PRIVATE
    advapi32
    msimg32
    ole32
    shell32
    windowscodecs)

set_target_properties(IconReplacer.ShellExtension PROPERTIES
    OUTPUT_NAME "IconReplacer.ShellExtension"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out")

add_executable(IconReplacer.ShellExtension.Smoke
    "${CMAKE_CURRENT_SOURCE_DIR}/../../tests/native/ShellExtensionSmoke.cpp")

target_compile_features(IconReplacer.ShellExtension.Smoke PRIVATE cxx_std_20)

target_compile_definitions(IconReplacer.ShellExtension.Smoke PRIVATE
    UNICODE
    _UNICODE
    WIN32_LEAN_AND_MEAN
    NOMINMAX)

target_compile_options(IconReplacer.ShellExtension.Smoke PRIVATE
    /W4
    /permissive-
    /EHsc)

target_link_libraries(IconReplacer.ShellExtension.Smoke PRIVATE
    ole32
    shell32)

set_target_properties(IconReplacer.ShellExtension.Smoke PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out")
