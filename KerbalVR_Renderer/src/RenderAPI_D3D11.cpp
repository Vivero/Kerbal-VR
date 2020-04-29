#pragma warning(disable : 26812)

#include "RenderAPI.h"
#include "PlatformBase.h"

// Direct3D 11 implementation of RenderAPI.

#if SUPPORT_D3D11

#include <assert.h>
#include <d3d11.h>
#include "IUnityGraphicsD3D11.h"


class RenderAPI_D3D11 : public RenderAPI
{
public:
    RenderAPI_D3D11();
    virtual ~RenderAPI_D3D11() { }

    virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces);

    virtual bool GetUsesReverseZ() { return (int)m_Device->GetFeatureLevel() >= (int)D3D_FEATURE_LEVEL_10_0; }

    virtual void* BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch);
    virtual void EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr);

private:
    void CreateResources();
    void ReleaseResources();

private:
    ID3D11Device* m_Device;
    ID3D11RasterizerState* m_RasterState;
    ID3D11BlendState* m_BlendState;
    ID3D11DepthStencilState* m_DepthState;
};


RenderAPI* CreateRenderAPI_D3D11()
{
    return new RenderAPI_D3D11();
}


RenderAPI_D3D11::RenderAPI_D3D11()
    : m_Device(NULL)
    , m_RasterState(NULL)
    , m_BlendState(NULL)
    , m_DepthState(NULL)
{
}


void RenderAPI_D3D11::ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces)
{
    switch (type)
    {
    case kUnityGfxDeviceEventInitialize:
    {
        IUnityGraphicsD3D11* d3d = interfaces->Get<IUnityGraphicsD3D11>();
        m_Device = d3d->GetDevice();
        CreateResources();
        break;
    }
    case kUnityGfxDeviceEventShutdown:
        ReleaseResources();
        break;
    }
}


void RenderAPI_D3D11::CreateResources()
{
    D3D11_BUFFER_DESC desc;
    memset(&desc, 0, sizeof(desc));

    // render states
    D3D11_RASTERIZER_DESC rsdesc;
    memset(&rsdesc, 0, sizeof(rsdesc));
    rsdesc.FillMode = D3D11_FILL_SOLID;
    rsdesc.CullMode = D3D11_CULL_NONE;
    rsdesc.DepthClipEnable = TRUE;
    m_Device->CreateRasterizerState(&rsdesc, &m_RasterState);

    D3D11_DEPTH_STENCIL_DESC dsdesc;
    memset(&dsdesc, 0, sizeof(dsdesc));
    dsdesc.DepthEnable = TRUE;
    dsdesc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ZERO;
    dsdesc.DepthFunc = GetUsesReverseZ() ? D3D11_COMPARISON_GREATER_EQUAL : D3D11_COMPARISON_LESS_EQUAL;
    m_Device->CreateDepthStencilState(&dsdesc, &m_DepthState);

    D3D11_BLEND_DESC bdesc;
    memset(&bdesc, 0, sizeof(bdesc));
    bdesc.RenderTarget[0].BlendEnable = FALSE;
    bdesc.RenderTarget[0].RenderTargetWriteMask = 0xF;
    m_Device->CreateBlendState(&bdesc, &m_BlendState);
}


void RenderAPI_D3D11::ReleaseResources()
{
    SAFE_RELEASE(m_RasterState);
    SAFE_RELEASE(m_BlendState);
    SAFE_RELEASE(m_DepthState);
}


void* RenderAPI_D3D11::BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch)
{
    const int rowPitch = textureWidth * 4;
    // Just allocate a system memory buffer here for simplicity
    unsigned char* data = new unsigned char[(size_t)rowPitch * textureHeight];
    *outRowPitch = rowPitch;
    return data;
}


void RenderAPI_D3D11::EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr)
{
    ID3D11Texture2D* d3dtex = (ID3D11Texture2D*)textureHandle;
    assert(d3dtex);

    ID3D11DeviceContext* ctx = NULL;
    m_Device->GetImmediateContext(&ctx);
    // Update texture data, and free the memory buffer
    ctx->UpdateSubresource(d3dtex, 0, NULL, dataPtr, rowPitch, 0);
    delete[] (unsigned char*)dataPtr;
    ctx->Release();
}

#endif // #if SUPPORT_D3D11
