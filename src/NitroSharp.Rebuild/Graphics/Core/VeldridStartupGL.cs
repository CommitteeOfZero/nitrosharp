using System;
using System.Diagnostics;
using System.Threading;
using SDL;
using Veldrid;
using Veldrid.OpenGL;
using static SDL.SDL3;

namespace NitroSharp.Graphics.Core;

internal static class VeldridStartupGL
{
    public static unsafe GraphicsDevice CreateDefaultOpenGLGraphicsDevice(
        GraphicsDeviceOptions options,
        Sdl3Window window,
        GraphicsBackend backend)
    {
        SDL_ClearError();
        SDL_Window* sdlWindow = window.SdlWindow;

        SetSDLGLContextAttributes(options, backend);

        SDL_GLContextState* glContext = SDL_GL_CreateContext(sdlWindow);
        string? error = SDL_GetError();
        if (!string.IsNullOrEmpty(error))
        {
            throw new VeldridException(
                $"Unable to create OpenGL Context: \"{error}\"." +
                $"This may indicate that the system does not support the requested OpenGL" +
                $"profile, version, or Swapchain format.");
        }

        int actualDepthSize;
        SDL_GL_GetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, &actualDepthSize);
        int actualStencilSize;
        SDL_GL_GetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, &actualStencilSize);

        SDL_GL_SetSwapInterval(options.SyncToVerticalBlank ? 1 : 0);

        var platformInfo = new OpenGLPlatformInfo(
            (IntPtr)glContext,
            proc => SDL_GL_GetProcAddress(proc),
            context => SDL_GL_MakeCurrent(sdlWindow, (SDL_GLContextState*)context),
            () => (IntPtr)SDL_GL_GetCurrentContext(),
            () => SDL_GL_MakeCurrent((SDL_Window*)IntPtr.Zero, (SDL_GLContextState*)IntPtr.Zero),
            context => SDL_GL_DestroyContext((SDL_GLContextState*)context),
            () => SDL_GL_SwapWindow(sdlWindow),
            sync => SDL_GL_SetSwapInterval(sync ? 1 : 0));

        return GraphicsDevice.CreateOpenGL(
            options,
            platformInfo,
            window.Size.Width,
            window.Size.Height);
    }

    private static void SetSDLGLContextAttributes(GraphicsDeviceOptions options, GraphicsBackend backend)
    {
        if (backend != GraphicsBackend.OpenGL && backend != GraphicsBackend.OpenGLES)
        {
            throw new VeldridException(
                $"{nameof(backend)} must be {nameof(GraphicsBackend.OpenGL)} or {nameof(GraphicsBackend.OpenGLES)}.");
        }

        SDL_GLContextFlag contextFlags = options.Debug
            ? SDL_GLContextFlag.SDL_GL_CONTEXT_DEBUG_FLAG | SDL_GLContextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG
            : SDL_GLContextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;

        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_FLAGS, (int)contextFlags);

        (int major, int minor) = GetMaxGLVersion(backend == GraphicsBackend.OpenGLES);

        if (backend == GraphicsBackend.OpenGL)
        {
            SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE);
        }
        else
        {
            SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_ES);
        }

        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, minor);

        int depthBits = 0;
        int stencilBits = 0;
        if (options.SwapchainDepthFormat.HasValue)
        {
            switch (options.SwapchainDepthFormat)
            {
                case PixelFormat.D16_UNorm:
                case PixelFormat.R16_UNorm:
                    depthBits = 16;
                    break;
                case PixelFormat.D16_UNorm_S8_UInt:
                    depthBits = 16;
                    stencilBits = 8;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    depthBits = 24;
                    stencilBits = 8;
                    break;
                case PixelFormat.D32_Float:
                case PixelFormat.R32_Float:
                    depthBits = 32;
                    break;
                case PixelFormat.D32_Float_S8_UInt:
                    depthBits = 32;
                    stencilBits = 8;
                    break;
                default:
                    throw new VeldridException("Invalid depth format: " + options.SwapchainDepthFormat.Value);
            }
        }

        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, depthBits);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, stencilBits);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, options.SwapchainSrgbFormat ? 1 : 0);
    }

    private static readonly Lock s_glVersionLock = new();
    private static (int Major, int Minor)? s_maxSupportedGLVersion;
    private static (int Major, int Minor)? s_maxSupportedGLESVersion;

    private static (int Major, int Minor) GetMaxGLVersion(bool gles)
    {
        lock (s_glVersionLock)
        {
            (int Major, int Minor)? maxVer = gles ? s_maxSupportedGLESVersion : s_maxSupportedGLVersion;
            if (maxVer == null)
            {
                maxVer = TestMaxVersion(gles);
                if (gles) { s_maxSupportedGLESVersion = maxVer; }
                else { s_maxSupportedGLVersion = maxVer; }
            }

            return maxVer.Value;
        }
    }

    private static (int Major, int Minor) TestMaxVersion(bool gles)
    {
        (int, int)[] testVersions = gles
            ? [(3, 2), (3, 0)]
            : [(4, 6), (4, 3), (4, 0), (3, 3), (3, 0)];

        foreach ((int major, int minor) in testVersions)
        {
            if (TestIndividualGLVersion(gles, major, minor)) { return (major, minor); }
        }

        return (0, 0);
    }

    private static unsafe bool TestIndividualGLVersion(bool gles, int major, int minor)
    {
        SDL_GLProfile profileMask = gles
            ? SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_ES
            : SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE;

        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)profileMask);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, minor);

        SDL_Window* window = SDL_CreateWindow(
            string.Empty,
            1, 1,
            SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL_WindowFlags.SDL_WINDOW_OPENGL);
        string? error = SDL_GetError();

        if (window == null || !string.IsNullOrEmpty(error))
        {
            SDL_ClearError();
            Debug.WriteLine($"Unable to create version {major}.{minor} {profileMask} context.");
            return false;
        }

        SDL_GLContextState* context = SDL_GL_CreateContext(window);
        error = SDL_GetError();
        if (error != null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                SDL_ClearError();
                Debug.WriteLine($"Unable to create version {major}.{minor} {profileMask} context.");
                SDL_DestroyWindow(window);
                return false;
            }
        }

        SDL_GL_DestroyContext(context);
        SDL_DestroyWindow(window);
        return true;
    }
}
