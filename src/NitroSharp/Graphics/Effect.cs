using System;
using System.Linq;
using Veldrid;

namespace NitroSharp.Graphics
{
    public abstract class Effect : IDisposable
    {
        protected readonly GraphicsDevice _gd;
        protected readonly Shader _vs, _fs;
        protected readonly ShaderSetDescription _shaderSet;
        protected ResourceLayout[] _resourceLayouts;
        private BoundResourceSet[] _boundResourceSets;

        protected OutputDescription _outputDescription;
        private Pipeline _pipeline;
        private bool _initialized;

        protected Effect(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
        {
            _gd = graphicsDevice;
            _vs = vertexShader;
            _fs = fragmentShader;
            _shaderSet = new ShaderSetDescription(
                new[]
                {
                    VertexLayout
                },
                new Shader[]
                {
                    vertexShader,
                    fragmentShader
                });
        }

        protected abstract VertexLayoutDescription VertexLayout { get; }

        protected void Initialize(params BoundResourceSet[] boundResourceSets)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Already initialized.");
            }

            _boundResourceSets = boundResourceSets;
            _resourceLayouts = _boundResourceSets.Select(x => x.ResourceLayout).ToArray();
            //CreatePipeline();
            _initialized = true;
        }

        private void CreatePipeline()
        {
            _pipeline = _gd.ResourceFactory.CreateGraphicsPipeline(SetupPipeline());
        }

        protected virtual GraphicsPipelineDescription SetupPipeline()
        {
            return new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                _shaderSet,
                _resourceLayouts,
                _outputDescription);
        }

        public void Apply(CommandList commandList, OutputDescription outputDescription)
        {
            ThrowIfUninitialized();

            if (_outputDescription.ColorAttachments == null || !outputDescription.Equals(_outputDescription))
            {
                _pipeline?.Dispose();
                _outputDescription = outputDescription;
                CreatePipeline();
            }

            commandList.SetPipeline(_pipeline);
            for (int i = 0; i < _boundResourceSets.Length; i++)
            {
                _boundResourceSets[i].Apply(commandList, (uint)i);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Initialize() must be called before using this object.");
            }
        }

        public virtual void Dispose()
        {
            foreach (var resourceSet in _boundResourceSets)
            {
                resourceSet.Dispose();
            }

            _pipeline.Dispose();
            _vs.Dispose();
            _fs.Dispose();
        }
    }
}
