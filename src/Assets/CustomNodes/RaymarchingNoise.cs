using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Volumetric Rendering", "Raymarching", "Noise")]
    public class RaymarchingNoise : CodeFunctionNode
    {
        public RaymarchingNoise()
        {
            name = "Raymarching Noise";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Raymarching", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Raymarching(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.ObjectSpaceViewDirection)] Vector3 Direction,
            [Slot(2, Binding.None, 500f, 500f, 500f, 500f)] Vector1 Scale,
            [Slot(3, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector1 Treshold,
            [Slot(4, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector3 LightDirection,
            [Slot(5, Binding.None, 100f, 100f, 100f, 100f)] Vector1 Steps,
            [Slot(6, Binding.None, 0.01f, 0.01f, 0.01f, 0.01f)] Vector1 MinDistance,
            [Slot(7, Binding.None)] out Vector4 Out,
            [Slot(8, Binding.None)] out Vector3 RayPosition)
        {
            Out = Vector4.zero;
            RayPosition = Vector3.zero;
            return
                @"
{
    // Out = noise_raymarch(Position, Direction, Scale, Treshold, LightDirection, Steps, MinDistance);
    Out = float4(1,1,1,0);
    RayPosition = Position;
    for(int i = 0; i < Steps; i++)
	{
		float distance = noise_distance(Position, Scale, Treshold);
		if (distance < MinDistance)
        {
            Out = noise_render(Position, Scale, Treshold, LightDirection);
            RayPosition = Position;
            break;
        }
		Position -= 0.1 * Direction;
	}
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("random", s => s.Append(@"
inline float random(float3 position)
{
    return frac(sin(dot(position, float3(12.9898, 78.233, 125.67)))*43758.5453);
}"));

            registry.ProvideFunction("interpolate", s => s.Append(@"
inline float interpolate(float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}
"));

            registry.ProvideFunction("value_noise_3D", s => s.Append(@"
inline float value_noise_3D(float3 position)
{
    float3 i = floor(position);
    float3 f = frac(position);
    f = f * f * (3.0 - 2.0 * f);

    position = abs(frac(position) - 0.5);
    float3 c000 = i + float3(0.0, 0.0, 0.0);
    float3 c001 = i + float3(0.0, 0.0, 1.0);
    float3 c010 = i + float3(0.0, 1.0, 0.0);
    float3 c011 = i + float3(0.0, 1.0, 1.0);
    float3 c100 = i + float3(1.0, 0.0, 0.0);
    float3 c101 = i + float3(1.0, 0.0, 1.0);
    float3 c110 = i + float3(1.0, 1.0, 0.0);
    float3 c111 = i + float3(1.0, 1.0, 1.0);
    float r000 = random(c000);
    float r001 = random(c001);
    float r010 = random(c010);
    float r011 = random(c011);
    float r100 = random(c100);
    float r101 = random(c101);
    float r110 = random(c110);
    float r111 = random(c111);

    float bottomFront = interpolate(r000, r100, f.x);
    float topFront = interpolate(r010, r110, f.x);
    float bottomBack = interpolate(r001, r101, f.x);
    float topBack = interpolate(r011, r111, f.x);
    float front = interpolate(bottomFront, topFront, f.y);
    float back = interpolate(bottomBack, topBack, f.y);
    float t = interpolate(front, back, f.z);
    return t;
}"));
            registry.ProvideFunction("lambert", s => s.Append(@"
float4 lambert (float3 normal, float3 light_direction) {
	float ndotl = max(dot(normal, light_direction), 0);
	float4 c = float4(ndotl, ndotl, ndotl, 1);
	return c;
}
"));
            registry.ProvideFunction("noise_distance", s => s.Append(@"
float noise_distance(float3 position, float scale, float treshold)
{
    float value = 0.0;
    for(int i = 0; i < 3; i++)
    {
        float freq = pow(2.0, float(i));
        float amp = pow(0.5, float(3-i));
        value += value_noise_3D(float3(position.x*scale/freq, position.y*scale/freq, position.z*scale/freq))*amp;
    }
    return value - treshold;
}"));
            registry.ProvideFunction("noise_normal", s => s.Append(@"
float3 noise_normal(float3 position, float scale, float treshold)
{
	const float eps = 0.01;

	return normalize
	(	float3
		(	noise_distance(position + float3(eps, 0, 0), scale, treshold) - noise_distance(position - float3(eps, 0, 0), scale, treshold),
			noise_distance(position + float3(0, eps, 0), scale, treshold) - noise_distance(position - float3(0, eps, 0), scale, treshold),
			noise_distance(position + float3(0, 0, eps), scale, treshold) - noise_distance(position - float3(0, 0, eps), scale, treshold)
		)
	);
}
"));
            registry.ProvideFunction("noise_render", s => s.Append(@"
float4 noise_render(float3 position, float scale, float treshold, float3 light_direction)
{
	float3 normal = noise_normal(position, scale, treshold);
	return lambert(normal, light_direction);
}
"));
            registry.ProvideFunction("noise_raymarch", s => s.Append(@"
float4 noise_raymarch(float3 position, float3 direction, float scale, float treshold, float3 light_direction, int steps, float min_distance)
{
	for(int i = 0; i < steps; i++)
	{
		float distance = noise_distance(position, scale, treshold);
		if (distance < min_distance)
            return noise_render(position, scale, treshold, light_direction);

		position -= 0.1 * direction;
	}
	return float4(1,1,1,0); // White
}
"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
