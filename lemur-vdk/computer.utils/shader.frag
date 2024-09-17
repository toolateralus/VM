#version 450 core
in vec2 UV;
in vec3 Normal;
in vec4 FragPos;

out vec4 FragColor;

uniform float time;
uniform vec4 color;

void main() {
   FragColor = color;
}