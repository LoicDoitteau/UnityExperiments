using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Volumetric Rendering", "Raymarching", "Sphere")]
    public class RaymarchingSphere : CodeFunctionNode
    {
        public RaymarchingSphere()
        {
            name = "Raymarching Sphere";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SphereRaymarching", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SphereRaymarching(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.ObjectSpaceViewDirection)] Vector3 Direction,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector3 Center,
            [Slot(3, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector1 Radius,
            [Slot(4, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector3 LightDirection,
            [Slot(5, Binding.None, 100f, 100f, 100f, 100f)] Vector1 Steps,
            [Slot(6, Binding.None, 0.01f, 0.01f, 0.1f, 0.01f)] Vector1 MinDistance,
            [Slot(7, Binding.None)] out Vector4 Out,
            [Slot(8, Binding.None)] out Vector3 RayPosition)
        {
            Out = Vector4.zero;
            RayPosition = Vector3.zero;
            return
                @"
{
    // Out = sphere_raymarch(Position, Direction, Center, Radius, LightDirection, (int)Steps, MinDistance);
    Out = float4(1,1,1,0);
    RayPosition = Position;
    for(int i = 0; i < Steps; i++)
	{
		float distance = sphere_distance(Position, Center, Radius);
		if (distance < MinDistance)
        {
            Out = sphere_render(Position, Center, Radius, LightDirection);
            RayPosition = Position;
            break;
        }

		Position -= distance * Direction;
	}
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("lambert", s => s.Append(@"
float4 lambert (float3 normal, float3 light_direction) {
	float ndotl = max(dot(normal, light_direction), 0);
    // if(ndotl < 0.2) ndotl = 0.2;
    // else if(ndotl < 0.4) ndotl = 0.4;
    // else if(ndotl < 0.6) ndotl = 0.6;
    // else if(ndotl < 0.8) ndotl = 0.8;
    // else ndotl = 1;
	float4 c = float4(ndotl, ndotl, ndotl, 1);
	return c;
}
"));
            registry.ProvideFunction("sphere_distance", s => s.Append(@"
float sphere_distance(float3 position, float3 center, float radius)
{
    // return max(-(distance(position + float3(0.1, 0, 0), center) - radius), distance(position - float3(0.1, 0, 0), center) - radius);
    return distance(position, center) - radius;
}"));
            registry.ProvideFunction("sphere_normal", s => s.Append(@"
float3 sphere_normal(float3 position, float3 center, float radius)
{
	const float eps = 0.01;

	return normalize
	(	float3
		(	sphere_distance(position + float3(eps, 0, 0), center, radius) - sphere_distance(position - float3(eps, 0, 0), center, radius),
			sphere_distance(position + float3(0, eps, 0), center, radius) - sphere_distance(position - float3(0, eps, 0), center, radius),
			sphere_distance(position + float3(0, 0, eps), center, radius) - sphere_distance(position - float3(0, 0, eps), center, radius)
		)
	);
}
"));
            registry.ProvideFunction("sphere_render", s => s.Append(@"
float4 sphere_render(float3 position, float3 center, float radius, float3 light_direction)
{
	float3 normal = sphere_normal(position, center, radius);
	return lambert(normal, light_direction);
}
"));
            registry.ProvideFunction("sphere_raymarch", s => s.Append(@"
float4 sphere_raymarch(float3 position, float3 direction, float3 center, float radius, float3 light_direction, int steps, float min_distance)
{
	for(int i = 0; i < steps; i++)
	{
		float distance = sphere_distance(position, center, radius);
		if (distance < min_distance)
            return sphere_render(position, center, radius, light_direction);

		position -= distance * direction;
	}
	return float4(1,1,1,0); // White
}
"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
