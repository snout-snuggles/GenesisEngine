﻿// ReSharper disable ConvertClosureToMethodGroup

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Machine.Specifications;
using NSubstitute;
using Microsoft.Xna.Framework;

namespace GenesisEngine.Specs.DomainSpecs
{
    [Subject(typeof(QuadMesh))]
    public class when_the_mesh_is_initialized : QuadMeshContext
    {
        Establish context = () =>
            _terrainColorizer.GetColor(Arg.Any<double>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<QuadNodeExtents>())
                .Returns(Color.PapayaWhip);

        Because of = () =>
            InitializeTopFacingMesh();

        It should_initialize_the_renderer = () =>
            _renderer.InitializeWasCalled.ShouldBeTrue();

        //It should_get_height_data_from_the_generator = () =>
        //    _generator.Received().GetHeight(Arg.Any<DoubleVector3>(), Arg.Is(5), Arg.Any<double>());

        It should_project_center_point_into_spherical_mesh_space = () =>
        {
            var centerPosition = _renderer.Vertices[_renderer.Vertices.Length / 2].Position;
            centerPosition.ShouldBeCloseTo(Vector3.Zero);
        };

        It should_calculate_center_point_normal = () =>
        {
            var centerNormal = _renderer.Vertices[_renderer.Vertices.Length / 2].Normal;
            centerNormal.ShouldBeCloseTo(Vector3.Up);
        };

        It should_project_top_left_corner_into_spherical_mesh_space = () =>
        {
            var topLeftPosition = _renderer.Vertices[0].Position;
            AssertCornerIsProjected(topLeftPosition, Vector3.Up, Vector3.Left, Vector3.Forward);
        };

        It should_project_bottom_right_corner_into_spherical_mesh_space = () =>
        {
            var bottomRightPosition = _renderer.Vertices[_renderer.Vertices.Length - 1].Position;
            AssertCornerIsProjected(bottomRightPosition, Vector3.Up, Vector3.Backward, Vector3.Right);
        };

        It should_assign_vertex_colors_from_the_colorizer = () =>
            _renderer.Vertices[0].Color.ShouldEqual(Color.PapayaWhip);
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_is_below_the_horizon : QuadMeshContext
    {
        Establish context = () =>
            InitializeTopFacingMesh();

        Because of = () =>
            _mesh.Update(DoubleVector3.Down * 100, DoubleVector3.Zero);

        It should_not_be_visible = () =>
            _mesh.IsAboveHorizonToCamera.ShouldBeFalse();
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_is_above_the_horizon : QuadMeshContext
    {
        Establish context = () =>
            InitializeTopFacingMesh();

        Because of = () =>
            _mesh.Update(DoubleVector3.Up * 100, DoubleVector3.Zero);

        It should_be_visible = () =>
            _mesh.IsAboveHorizonToCamera.ShouldBeTrue();
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_is_updated : QuadMeshContext
    {
        public static DoubleVector3 _topLeftPosition;
        public static DoubleVector3 _cameraPosition;

        Establish context = () =>
        {
            InitializeTopFacingMesh();

            _topLeftPosition = _renderer.Vertices[0].Position;
            _cameraPosition = DoubleVector3.Up * 100;
        };

        Because of = () =>
            _mesh.Update(_cameraPosition, DoubleVector3.Zero);

        It should_calculate_the_ratio_of_distance_to_width = () =>
            _mesh.CameraDistanceToWidthRatio.ShouldEqual((_cameraPosition.Length() - _radius) / (_extents.Width * _radius));
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_in_the_view_frustum_is_drawn : QuadMeshContext
    {
        public static DoubleVector3 _cameraLocation;
        public static BoundingFrustum _viewFrustum;
        public static Matrix _viewMatrix;
        public static Matrix _projectionMatrix;

        Establish context = () =>
        {
            _cameraLocation = DoubleVector3.Up * 100;
            _viewFrustum = CreateFrustumLookingAt(DoubleVector3.Down * 200);
            _viewMatrix = Matrix.Identity;
            _projectionMatrix = Matrix.Identity;

            InitializeTopFacingMesh();
            _mesh.Update(_cameraLocation, DoubleVector3.Zero);
        };

        Because of = () =>
            _mesh.Draw(_cameraLocation, _viewFrustum, _viewMatrix, _projectionMatrix);

        It should_draw_the_mesh = () =>
            _renderer.DrawWasCalled.ShouldBeTrue();

        It should_draw_the_mesh_in_the_correct_location = () =>
            _renderer.Location.ShouldEqual(Vector3.Up * _radius);
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_not_in_the_view_frustum_is_drawn : QuadMeshContext
    {
        public static DoubleVector3 _cameraLocation;
        public static BoundingFrustum _viewFrustum;
        public static Matrix _viewMatrix;
        public static Matrix _projectionMatrix;

        Establish context = () =>
        {
            _cameraLocation = DoubleVector3.Up * 100;
            _viewFrustum = CreateFrustumLookingAt(DoubleVector3.Up * 200 + DoubleVector3.Right);
            _viewMatrix = Matrix.Identity;
            _projectionMatrix = Matrix.Identity;

            InitializeTopFacingMesh();
            _mesh.Update(_cameraLocation, DoubleVector3.Zero);
        };

        Because of = () =>
            _mesh.Draw(_cameraLocation, _viewFrustum, _viewMatrix, _projectionMatrix);

        It should_not_draw_the_mesh = () =>
            _renderer.DrawWasCalled.ShouldBeFalse();
    }

    [Subject(typeof(QuadMesh))]
    public class when_a_mesh_is_disposed : QuadMeshContext
    {
        Establish context = () =>
            InitializeTopFacingMesh();

        Because of = () =>
            _mesh.Dispose();

        It should_dispose_the_renderer = () =>
            _renderer.DisposeWasCalled.ShouldBeTrue();
    }

    public class QuadMeshContext
    {
        public static readonly float _radius = 10;
        public static DoubleVector3 _location;
        public static QuadNodeExtents _extents = new QuadNodeExtents(-1.0, 1.0, -1.0, 1.0);

        public static IHeightmapGenerator _heightmapGenerator;
        public static ITerrainColorizer _terrainColorizer;
        public static MockQuadMeshRenderer _renderer;
        public static ISettings _settings;
        public static Statistics _statistics;

        public static QuadMesh _mesh;

        Establish context = () =>
        {
            var heightGenerator = Substitute.For<IHeightGenerator>();
            heightGenerator.GetHeight(DoubleVector3.Zero, 0, 0).ReturnsForAnyArgs(0.0);
            _heightmapGenerator = new HeightmapGenerator(heightGenerator);

            _terrainColorizer = Substitute.For<ITerrainColorizer>();

            // We're using a hand-rolled fake here because of a bug
            // in .Net that prevents mocking of multi-dimentional arrays:
            // http://code.google.com/p/moq/issues/detail?id=182#c0
            _renderer = new MockQuadMeshRenderer();

            _settings = Substitute.For<ISettings>();

            _statistics = new Statistics();

            _mesh = new QuadMesh(_heightmapGenerator, _terrainColorizer, _renderer, _settings);
        };

        public static void InitializeTopFacingMesh()
        {
            _mesh.Initialize(_radius, Vector3.Up, Vector3.Backward, Vector3.Right, _extents, 5);
        }

        public static void AssertCornerIsProjected(Vector3 projectedVector, Vector3 normalVector, Vector3 uVector, Vector3 vVector)
        {
            Vector3 expectedVector = ProjectCornerToSphere(normalVector, uVector, vVector);

            projectedVector.ShouldBeCloseTo(expectedVector);
        }

        public static Vector3 ProjectCornerToSphere(Vector3 normalVector, Vector3 uVector, Vector3 vVector)
        {
            var expectedVector = (normalVector * _radius) + (uVector * _radius) + (vVector * _radius);
            expectedVector.Normalize();
            expectedVector *= _radius;
            expectedVector -= normalVector * _radius;
            return expectedVector;
        }

        public static BoundingFrustum CreateFrustumLookingAt(Vector3 lookingAt)
        {
            var projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4 / 1f, 1f, 1f, 10f);
            var originBasedViewMatrix = Matrix.CreateLookAt(Vector3.Zero, lookingAt, Vector3.Up);
            
            return new BoundingFrustum(Matrix.Multiply(originBasedViewMatrix, projectionMatrix));
        }
    }

    public class MockQuadMeshRenderer : IQuadMeshRenderer, IDisposable
    {
        public bool InitializeWasCalled { get; private set; }
        public bool DrawWasCalled { get; private set; }
        public bool DisposeWasCalled { get; private set; }
        public Vector3 Location { get; private set; }
        public VertexPositionNormalColor[] Vertices { get; private set; }

        public virtual void Initialize(VertexPositionNormalColor[] vertices, short[] indices, BoundingBox boundingBox)
        {
            InitializeWasCalled = true;
            Vertices = vertices;
        }

        public virtual void Draw(DoubleVector3 location, DoubleVector3 cameraLocation, Matrix originBasedViewMatrix, Matrix projectionMatrix)
        {
            DrawWasCalled = true;
            Location = location;
        }

        public void Dispose()
        {
            DisposeWasCalled = true;
        }
    }
}

