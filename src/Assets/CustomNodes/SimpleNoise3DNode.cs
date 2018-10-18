using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Simple Noise 3D")]
    public class SimpleNoise3DNode : CodeFunctionNode
    {
        public SimpleNoise3DNode()
        {
            name = "Simple Noise 3D";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SimpleNoise3D", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SimpleNoise3D(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.None, 500f, 500f, 500f, 500f)] Vector1 Scale,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    float t = 0.0;
    for(int i = 0; i < 3; i++)
    {
        float freq = pow(2.0, float(i));
        float amp = pow(0.5, float(3-i));
        t += valueNoise3D(float3(Position.x*Scale/freq, Position.y*Scale/freq, Position.z*Scale/freq))*amp;
    }
    Out = t;
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("randomValue", s => s.Append(@"
inline float randomValue(float3 pos)
{
    return frac(sin(dot(pos, float3(12.9898, 78.233, 125.67)))*43758.5453);
}"));

            registry.ProvideFunction("interpolate", s => s.Append(@"
inline float interpolate(float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}
"));

            registry.ProvideFunction("valueNoise3D", s => s.Append(@"
inline float valueNoise3D(float3 pos)
{
    float3 i = floor(pos);
    float3 f = frac(pos);
    f = f * f * (3.0 - 2.0 * f);

    pos = abs(frac(pos) - 0.5);
    float3 c000 = i + float3(0.0, 0.0, 0.0);
    float3 c001 = i + float3(0.0, 0.0, 1.0);
    float3 c010 = i + float3(0.0, 1.0, 0.0);
    float3 c011 = i + float3(0.0, 1.0, 1.0);
    float3 c100 = i + float3(1.0, 0.0, 0.0);
    float3 c101 = i + float3(1.0, 0.0, 1.0);
    float3 c110 = i + float3(1.0, 1.0, 0.0);
    float3 c111 = i + float3(1.0, 1.0, 1.0);
    float r000 = randomValue(c000);
    float r001 = randomValue(c001);
    float r010 = randomValue(c010);
    float r011 = randomValue(c011);
    float r100 = randomValue(c100);
    float r101 = randomValue(c101);
    float r110 = randomValue(c110);
    float r111 = randomValue(c111);

    float bottomFront = interpolate(r000, r100, f.x);
    float topFront = interpolate(r010, r110, f.x);
    float bottomBack = interpolate(r001, r101, f.x);
    float topBack = interpolate(r011, r111, f.x);
    float front = interpolate(bottomFront, topFront, f.y);
    float back = interpolate(bottomBack, topBack, f.y);
    float t = interpolate(front, back, f.z);
    return t;
}"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
