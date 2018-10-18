using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Voronoi 3D")]
    public class Voronoi3DNode : CodeFunctionNode
    {
        public Voronoi3DNode()
        {
            name = "Voronoi 3D";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Voronoi3D", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Voronoi3D(
            [Slot(0, Binding.ObjectSpacePosition)] Vector3 Position,
            [Slot(1, Binding.None, 5.0f, 5.0f, 5.0f, 5.0f)] Vector1 CellDensity,
            [Slot(2, Binding.None)] out Vector1 Out,
            [Slot(3, Binding.None)] out Vector3 Cells)
        {
            Cells = Vector3.zero;
            return
                @"
{
    Out = 0;
    Cells = float3(0, 1, 0);
    float3 g = floor(Position * CellDensity);
    float3 f = frac(Position * CellDensity);
    float res = float(8.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            for(int z=-1; z<=1; z++)
            {
                float3 lattice = float3(x,y,z);
                float3 offset = random_vector(lattice + g);
                float d = distance(lattice + offset, f);

                if(d < res)
                {
                    res = d;
                    Out = res;
                    Cells = offset;
                }
            }
        }
    }
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
            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}