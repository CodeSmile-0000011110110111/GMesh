using CodeSmile;
using CodeSmile.GraphMesh;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GMeshTestBehaviour : MonoBehaviour
{
	[SerializeField] private bool _recreateMesh;
	[SerializeField] private bool _autoRecreateMesh;

	[Header("Primitive Parameters")]
	[SerializeField] private PrimitiveType _primitiveType;
	[Range(3, 60)] [SerializeField] private int _polygonVertexCount = 3;
	[Range(0.01f, 1000f)] [SerializeField] private float _polygonScale = 1f;
	[SerializeField] private GMeshPlane _planeParameters = new();
	[SerializeField] private GMeshCube _cubeParameters = new();

	[Header("Euler Operators")]
	[Range(0, 9)] [SerializeField] private int _edgeTesselation;

	[Header("Debug")]
	public bool _logToConsole;
	public GMesh.DebugDraw _debugDraw = 0;

	private int _prevTriangulationApproach;
	private int _prevVertexCount;
	private PrimitiveType _prevPrimitiveType;

	//private PlaneParameters _planeParameters = new();
	private MeshFilter _meshFilter;
	private GMesh _gMesh;
	private Transform _centroidMarker;

	private void OnEnable()
	{
		AssemblyReloadEvents.beforeAssemblyReload += DisposeGMesh;
		_meshFilter = GetComponent<MeshFilter>();
		UpdateMesh();
	}

	private void OnDestroy() => DisposeGMesh();

	private void OnDrawGizmos()
	{
		if (_gMesh == null)
			return;

		_gMesh.DebugDrawGizmos(transform, _debugDraw);
	}

	private void OnValidate()
	{
		if (_planeParameters.ResetToDefaults)
			_planeParameters.Reset();
		if (_cubeParameters.ResetToDefaults)
			_cubeParameters.Reset();

		if (_prevPrimitiveType != _primitiveType)
		{
			_prevPrimitiveType = _primitiveType;
			_recreateMesh = true;
		}

		if (_recreateMesh || _autoRecreateMesh)
		{
			_recreateMesh = false;
			StartCoroutine(UpdateMeshAfterDelay());
		}
	}

	private void DisposeGMesh()
	{
		_gMesh?.Dispose();
		_gMesh = null;
	}

	private IEnumerator UpdateMeshAfterDelay()
	{
		yield return null;

		UpdateMesh();
	}

	private void UpdateMesh()
	{
		DisposeGMesh();
		_gMesh = CreatePrimitive();

		for (var t = 0; t < _edgeTesselation; t++)
		{
			var edgeCount = _gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				_gMesh.SplitEdgeAndCreateVertex(i);
		}

		if (_logToConsole)
			_gMesh.DebugLogAllElements();

#if GMESH_VALIDATION
		_gMesh.ValidateFaces();
		_gMesh.ValidateLoops();
		_gMesh.ValidateEdges();
		_gMesh.ValidateVertices();
#endif

		_meshFilter.sharedMesh = _gMesh.ToMesh();

		UpdateCentroidMarker();
	}

	private void UpdateCentroidMarker()
	{
		const string markerName = "Centroid Marker";
		if (_centroidMarker == null)
		{
			_centroidMarker = GameObject.Find(markerName)?.transform;
			if (_centroidMarker == null)
			{
				_centroidMarker = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere).transform;
				_centroidMarker.localScale = new float3(0.02f);
				_centroidMarker.parent = transform;
				_centroidMarker.name = markerName;
				_centroidMarker.GetComponent<MeshRenderer>().sharedMaterial = null;
			}
		}

		_centroidMarker.localPosition = _gMesh.CalculateCentroid();
	}

	/*
	private static ProfilerMarker marker = new ProfilerMarker("GMesh update test");
	private void Update()
	{
		marker.Begin();
		using (var gMesh = GMesh.Cube(new GMeshCube(8, 2)))
		{
			_meshFilter.sharedMesh = gMesh.ToMesh();
		}
		marker.End();
		
		//Debug.Log("update");
	}
	*/

	private GMesh CreatePrimitive() => _primitiveType switch
	{
		PrimitiveType.Triangle => GMesh.Triangle(_polygonScale),
		PrimitiveType.Quad => GMesh.Quad(_polygonScale),
		PrimitiveType.Polygon => GMesh.Polygon(_polygonVertexCount, _polygonScale),
		PrimitiveType.Plane => GMesh.Plane(_planeParameters),
		PrimitiveType.Cube => GMesh.Cube(_cubeParameters),
		_ => throw new NotSupportedException(_primitiveType.ToString()),
	};

	private enum PrimitiveType
	{
		Triangle,
		Quad,
		Polygon,
		Plane,
		Cube,
	}
}