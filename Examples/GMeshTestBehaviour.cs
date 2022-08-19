using CodeSmile.GMesh;
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GMeshTestBehaviour : MonoBehaviour
{
	[SerializeField] private bool _recreateMesh;
	[SerializeField] private bool _autoRecreateMesh;

	[Header("Primitive Parameters")]
	[SerializeField] private PrimitiveType _primitiveType;
	[SerializeField] private PlaneParameters _planeParameters = new();
	[SerializeField] private CubeParameters _cubeParameters = new();

	[Header("Debug")]
	public bool _logToConsole;
	public GMesh.DebugDrawElements _debugDrawElements;

	private int _prevTriangulationApproach;
	private int _prevVertexCount;
	private PrimitiveType _prevPrimitiveType;

	//private PlaneParameters _planeParameters = new();
	private MeshFilter _meshFilter;
	private GMesh _gMesh;

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

		_gMesh.DebugDrawGizmos(transform, _debugDrawElements);
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
		if (_gMesh != null)
		{
			_gMesh.Dispose();
			_gMesh = null;
		}
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
		
		if (_logToConsole)
			_gMesh.DebugLogAllElements();
		
		_meshFilter.sharedMesh = _gMesh.ToMesh();
	}

	private GMesh CreatePrimitive() => _primitiveType switch
	{
		PrimitiveType.Plane => Primitives.Plane(_planeParameters),
		PrimitiveType.Cube => Primitives.Cube(_cubeParameters),
		_ => throw new NotSupportedException(_primitiveType.ToString()),
	};

	private enum PrimitiveType
	{
		Plane,
		Cube,
	}
}