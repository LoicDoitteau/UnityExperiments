using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Volumetric Rendering", "Raymarching", "Box")]
    public class RaymarchingBox : CodeFunctionNode
    {
        public RaymarchingBox()
        {
            name = "Raymarching Box";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("BoxRaymarching", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string BoxRaymarching(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.ObjectSpaceViewDirection)] Vector3 Direction,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector3 Center,
            [Slot(3, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector3 Bounding,
            [Slot(4, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector3 LightDirection,
            [Slot(5, Binding.None, 100f, 100f, 100f, 100f)] Vector1 Steps,
            [Slot(6, Binding.None, 0.01f, 0.01f, 0.1f, 0.01f)] Vector1 MinDistance,
            [Slot(7, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    Out = box_raymarch(Position, Direction, Center, Bounding, LightDirection, (int)Steps, MinDistance);
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
            registry.ProvideFunction("box_distance", s => s.Append(@"
float box_distance(float3 position, float3 center, float3 bounding)
{
    float3 d = abs(position - center) - bounding;
    return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}"));
            registry.ProvideFunction("box_normal", s => s.Append(@"
float3 box_normal(float3 position, float3 center, float3 bounding)
{
	const float eps = 0.01;

	return normalize
	(	float3
		(	box_distance(position + float3(eps, 0, 0), center, bounding) - box_distance(position - float3(eps, 0, 0), center, bounding),
			box_distance(position + float3(0, eps, 0), center, bounding) - box_distance(position - float3(0, eps, 0), center, bounding),
			box_distance(position + float3(0, 0, eps), center, bounding) - box_distance(position - float3(0, 0, eps), center, bounding)
		)
	);
}
"));
            registry.ProvideFunction("box_render", s => s.Append(@"
float4 box_render(float3 position, float3 center, float3 bounding, float3 light_direction)
{
	float3 normal = box_normal(position, center, bounding);
	return lambert(normal, light_direction);
}
"));
            registry.ProvideFunction("box_raymarch", s => s.Append(@"
float4 box_raymarch(float3 position, float3 direction, float3 center, float3 bounding, float3 light_direction, int steps, float min_distance)
{
	for(int i = 0; i < steps; i++)
	{
		float distance = box_distance(position, center, bounding);
		if (distance < min_distance)
            return box_render(position, center, bounding, light_direction);

		position -= distance * direction;
	}
	return float4(1,1,1,0); // White
}
"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
