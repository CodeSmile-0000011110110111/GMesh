using CodeSmile;
using CodeSmile.GraphMesh;
using System;
using System.Collections;
using System.Collections.Generic;
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
	[SerializeField] private GMeshVoxPlane _voxPlaneParameters = new();
	[SerializeField] private GMeshCube _cubeParameters = new();

	[Header("Euler Operators")]
	[Range(0, 9)] [SerializeField] private int _edgeTesselation;

	[Header("Debug")]
	[SerializeField] private bool _logToConsole;
	[SerializeField] private bool _logAsSingleLine;
	[SerializeField] private GMesh.DebugDraw _debugDraw = 0;
	[Range(6, 24)] [SerializeField] private int _debugLabelFontSize = 12;

	[Header("Manual Changes")]
	[SerializeField] private List<SplitEdgeData> _splitEdges;

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

		_gMesh.DebugDrawGizmos(transform, _debugDraw, _debugLabelFontSize);
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

		if (_primitiveType != PrimitiveType.VoxPlane)
		{
			for (var t = 0; t < _edgeTesselation; t++)
			{
				var edgeCount = _gMesh.ValidEdgeCount;
				for (var i = 0; i < edgeCount; i++)
					_gMesh.SplitEdgeAndCreateVertex(i);
			}
		}
		else
		{
			foreach (var split in _splitEdges)
			{
				_gMesh.SplitEdgeAtVertex(split.EdgeIndex, split.ExistingVertexIndex);
			}
		}

		if (_logToConsole)
			_gMesh.DebugLogAllElements("", _logAsSingleLine);

#if GMESH_VALIDATION
		_gMesh.ValidateFaces();
		_gMesh.ValidateLoops();
		_gMesh.ValidateEdges();
		_gMesh.ValidateVertices();
#endif

		var mesh = _gMesh.ToMesh();
		mesh.name = _gMesh.ToString();
		_meshFilter.sharedMesh = mesh;

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

	private GMesh CreatePrimitive() => _primitiveType switch
	{
		PrimitiveType.Triangle => GMesh.Triangle(_polygonScale),
		PrimitiveType.Quad => GMesh.Quad(_polygonScale),
		PrimitiveType.Polygon => GMesh.Polygon(_polygonVertexCount, _polygonScale),
		PrimitiveType.Plane => GMesh.Plane(_planeParameters),
		PrimitiveType.VoxPlane => GMesh.VoxPlane(_voxPlaneParameters),
		PrimitiveType.Cube => GMesh.Cube(_cubeParameters),
		_ => throw new NotSupportedException(_primitiveType.ToString()),
	};

	[Serializable]
	private struct SplitEdgeData
	{
		public int EdgeIndex;
		public int ExistingVertexIndex;
	}
	
	private enum PrimitiveType
	{
		Triangle,
		Quad,
		Polygon,
		Plane,
		VoxPlane,
		Cube,
	}
}