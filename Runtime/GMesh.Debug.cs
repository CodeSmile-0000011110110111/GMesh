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
		public override string ToString() => $"{GetType().Name} with {FaceCount} faces, {EdgeCount} edges, {VertexCount} vertices";

		/// <summary>
		/// Dump all elements for debugging purposes.
		/// </summary>
		public void DebugLogAllElements(string headerMessage = "")
		{
			if (string.IsNullOrWhiteSpace(headerMessage) == false)
				Debug.Log(headerMessage);

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

		/// <summary>
		/// Draws vertex gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawVertexGizmos(float3 localPosition, GUIStyle style, float lineThickness = 5f)
		{
			var pos = localPosition;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			foreach (var v in Vertices)
			{
				if (v.IsValid == false)
					continue;

				Handles.Label(pos + v.Position, v.Index.ToString(), style);

				var edgeCenter = CalculateEdgeCenter(v.BaseEdgeIndex);
				Handles.DrawBezier(pos + v.Position, pos + edgeCenter, pos + v.Position, pos + edgeCenter, lineColor, null, lineThickness);
			}
		}

		/// <summary>
		/// Draws edge gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawEdgeGizmos(float3 localPosition, GUIStyle style, float lineThickness = 2f)
		{
			var pos = localPosition;
			var prevNextFontSizeOffset = 4;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			foreach (var e in Edges)
			{
				if (e.IsValid == false)
					continue;

				var v0 = GetVertex(e.Vertex0Index).Position;
				var v1 = GetVertex(e.Vertex1Index).Position;
				Handles.DrawBezier(v0 + pos, v1 + pos, v0 + pos, v1 + pos, lineColor, null, lineThickness);

				var edgeCenter = CalculateCenter(v0, v1);
				Handles.Label(edgeCenter + pos, e.Index.ToString(), style);
			}
		}

		/// <summary>
		/// Draws edge cycle (prev/next) gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawEdgeCycleGizmos(float3 localPosition, GUIStyle style, float lineThickness = 2f)
		{
			var pos = localPosition;
			var prevNextFontSizeOffset = 4;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			foreach (var e in Edges)
			{
				if (e.IsValid == false)
					continue;

				var v0PrevEdge = GetEdge(e.V0PrevEdgeIndex);
				var v0NextEdge = GetEdge(e.V0NextEdgeIndex);
				var v1NextEdge = GetEdge(e.V1NextEdgeIndex);
				var v1PrevEdge = GetEdge(e.V1PrevEdgeIndex);

				var v0 = GetVertex(e.Vertex0Index).Position;
				var v1 = GetVertex(e.Vertex1Index).Position;
				var edgeDir = v1 - v0;

				var edgeCutOff = 0.22f;
				var mainEdgeV0 = v0 + edgeDir * edgeCutOff;
				var mainEdgeV1 = v1 - edgeDir * edgeCutOff;
				Handles.DrawBezier(mainEdgeV0 + pos, mainEdgeV1 + pos, mainEdgeV0 + pos, mainEdgeV1 + pos, lineColor, null, lineThickness);
				var edgeCenter = CalculateCenter(v0, v1);
				Handles.Label(edgeCenter + pos, e.Index.ToString(), style);

				var toV0Prev = mainEdgeV0 + (CalculateEdgeCenter(v0PrevEdge) - mainEdgeV0) * edgeCutOff;
				var toV0Next = mainEdgeV0 + (CalculateEdgeCenter(v0NextEdge) - mainEdgeV0) * edgeCutOff;
				Handles.DrawBezier(mainEdgeV0 + pos, toV0Prev + pos, mainEdgeV0 + pos, toV0Prev + pos, lineColor, null, lineThickness);
				Handles.DrawBezier(mainEdgeV0 + pos, toV0Next + pos, mainEdgeV0 + pos, toV0Next + pos, lineColor, null, lineThickness);

				var toV1Prev = mainEdgeV1 + (CalculateEdgeCenter(v1PrevEdge) - mainEdgeV1) * edgeCutOff;
				var toV1Next = mainEdgeV1 + (CalculateEdgeCenter(v1NextEdge) - mainEdgeV1) * edgeCutOff;
				Handles.DrawBezier(mainEdgeV1 + pos, toV1Prev + pos, mainEdgeV1 + pos, toV1Prev + pos, lineColor, null, lineThickness);
				Handles.DrawBezier(mainEdgeV1 + pos, toV1Next + pos, mainEdgeV1 + pos, toV1Next + pos, lineColor, null, lineThickness);

				style.fontSize += -prevNextFontSizeOffset;
				var textEdgeV0 = v0 + edgeDir * edgeCutOff * .5f;
				var textEdgeV1 = v1 - edgeDir * edgeCutOff * .5f;
				Handles.Label(textEdgeV0 + pos, "V0", style);
				Handles.Label(textEdgeV1 + pos, "V1", style);
				Handles.Label(toV0Prev + pos, $"<{e.V0PrevEdgeIndex}", style);
				Handles.Label(toV0Next + pos, $"{e.V0NextEdgeIndex}>", style);
				Handles.Label(toV1Prev + pos, $"<{e.V1PrevEdgeIndex}", style);
				Handles.Label(toV1Next + pos, $"{e.V1NextEdgeIndex}>", style);
				style.fontSize += prevNextFontSizeOffset;
			}
		}

		/// <summary>
		/// Draws loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawLoopGizmos(float3 localPosition, GUIStyle style, float lineThickness = 2f)
		{
			var pos = localPosition;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
			var loopBulge = 0.2f;
			var prevNextFontSizeOffset = 4;

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
						var toCenter = vStart + (centroid - vStart) * .25f;
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
		/// Draws radial loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawRadialLoopCycleGizmos(float3 localPosition, GUIStyle style, float lineThickness = 2f)
		{
			throw new NotImplementedException();
			var pos = localPosition;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
			var loopBulge = 0.2f;
			var prevNextFontSizeOffset = 4;

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
		/// <param name="localPosition"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DrawFaceGizmos(float3 localPosition, GUIStyle style, float lineThickness = 3f)
		{
			var pos = localPosition;
			var txColor = style.normal.textColor;
			var lineDarken = 0.5f;
			var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

			foreach (var f in Faces)
			{
				if (f.IsValid == false)
					continue;

				var centroid = CalculateFaceCentroid(f);
				var firstLoop = GetLoop(f.FirstLoopIndex);
				var vertex = GetVertex(firstLoop.VertexIndex).Position;

				Handles.DrawBezier(centroid + pos, vertex + pos, centroid + pos, vertex + pos, lineColor, null, lineThickness);
				Handles.Label(centroid + pos, $"{f.Index}", style);
			}
		}
	}
}