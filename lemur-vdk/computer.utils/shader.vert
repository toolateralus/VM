#version 450 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aUV;
layout(location = 2) in vec3 aNormal;


out vec2 UV;
out vec3 Normal;
out vec4 FragPos;

uniform float time;
uniform mat4 viewProjectionMatrix;
uniform mat4 modelMatrix;

void main() {
  gl_Position = viewProjectionMatrix * modelMatrix * vec4(aPos, 1.0);
  FragPos = gl_Position;
  UV = aUV;
  Normal = aNormal;
}   