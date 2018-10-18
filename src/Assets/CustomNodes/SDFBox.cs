using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Volumetric Rendering", "Raymarching", "Signed Distance Function", "Box")]
    public class SDGBox : CodeFunctionNode
    {
        public SDGBox()
        {
            name = "Distance Signed Box";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SDFBoxRaymarching", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SDFBoxRaymarching(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.ObjectSpaceViewDirection)] Vector3 Direction,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector3 Center,
            [Slot(3, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector3 Bounding,
            [Slot(4, Binding.None, 100f, 100f, 100f, 100f)] Vector1 Steps,
            [Slot(5, Binding.None, 0.01f, 0.01f, 0.01f, 0.01f)] Vector1 MinDistance,
            [Slot(6, Binding.None)] out Vector1 Out,
            [Slot(7, Binding.None)] out Vector3 DeltaPos,
            [Slot(8, Binding.None)] out Vector3 DeltaNeg)
        {
            DeltaPos = Vector3.zero;
            DeltaNeg = Vector3.zero;
            return
                @"
{
    const float eps = 0.01;
    Out = 10;
    DeltaPos = float3(0, 0, 0);
    DeltaNeg = float3(0, 0, 0);
    for(int i = 0; i < Steps; i++)
	{
		float distance = box_distance(Position, Center, Bounding);
		if (distance < MinDistance)
        {
            Out = distance;
            DeltaPos = float3
            (	box_distance(Position + float3(eps, 0, 0), Center, Bounding),
                box_distance(Position + float3(0, eps, 0), Center, Bounding),
                box_distance(Position + float3(0, 0, eps), Center, Bounding)
            );
            DeltaNeg = float3
            (	box_distance(Position - float3(eps, 0, 0), Center, Bounding),
                box_distance(Position - float3(0, eps, 0), Center, Bounding),
                box_distance(Position - float3(0, 0, eps), Center, Bounding)
            );
            break;
        }
		Position -= distance * Direction;
	}
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("box_distance", s => s.Append(@"
float box_distance(float3 position, float3 center, float3 bounding)
{
    float3 d = abs(position - center) - bounding;
    return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}"));
            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
