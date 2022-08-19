// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		[Flags]
		public enum DebugDrawElements
		{
			Vertices = 1 << 0,
			Edges = 1 << 1,
			Loops = 1 << 2,
			Faces = 1 << 3,
			EdgeCycles = 1 << 4,
			LoopCycles = 1 << 5,
			
			Default = Vertices | Edges | Faces,
		}

		public override string ToString() => $"{GetType().Name} with {FaceCount} faces, {EdgeCount} edges, {VertexCount} vertices, Pivot: {Pivot}, Centroid: {CalculateCentroid()}";

		/// <summary>
		/// Dump all elements for debugging purposes.
		/// </summary>
		public void DebugLogAllElements(string headerMessage = "")
		{
			if (string.IsNullOrWhiteSpace(headerMessage) == false)
				Debug.Log(headerMessage);

			var f = new float3();
			f += new float3(1, 2, 3) / 1f;
			f += new float3(1, 2, 3) / 2f;
			f += new float3(1, 2, 3) / 3f;

			var f2 = (new float3(1, 2, 3) + new float3(1, 2, 3) + new float3(1, 2, 3)) / 3f;
			
			Debug.Log($"same? {f} vs {f2}");

			Debug.Log(this);
			for (var i = 0; i < FaceCount; i++)
				Debug.Log(GetFace(i));
			for (var i = 0; i < LoopCount; i++)
				Debug.Log(GetLoop(i));
			for (var i = 0; i < EdgeCount; i++)
				Debug.Log(GetEdge(i));
			for (var i = 0; i < VertexCount; i++)
				Debug.Log(GetVertex(i));
		}

		public void DebugDrawGizmos(Transform transform, DebugDrawElements drawElements = DebugDrawElements.Default)
		{
			var vertColor = Color.cyan;
			var edgeColor = Color.yellow;
			var loopColor = Color.green;
			var faceColor = Color.red;

			var textStyle = new GUIStyle();
			textStyle.alignment = TextAnchor.UpperCenter;
			textStyle.normal.textColor = vertColor;
			textStyle.fontSize = 14;

			if (drawElements.HasFlag(DebugDrawElements.Vertices))
				DebugDrawVertexGizmos(transform, textStyle);

			textStyle.normal.textColor = edgeColor;
			if (drawElements.HasFlag(DebugDrawElements.Edges))
				DebugDrawEdgeGizmos(transform, textStyle);
			if (drawElements.HasFlag(DebugDrawElements.EdgeCycles))
				DebugDrawEdgeCycleGizmos(transform, textStyle);

			textStyle.normal.textColor = loopColor;
			if (drawElements.HasFlag(DebugDrawElements.Loops))
				DebugDrawLoopGizmos(transform, textStyle);
			if (drawElements.HasFlag(DebugDrawElements.LoopCycles))
				DebugDrawLoopCycleGizmos(transform, textStyle);

			textStyle.normal.textColor = faceColor;
			if (drawElements.HasFlag(DebugDrawElements.Faces))
				DebugDrawFaceGizmos(transform, textStyle);
		}

		/// <summary>
		/// Draws vertex gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawVertexGizmos(Transform transform, GUIStyle style, float lineThickness = 5f)
		{
			var scale = (float3)transform.localScale;
			var t = new RigidTransform(transform.rotation, transform.position);
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var v in Vertices)
			{
				if (v.IsValid == false)
					continue;

				var vPos = math.transform(t, v.Position * scale);
				Handles.Label(vPos, v.Index.ToString(), style);

				var edgeCenter = math.transform(t, CalculateEdgeCenter(v.BaseEdgeIndex) * scale);
				Handles.DrawBezier(vPos, edgeCenter, vPos, edgeCenter, lineColor, null, lineThickness);
			}
		}

		/// <summary>
		/// Draws edge gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawEdgeGizmos(Transform transform, GUIStyle style, float lineThickness = 2f)
		{
			var scale = (float3)transform.localScale;
			var t = new RigidTransform(transform.rotation, transform.position);
			var prevNextFontSizeOffset = 4;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var e in Edges)
			{
				if (e.IsValid == false)
					continue;

				var v0 = math.transform(t, GetVertex(e.Vertex0Index).Position * scale);
				var v1 = math.transform(t, GetVertex(e.Vertex1Index).Position * scale);
				Handles.DrawBezier(v0, v1, v0, v1, lineColor, null, lineThickness);

				var edgeCenter = CalculateCenter(v0, v1);
				Handles.Label(edgeCenter, e.Index.ToString(), style);
			}
		}

		/// <summary>
		/// Draws edge cycle (prev/next) gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawEdgeCycleGizmos(Transform transform, GUIStyle style, float lineThickness = 2f)
		{
			var scale = (float3)transform.localScale;
			var t = new RigidTransform(transform.rotation, transform.position);
			var prevNextFontSizeOffset = 4;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var e in Edges)
			{
				if (e.IsValid == false)
					continue;

				var v0PrevEdge = GetEdge(e.V0PrevEdgeIndex);
				var v0NextEdge = GetEdge(e.V0NextEdgeIndex);
				var v1NextEdge = GetEdge(e.V1NextEdgeIndex);
				var v1PrevEdge = GetEdge(e.V1PrevEdgeIndex);

				var v0Pos = math.transform(t, GetVertex(e.Vertex0Index).Position * scale);
				var v1Pos = math.transform(t, GetVertex(e.Vertex1Index).Position * scale);

				var edgeCutOff = 0.22f;
				var edgeDir = (v1Pos - v0Pos) * edgeCutOff;
				var mainEdgeV0 = v0Pos + edgeDir;
				var mainEdgeV1 = v1Pos - edgeDir;
				Handles.DrawBezier(mainEdgeV0, mainEdgeV1, mainEdgeV0, mainEdgeV1, lineColor, null, lineThickness);
				var edgeCenter = CalculateCenter(v0Pos, v1Pos);
				Handles.Label(edgeCenter, e.Index.ToString(), style);

				var toV0Prev = mainEdgeV0 + (math.transform(t, CalculateEdgeCenter(v0PrevEdge) * scale) - mainEdgeV0) * edgeCutOff;
				var toV0Next = mainEdgeV0 + (math.transform(t, CalculateEdgeCenter(v0NextEdge) * scale) - mainEdgeV0) * edgeCutOff;
				Handles.DrawBezier(mainEdgeV0, toV0Prev, mainEdgeV0, toV0Prev, lineColor, null, lineThickness);
				Handles.DrawBezier(mainEdgeV0, toV0Next, mainEdgeV0, toV0Next, lineColor, null, lineThickness);

				var toV1Prev = mainEdgeV1 + (math.transform(t, CalculateEdgeCenter(v1PrevEdge) * scale) - mainEdgeV1) * edgeCutOff;
				var toV1Next = mainEdgeV1 + (math.transform(t, CalculateEdgeCenter(v1NextEdge) * scale) - mainEdgeV1) * edgeCutOff;
				Handles.DrawBezier(mainEdgeV1, toV1Prev, mainEdgeV1, toV1Prev, lineColor, null, lineThickness);
				Handles.DrawBezier(mainEdgeV1, toV1Next, mainEdgeV1, toV1Next, lineColor, null, lineThickness);

				style.fontSize += -prevNextFontSizeOffset;
				var textEdgeV0 = v0Pos + edgeDir * edgeCutOff * 3f;
				var textEdgeV1 = v1Pos - edgeDir * edgeCutOff * 3f;
				Handles.Label(textEdgeV0, "V0", style);
				Handles.Label(textEdgeV1, "V1", style);
				Handles.Label(toV0Prev, $"<{e.V0PrevEdgeIndex}", style);
				Handles.Label(toV0Next, $"{e.V0NextEdgeIndex}>", style);
				Handles.Label(toV1Prev, $"<{e.V1PrevEdgeIndex}", style);
				Handles.Label(toV1Next, $"{e.V1NextEdgeIndex}>", style);
				style.fontSize += prevNextFontSizeOffset;
			}
		}

		/// <summary>
		/// Draws loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawLoopGizmos(Transform transform, GUIStyle style, float lineThickness = 2f)
		{
			var scale = (float3)transform.localScale;
			var t = new RigidTransform(transform.rotation, transform.position);
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
			var loopBulge = 0.2f;
			var prevNextFontSizeOffset = 4;

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var f in Faces)
			{
				if (f.IsValid == false)
					continue;

				var centroid = math.transform(t, CalculateFaceCentroid(f) * scale);

				ForEachLoop(f, l =>
				{
					var e = GetEdge(l.EdgeIndex);
					var vStartIndex = l.VertexIndex;
					var vEndIndex = e.GetOtherVertexIndex(l.VertexIndex);
					var vStart = math.transform(t, GetVertex(vStartIndex).Position * scale);
					var vEnd = math.transform(t, GetVertex(vEndIndex).Position * scale);

					var tStart = vStart + (centroid - vStart) * loopBulge;
					var tEnd = vEnd + (centroid - vEnd) * loopBulge;
					Handles.DrawBezier(vStart, vEnd, tStart, tEnd, lineColor, null, lineThickness);

					var loopCenter = CalculateCenter(vStart, vEnd);
					loopCenter += (centroid - loopCenter) * loopBulge;
					Handles.Label(loopCenter, $"{l.Index}", style);

					// mark the start of the loop 
					if (f.FirstLoopIndex == l.Index)
					{
						var toCenter = vStart + (centroid - vStart) * .25f;
						Handles.DrawBezier(toCenter, vStart, toCenter, vStart, lineColor, null, lineThickness + 4f);
					}

					// TODO: draw prev/next?
					/*
					var aboveCenter = vStart + (vEnd - vStart) * (1f-loopBulge);
					aboveCenter += (centroid - loopCenter) * loopBulge;
					var belowCenter = vStart + (vEnd - vStart) * loopBulge;
					belowCenter += (centroid - loopCenter) * loopBulge;
					style.fontSize += -prevNextFontSizeOffset;
					Handles.Label(belowCenter + pos, $"<{l.PrevLoopIndex}", style);
					Handles.Label(aboveCenter + pos, $"{l.NextLoopIndex}>", style);
					style.fontSize += prevNextFontSizeOffset;
					*/
				});
			}
		}

		/// <summary>
		/// Draws radial loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawLoopCycleGizmos(Transform transform, GUIStyle style, float lineThickness = 2f)
		{
			throw new NotImplementedException();

			var pos = (float3)transform.localPosition;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
			var loopBulge = 0.2f;
			var prevNextFontSizeOffset = 4;

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var f in Faces)
			{
				if (f.IsValid == false)
					continue;

				var centroid = CalculateFaceCentroid(f);

				ForEachLoop(f, l =>
				{
					var e = GetEdge(l.EdgeIndex);
					var vStartIndex = l.VertexIndex;
					var vEndIndex = e.GetOtherVertexIndex(l.VertexIndex);
					var vStart = GetVertex(vStartIndex).Position;
					var vEnd = GetVertex(vEndIndex).Position;

					var tStart = vStart + (centroid - vStart) * loopBulge;
					var tEnd = vEnd + (centroid - vEnd) * loopBulge;
					Handles.DrawBezier(vStart + pos, vEnd + pos, tStart + pos, tEnd + pos, lineColor, null, lineThickness);

					var loopCenter = CalculateCenter(vStart, vEnd);
					loopCenter += (centroid - loopCenter) * loopBulge;
					Handles.Label(loopCenter + pos, $"{l.Index}", style);

					if (f.FirstLoopIndex == l.Index)
					{
						var toCenter = CalculateCenter(vStart, centroid) * .2f;
						Handles.DrawBezier(toCenter + pos, vStart + pos, toCenter + pos, vStart + pos, lineColor, null, lineThickness + 4f);
					}

					/*
					var aboveCenter = vStart + (vEnd - vStart) * (1f-loopBulge);
					aboveCenter += (centroid - loopCenter) * loopBulge;
					var belowCenter = vStart + (vEnd - vStart) * loopBulge;
					belowCenter += (centroid - loopCenter) * loopBulge;
					style.fontSize += -prevNextFontSizeOffset;
					Handles.Label(belowCenter + pos, $"<{l.PrevLoopIndex}", style);
					Handles.Label(aboveCenter + pos, $"{l.NextLoopIndex}>", style);
					style.fontSize += prevNextFontSizeOffset;
					*/
				});
			}
		}

		/// <summary>
		/// Draws face gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawFaceGizmos(Transform transform, GUIStyle style, float lineThickness = 3f)
		{
			var scale = (float3)transform.localScale;
			var t = new RigidTransform(transform.rotation, transform.position);
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var f in Faces)
			{
				if (f.IsValid == false)
					continue;

				var centroid = math.transform(t, CalculateFaceCentroid(f) * scale);
				var firstLoop = GetLoop(f.FirstLoopIndex);
				var vertex = math.transform(t, GetVertex(firstLoop.VertexIndex).Position * scale);

				Handles.DrawBezier(centroid, vertex, centroid, vertex, lineColor, null, lineThickness);
				Handles.Label(centroid, $"{f.Index}", style);
			}
		}
	}
}