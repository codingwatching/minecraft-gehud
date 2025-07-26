void UnpackVertex_float(float2 Data, out float3 Position, out float2 UV, out float3 Norm, out float4 Light)
{
#if defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X)
    uint aData = asuint(Data.x);
    uint bData = asuint(Data.y);
#else
    uint aData = uint(Data.x);
    uint bData = uint(Data.y);
#endif

    // Mask = 2 ^ Bit - 1

    // A
    uint yBit = 5u;
    uint zBit = 5u;
    uint iBit = 9u;
    uint nBit = 3u;

    uint yMask = 31u;
    uint zMask = 31u;
    uint iMask = 511u;
    uint nMask = 7u;

    uint inBit = iBit + nBit;
    uint zinBit = zBit + inBit;
    uint yzinBit = yBit + zinBit;

    uint x = uint(aData >> yzinBit);
    uint y = uint((aData >> zinBit) & yMask);
    uint z = uint((aData >> inBit) & zMask);
    uint i = uint((aData >> nBit) & iMask);
    uint n = uint(aData & nMask);

    Position = float3(x, y, z);

    uint u = i % 17u;
    uint v = i / 17u;
    float uvStep = 16.0 / 256.0;
    UV = float2(u * uvStep, v * uvStep);

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
}