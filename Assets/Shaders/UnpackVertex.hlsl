void UnpackVertex_float(int AtlasSize, float3 Data, out float3 Position, out float2 UV, out float3 Norm, out float4 Light)
{
#if defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X)
    uint aData = asuint(Data.x);
    uint bData = asuint(Data.y);
    uint cData = asuint(Data.z);
#else
    uint aData = uint(Data.x);
    uint bData = uint(Data.y);
    uint cData = uint(Data.z);
#endif

    // Mask = 2 ^ Bit - 1

    // A

    uint yBit = 5u;
    uint zBit = 5u;
    uint nBit = 3u;

    uint yMask = 31u;
    uint zMask = 31u;
    uint nMask = 7u;

    uint znBit = zBit + nBit;
    uint yznBit = yBit + znBit;

    uint x = uint(aData >> yznBit);
    uint y = uint((aData >> znBit) & yMask);
    uint z = uint((aData >> nBit) & zMask);
    uint n = uint(aData & nMask);

    Position = float3(x, y, z);

    static const float3 normals[6] = {
        float3(1.0f, 0.0f, 0.0f),
        float3(-1.0f, 0.0f, 0.0f),
        float3(0.0f, 1.0f, 0.0f),
        float3(0.0f, -1.0f, 0.0f),
        float3(0.0f, 0.0f, 1.0f),
        float3(0.0f, 0.0f, -1.0f),
    };

    Norm = normals[n];

    // B

    uint gBit = 6u;
    uint bBit = 6u;
    uint sBit = 6u;

    uint gMask = 63u;
    uint bMask = 63u;
    uint sMask = 63u;

    uint bsBit = bBit + sBit;
    uint gbsBit = gBit + bsBit;

    float r = int(bData >> gbsBit) / 4.0 / 16.0;
    float g = int((bData >> bsBit) & gMask) / 4.0 / 16.0;
    float b = int((bData >> sBit) & bMask) / 4.0 / 16.0;
    float s = int(bData & sMask) / 4.0 / 16.0;
    Light = float4(r, g, b, s);

    // C
    uint u = cData % (AtlasSize + 1);
    uint v = cData / (AtlasSize + 1);
    float uvStep = 1.0 / AtlasSize;
    UV = float2(u * uvStep, v * uvStep);
}