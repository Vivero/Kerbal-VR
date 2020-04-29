#include <assert.h>

#include "PlatformBase.h"
#include "RenderAPI.h"
#include "openvr.h"

//
// Global Variables
//

// structure to hold together the texture information
struct OpenVrTexture_t {
    vr::Texture_t         texture;
    vr::VRTextureBounds_t bounds;
};
static struct OpenVrTexture_t s_hmdEyeTextures[2];

//
// UnitySetInterfaces
//
// These hooks are not getting called when the plugin is loaded in Kerbal Space Program,
// not sure why. These are thought to be required, per the Unity documentation. They do
// get called in the Unity tester project, so I may just leave these here for future
// reference.
//

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);

static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces * unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
    s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    // Run OnGraphicsDeviceEvent(initialize) manually on plugin load
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}


//
// GraphicsDeviceEvent
//

static RenderAPI* s_CurrentAPI = NULL;
static UnityGfxRenderer s_DeviceType = kUnityGfxRendererNull;

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    // Create graphics API implementation upon initialization
    if (eventType == kUnityGfxDeviceEventInitialize)
    {
        assert(s_CurrentAPI == NULL);
        s_DeviceType = s_Graphics->GetRenderer();
        s_CurrentAPI = CreateRenderAPI(s_DeviceType);
    }

    // Let the implementation process the device related events
    if (s_CurrentAPI)
    {
        s_CurrentAPI->ProcessDeviceEvent(eventType, s_UnityInterfaces);
    }

    // Cleanup graphics API implementation upon shutdown
    if (eventType == kUnityGfxDeviceEventShutdown)
    {
        delete s_CurrentAPI;
        s_CurrentAPI = NULL;
        s_DeviceType = kUnityGfxRendererNull;
    }
}


//
// KerbalVR Renderer Functions
//

// store the texture information for the headset renders
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetTextureFromUnity(
    int textureIndex,
    void* textureHandle,
    float uMin,
    float uMax,
    float vMin,
    float vMax)
{
    // arguments check
    assert(textureIndex >= 0 && textureIndex < 2);
    assert(textureHandle != NULL);

    s_hmdEyeTextures[textureIndex].texture.eColorSpace = vr::EColorSpace::ColorSpace_Auto;
    s_hmdEyeTextures[textureIndex].texture.eType = vr::ETextureType::TextureType_DirectX;
    s_hmdEyeTextures[textureIndex].texture.handle = textureHandle;
    s_hmdEyeTextures[textureIndex].bounds.uMin = uMin;
    s_hmdEyeTextures[textureIndex].bounds.uMax = uMax;
    s_hmdEyeTextures[textureIndex].bounds.vMin = vMin;
    s_hmdEyeTextures[textureIndex].bounds.vMax = vMax;
}

// Plugin function to handle a specific rendering event
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    // don't do anything if textures don't exist
    if (/*s_CurrentAPI == NULL || */ s_hmdEyeTextures[0].texture.handle == NULL || s_hmdEyeTextures[1].texture.handle == NULL)
        return;

    // must be called before Submit on the render thread
    vr::EVRCompositorError error = vr::EVRCompositorError::VRCompositorError_None;
    error = vr::VRCompositor()->WaitGetPoses(NULL, 0, NULL, 0);

    // submit the eye textures
    error = vr::VRCompositor()->Submit(vr::EVREye::Eye_Left, &(s_hmdEyeTextures[0].texture), &(s_hmdEyeTextures[0].bounds));
    error = vr::VRCompositor()->Submit(vr::EVREye::Eye_Right, &(s_hmdEyeTextures[1].texture), &(s_hmdEyeTextures[1].bounds));

    // notify we are done submitting frames so the render thread can continue along on its merry way
    vr::VRCompositor()->PostPresentHandoff();
}

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
GetRenderEventFunc()
{
    return OnRenderEvent;
}
