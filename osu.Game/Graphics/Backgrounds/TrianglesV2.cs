﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Utils;
using osuTK;
using System;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Allocation;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using osuTK.Graphics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace osu.Game.Graphics.Backgrounds
{
    public class TrianglesV2 : Drawable
    {
        private const float triangle_size = 100;
        private const float base_velocity = 50;
        private const int texture_height = 128;

        /// <summary>
        /// sqrt(3) / 2
        /// </summary>
        private const float equilateral_triangle_ratio = 0.866f;

        private readonly Bindable<Color4> colourTop = new Bindable<Color4>(Color4.White);
        private readonly Bindable<Color4> colourBottom = new Bindable<Color4>(Color4.Black);

        public Color4 ColourTop
        {
            get => colourTop.Value;
            set => colourTop.Value = value;
        }

        public Color4 ColourBottom
        {
            get => colourBottom.Value;
            set => colourBottom.Value = value;
        }

        public float Thickness { get; set; } = 0.02f; // No need for invalidation since it's happening in Update()

        /// <summary>
        /// Whether we should create new triangles as others expire.
        /// </summary>
        protected virtual bool CreateNewTriangles => true;

        private readonly BindableFloat spawnRatio = new BindableFloat(1f);

        /// <summary>
        /// The amount of triangles we want compared to the default distribution.
        /// </summary>
        public float SpawnRatio
        {
            get => spawnRatio.Value;
            set => spawnRatio.Value = value;
        }

        /// <summary>
        /// The relative velocity of the triangles. Default is 1.
        /// </summary>
        public float Velocity = 1;

        private readonly List<TriangleParticle> parts = new List<TriangleParticle>();

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private Random? stableRandom;

        private IShader shader = null!;
        private Texture texture = null!;

        /// <summary>
        /// Construct a new triangle visualisation.
        /// </summary>
        /// <param name="seed">An optional seed to stabilise random positions / attributes. Note that this does not guarantee stable playback when seeking in time.</param>
        public TrianglesV2(int? seed = null)
        {
            if (seed != null)
                stableRandom = new Random(seed.Value);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "TriangleBorder");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            colourTop.BindValueChanged(_ => updateTexture());
            colourBottom.BindValueChanged(_ => updateTexture(), true);

            spawnRatio.BindValueChanged(_ => Reset(), true);
        }

        private void updateTexture()
        {
            var image = new Image<Rgba32>(texture_height, 1);

            texture = renderer.CreateTexture(1, texture_height, true);

            for (int i = 0; i < texture_height; i++)
            {
                float ratio = (float)i / texture_height;

                image[i, 0] = new Rgba32(
                    colourBottom.Value.R * ratio + colourTop.Value.R * (1f - ratio),
                    colourBottom.Value.G * ratio + colourTop.Value.G * (1f - ratio),
                    colourBottom.Value.B * ratio + colourTop.Value.B * (1f - ratio)
                );
            }

            texture.SetData(new TextureUpload(image));
            Invalidate(Invalidation.DrawNode);
        }

        protected override void Update()
        {
            base.Update();

            Invalidate(Invalidation.DrawNode);

            if (CreateNewTriangles)
                addTriangles(false);

            float elapsedSeconds = (float)Time.Elapsed / 1000;
            // Since position is relative, the velocity needs to scale inversely with DrawHeight.
            float movedDistance = -elapsedSeconds * Velocity * base_velocity / DrawHeight;

            for (int i = 0; i < parts.Count; i++)
            {
                TriangleParticle newParticle = parts[i];

                newParticle.Position.Y += Math.Max(0.5f, parts[i].SpeedMultiplier) * movedDistance;

                parts[i] = newParticle;

                float bottomPos = parts[i].Position.Y + triangle_size * equilateral_triangle_ratio / DrawHeight;
                if (bottomPos < 0)
                    parts.RemoveAt(i);
            }
        }

        /// <summary>
        /// Clears and re-initialises triangles according to a given seed.
        /// </summary>
        /// <param name="seed">An optional seed to stabilise random positions / attributes. Note that this does not guarantee stable playback when seeking in time.</param>
        public void Reset(int? seed = null)
        {
            if (seed != null)
                stableRandom = new Random(seed.Value);

            parts.Clear();
            addTriangles(true);
        }

        protected int AimCount { get; private set; }

        private void addTriangles(bool randomY)
        {
            // Limited by the maximum size of QuadVertexBuffer for safety.
            const int max_triangles = ushort.MaxValue / (IRenderer.VERTICES_PER_QUAD + 2);

            AimCount = (int)Math.Clamp(DrawWidth * 0.02f * SpawnRatio, 1, max_triangles);

            int currentCount = parts.Count;

            for (int i = 0; i < AimCount - currentCount; i++)
                parts.Add(createTriangle(randomY));
        }

        private TriangleParticle createTriangle(bool randomY)
        {
            TriangleParticle particle = CreateTriangle();

            float y = 1;

            if (randomY)
            {
                // since triangles are drawn from the top - allow them to be positioned a bit above the screen
                float maxOffset = triangle_size * equilateral_triangle_ratio / DrawHeight;
                y = Interpolation.ValueAt(nextRandom(), -maxOffset, 1f, 0f, 1f);
            }

            particle.Position = new Vector2(nextRandom(), y);

            return particle;
        }

        /// <summary>
        /// Creates a triangle particle with a random speed multiplier.
        /// </summary>
        /// <returns>The triangle particle.</returns>
        protected virtual TriangleParticle CreateTriangle()
        {
            const float std_dev = 0.16f;
            const float mean = 0.5f;

            float u1 = 1 - nextRandom(); //uniform(0,1] random floats
            float u2 = 1 - nextRandom();
            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2)); // random normal(0,1)
            float speedMultiplier = Math.Max(mean + std_dev * randStdNormal, 0.1f); // random normal(mean,stdDev^2)

            return new TriangleParticle { SpeedMultiplier = speedMultiplier };
        }

        private float nextRandom() => (float)(stableRandom?.NextDouble() ?? RNG.NextSingle());

        protected override DrawNode CreateDrawNode() => new TrianglesDrawNode(this);

        private class TrianglesDrawNode : DrawNode
        {
            protected new TrianglesV2 Source => (TrianglesV2)base.Source;

            private IShader shader = null!;
            private Texture texture = null!;

            private readonly List<TriangleParticle> parts = new List<TriangleParticle>();
            private Vector2 size;
            private float thickness;

            private IVertexBatch<TexturedVertex2D>? vertexBatch;

            public TrianglesDrawNode(TrianglesV2 source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                shader = Source.shader;
                texture = Source.texture;
                size = Source.DrawSize;
                thickness = Source.Thickness;

                parts.Clear();
                parts.AddRange(Source.parts);
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (Source.AimCount == 0)
                    return;

                if (vertexBatch == null || vertexBatch.Size != Source.AimCount)
                {
                    vertexBatch?.Dispose();
                    vertexBatch = renderer.CreateQuadBatch<TexturedVertex2D>(Source.AimCount, 1);
                }

                shader.Bind();
                shader.GetUniform<float>("thickness").UpdateValue(ref thickness);

                foreach (TriangleParticle particle in parts)
                {
                    var offset = triangle_size * new Vector2(0.5f, equilateral_triangle_ratio);

                    Vector2 topLeft = particle.Position * size + new Vector2(-offset.X, 0f);
                    Vector2 topRight = particle.Position * size + new Vector2(offset.X, 0);
                    Vector2 bottomLeft = particle.Position * size + new Vector2(-offset.X, offset.Y);
                    Vector2 bottomRight = particle.Position * size + new Vector2(offset.X, offset.Y);

                    var drawQuad = new Quad(
                        Vector2Extensions.Transform(topLeft, DrawInfo.Matrix),
                        Vector2Extensions.Transform(topRight, DrawInfo.Matrix),
                        Vector2Extensions.Transform(bottomLeft, DrawInfo.Matrix),
                        Vector2Extensions.Transform(bottomRight, DrawInfo.Matrix)
                    );

                    var tRect = new Quad(
                        topLeft.X / size.X,
                        topLeft.Y / size.Y * texture_height,
                        (topRight.X - topLeft.X) / size.X,
                        (bottomRight.Y - topRight.Y) / size.Y * texture_height
                    ).AABBFloat;

                    renderer.DrawQuad(texture, drawQuad, DrawColourInfo.Colour, tRect, vertexBatch.AddAction, textureCoords: tRect);
                }

                shader.Unbind();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                vertexBatch?.Dispose();
            }
        }

        protected struct TriangleParticle
        {
            /// <summary>
            /// The position of the top vertex of the triangle.
            /// </summary>
            public Vector2 Position;

            /// <summary>
            /// The speed multiplier of the triangle.
            /// </summary>
            public float SpeedMultiplier;
        }
    }
}
