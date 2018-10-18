using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Volumetric Rendering", "Raymarching", "Voronoi")]
    public class RaymarchingVoronoi : CodeFunctionNode
    {
        public RaymarchingVoronoi()
        {
            name = "Raymarching Voronoi";
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
            [Slot(7, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = voronoi_raymarch(Position, Direction, Scale, Treshold, LightDirection, Steps, MinDistance);
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("random_vector", s => s.Append(@"
inline float3 random_vector (float3 position)
{
    float3x3 m = float3x3(15.27, 47.63, 99.41, 89.98, 127.45, 12.84, 64.82, 158.34, 78.45);
    position = frac(sin(mul(position, m)) * 46839.32);
    return position;
}
"));

            registry.ProvideFunction("interpolate", s => s.Append(@"
inline float interpolate(float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}
"));

            registry.ProvideFunction("voronoi_3D", s => s.Append(@"
inline float voronoi_3D(float3 position)
{
    float3 g = floor(position);
    float3 f = frac(position);
    float res = float(8.0);

    for(int x=-1; x<=1; x++)
    {
        for(int y=-1; y<=1; y++)
        {
            for(int z=-1; z<=1; z++)
            {
                float3 lattice = float3(x,y,z);
                float3 offset = random_vector(lattice + g);
                float d = distance(lattice + offset, f);

                if(d < res)
                {
                    res = d;
                }
            }
        }
    }

    return res;
}"));
            registry.ProvideFunction("lambert", s => s.Append(@"
float4 lambert (float3 normal, float3 light_direction) {
	float ndotl = max(dot(normal, light_direction), 0);
	float4 c = float4(ndotl, ndotl, ndotl, 1);
	return c;
}
"));
            registry.ProvideFunction("voronoi_distance", s => s.Append(@"
float voronoi_distance(float3 position, float scale, float treshold)
{
    float value = voronoi_3D(float3(position.x*scale, position.y*scale, position.z*scale));
    return value - treshold;
}"));
            registry.ProvideFunction("voronoi_normal", s => s.Append(@"
float3 voronoi_normal(float3 position, float scale, float treshold)
{
	const float eps = 0.01;

	return normalize
	(	float3
		(	voronoi_distance(position + float3(eps, 0, 0), scale, treshold) - voronoi_distance(position - float3(eps, 0, 0), scale, treshold),
			voronoi_distance(position + float3(0, eps, 0), scale, treshold) - voronoi_distance(position - float3(0, eps, 0), scale, treshold),
			voronoi_distance(position + float3(0, 0, eps), scale, treshold) - voronoi_distance(position - float3(0, 0, eps), scale, treshold)
		)
	);
}
"));
            registry.ProvideFunction("voronoi_render", s => s.Append(@"
float4 voronoi_render(float3 position, float scale, float treshold, float3 light_direction)
{
	float3 normal = voronoi_normal(position, scale, treshold);
	return lambert(normal, light_direction);
}
"));
            registry.ProvideFunction("voronoi_raymarch", s => s.Append(@"
float4 voronoi_raymarch(float3 position, float3 direction, float scale, float treshold, float3 light_direction, int steps, float min_distance)
{
	for(int i = 0; i < steps; i++)
	{
		float distance = voronoi_distance(position, scale, treshold);
		if (distance < min_distance)
            return voronoi_render(position, scale, treshold, light_direction);

		position -= distance * direction * 0.1;
	}
	return float4(1,1,1,0); // White
}
"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
