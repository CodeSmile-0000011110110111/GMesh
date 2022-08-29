// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		[Flags]
		public enum DebugDraw
		{
			Vertices = 1 << 0,
			Edges = 1 << 1,
			EdgeCycles = 1 << 2,
			Loops = 1 << 3,
			LoopsWithWinding = 1 << 4,
			LoopsWithRelations = 1 << 5,
			LoopCycles = 1 << 6,
			Faces = 1 << 7,

			IndexLabels = 1 << 8,
			HighlightGraphErrors = 1 << 9,

			Default = Vertices | Edges | Faces,
		}

		public override string ToString() =>
			$"{GetType().Name} with {ValidFaceCount} faces, {ValidEdgeCount} edges, {ValidVertexCount} vertices, Pivot: {Pivot}";

		/// <summary>
		/// Dump all elements for debugging purposes.
		/// </summary>
		public void DebugLogAllElements(string headerMessage = "")
		{
			try
			{
				if (string.IsNullOrWhiteSpace(headerMessage) == false)
					Debug.Log(headerMessage);

				Debug.Log(this);
				for (var i = 0; i < ValidFaceCount; i++)
					Debug.Log(GetFace(i));
				for (var i = 0; i < ValidLoopCount; i++)
					Debug.Log(GetLoop(i));
				for (var i = 0; i < ValidEdgeCount; i++)
					Debug.Log(GetEdge(i));
				for (var i = 0; i < ValidVertexCount; i++)
					Debug.Log(GetVertex(i));
			}
			catch (Exception e)
			{
				Debug.LogWarning("DebugLogAllElements: " + e);
			}
		}

		public void DebugDrawGizmos(UnityEngine.Transform transform, DebugDraw debugDraw = DebugDraw.Default)
		{
			var vertColor = Color.cyan;
			var edgeColor = Color.yellow;
			var loopColor = Color.green;
			var loopCyclesColor = new Color(0.2f, .8f, 0.2f, 1f);
			var faceColor = Color.magenta;

			var textStyle = new GUIStyle();
			textStyle.alignment = TextAnchor.UpperCenter;
			textStyle.normal.textColor = vertColor;
			textStyle.fontSize = 12;

			var highlightErrors = debugDraw.HasFlag(DebugDraw.HighlightGraphErrors);

			var drawIndices = debugDraw.HasFlag(DebugDraw.IndexLabels);
			if (debugDraw.HasFlag(DebugDraw.Vertices))
				DebugDrawVertexGizmos(transform, textStyle, drawIndices, highlightErrors);

			textStyle.normal.textColor = edgeColor;
			if (debugDraw.HasFlag(DebugDraw.Edges))
				DebugDrawEdgeGizmos(transform, textStyle, drawIndices, highlightErrors);
			if (debugDraw.HasFlag(DebugDraw.EdgeCycles))
				DebugDrawEdgeCycleGizmos(transform, textStyle, drawIndices, highlightErrors);

			var drawWinding = debugDraw.HasFlag(DebugDraw.LoopsWithWinding);
			var drawRelations = debugDraw.HasFlag(DebugDraw.LoopsWithRelations);
			textStyle.normal.textColor = loopColor;
			if (debugDraw.HasFlag(DebugDraw.Loops))
				DebugDrawLoopGizmos(transform, textStyle, drawIndices, drawWinding, drawRelations, highlightErrors);
			textStyle.normal.textColor = loopCyclesColor;
			if (debugDraw.HasFlag(DebugDraw.LoopCycles))
				DebugDrawLoopCycleGizmos(transform, textStyle, drawIndices, highlightErrors);

			textStyle.normal.textColor = faceColor;
			if (debugDraw.HasFlag(DebugDraw.Faces))
				DebugDrawFaceGizmos(transform, textStyle, drawIndices, highlightErrors);
		}

		/// <summary>
		/// Draws vertex gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawVertexGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false,
			bool highlightErrors = false)
		{
			try
			{
				var lineThickness = 3f;
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
					if (drawIndices)
						Handles.Label(vPos, v.Index.ToString(), style);

					var edgeCenter = math.transform(t, CalculateEdgeCenter(v.BaseEdgeIndex) * scale);
					Handles.DrawBezier(vPos, edgeCenter, vPos, edgeCenter, lineColor, null, lineThickness);
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}

		/// <summary>
		/// Draws edge gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawEdgeGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false, bool highlightErrors = false)
		{
			try
			{
				var lineThickness = 2f;
				var scale = (float3)transform.localScale;
				var t = new RigidTransform(transform.rotation, transform.position);
				var txColor = style.normal.textColor;
				var lineDarken = 0.5f;
				var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

				Gizmos.matrix = transform.localToWorldMatrix;
				foreach (var e in Edges)
				{
					if (e.IsValid == false)
						continue;

					var v0 = math.transform(t, GetVertex(e.AVertexIndex).Position * scale);
					var v1 = math.transform(t, GetVertex(e.OVertexIndex).Position * scale);
					Handles.DrawBezier(v0, v1, v0, v1, lineColor, null, lineThickness);

					if (drawIndices)
					{
						var edgeCenter = CalculateCenter(v0, v1);
						Handles.Label(edgeCenter, e.Index.ToString(), style);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}

		/// <summary>
		/// Draws edge cycle (prev/next) gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawEdgeCycleGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false,
			bool highlightErrors = false)
		{
			try
			{
				var lineThickness = 2f;
				var scale = (float3)transform.localScale;
				var t = new RigidTransform(transform.rotation, transform.position);
				var prevNextFontSizeOffset = 3;
				var txColor = style.normal.textColor;
				var lineDarken = 0.5f;
				var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);

				Gizmos.matrix = transform.localToWorldMatrix;
				foreach (var e in Edges)
				{
					if (e.IsValid == false)
						continue;

					var v0PrevEdge = GetEdge(e.APrevEdgeIndex);
					var v0NextEdge = GetEdge(e.ANextEdgeIndex);
					var v1NextEdge = GetEdge(e.ONextEdgeIndex);
					var v1PrevEdge = GetEdge(e.OPrevEdgeIndex);

					var aVertex = GetVertex(e.AVertexIndex);
					var oVertex = GetVertex(e.OVertexIndex);
					var v0Pos = math.transform(t, aVertex.Position * scale);
					var v1Pos = math.transform(t, oVertex.Position * scale);
					var edgeCutOff = 0.22f;
					var edgeDir = (v1Pos - v0Pos) * edgeCutOff;
					var mainEdgeV0 = v0Pos + edgeDir;
					var mainEdgeV1 = v1Pos - edgeDir;

					string issue = null;
					if (highlightErrors)
					{
						var position = v0Pos;
						var foundIssue = ValidateVertexDiskCycle(aVertex, out issue) == false;
						if (foundIssue == false)
						{
							foundIssue = ValidateVertexDiskCycle(oVertex, out issue) == false;
							if (foundIssue)
								position = v1Pos;
							else
							{
								foundIssue = ValidateEdgeRadialLoopCycle(e, out issue) == false;
								if (foundIssue)
									position = v0Pos + (v1Pos - v0Pos) * 0.5f;
							}
						}

						if (foundIssue)
						{
							lineColor = Color.red;
							var errorStyle = new GUIStyle();
							errorStyle.normal.textColor = Color.red;
							errorStyle.alignment = TextAnchor.MiddleCenter;
							Handles.Label(position, issue, errorStyle);
						}
					}

					Handles.DrawBezier(mainEdgeV0, mainEdgeV1, mainEdgeV0, mainEdgeV1, lineColor, null, lineThickness);

					var toV0Prev = mainEdgeV0 + (math.transform(t, CalculateEdgeCenter(v0PrevEdge) * scale) - mainEdgeV0) * edgeCutOff;
					var toV0Next = mainEdgeV0 + (math.transform(t, CalculateEdgeCenter(v0NextEdge) * scale) - mainEdgeV0) * edgeCutOff;
					Handles.DrawBezier(mainEdgeV0, toV0Prev, mainEdgeV0, toV0Prev, lineColor, null, lineThickness);
					if (e.APrevEdgeIndex != e.ANextEdgeIndex)
						Handles.DrawBezier(mainEdgeV0, toV0Next, mainEdgeV0, toV0Next, lineColor, null, lineThickness);

					var toV1Prev = mainEdgeV1 + (math.transform(t, CalculateEdgeCenter(v1PrevEdge) * scale) - mainEdgeV1) * edgeCutOff;
					var toV1Next = mainEdgeV1 + (math.transform(t, CalculateEdgeCenter(v1NextEdge) * scale) - mainEdgeV1) * edgeCutOff;
					Handles.DrawBezier(mainEdgeV1, toV1Prev, mainEdgeV1, toV1Prev, lineColor, null, lineThickness);
					if (e.OPrevEdgeIndex != e.ONextEdgeIndex)
						Handles.DrawBezier(mainEdgeV1, toV1Next, mainEdgeV1, toV1Next, lineColor, null, lineThickness);

					if (drawIndices)
					{
						style.fontSize += -prevNextFontSizeOffset;
						var textEdgeV0 = v0Pos + edgeDir * edgeCutOff * 5f;
						var textEdgeV1 = v1Pos - edgeDir * edgeCutOff * 5f;
						Handles.Label(textEdgeV0, "V0", style);
						Handles.Label(textEdgeV1, "V1", style);
						if (e.APrevEdgeIndex != e.ANextEdgeIndex)
						{
							Handles.Label(toV0Prev, $"<{e.APrevEdgeIndex}", style);
							Handles.Label(toV0Next, $"{e.ANextEdgeIndex}>", style);
						}
						else
							Handles.Label(toV0Prev, $"<{e.APrevEdgeIndex}>", style);
						if (e.OPrevEdgeIndex != e.ONextEdgeIndex)
						{
							Handles.Label(toV1Prev, $"<{e.OPrevEdgeIndex}", style);
							Handles.Label(toV1Next, $"{e.ONextEdgeIndex}>", style);
						}
						else
							Handles.Label(toV1Prev, $"<{e.OPrevEdgeIndex}>", style);

						var edgeCenter = CalculateCenter(v0Pos, v1Pos);
						Handles.Label(edgeCenter, $"{e.Index} (L{e.BaseLoopIndex})", style);
						style.fontSize += prevNextFontSizeOffset;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}

		/// <summary>
		/// Draws loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawLoopGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false, bool drawWinding = false,
			bool drawRelations = false, bool highlightErrors = false)
		{
			try
			{
				var prevNextFontSizeOffset = 3;
				var lineThickness = 2f;
				var scale = (float3)transform.localScale;
				var t = new RigidTransform(transform.rotation, transform.position);
				var txColor = style.normal.textColor;
				var lineDarken = 0.5f;
				var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
				var loopBulge = 0.2f;

				Gizmos.matrix = transform.localToWorldMatrix;
				foreach (var f in Faces)
				{
					if (f.IsValid == false)
						continue;

					var centroid = math.transform(t, CalculateFaceCentroid(f) * scale);

					if (highlightErrors && ValidateFaceLoopCycle(f, out var issue) == false)
					{
						lineColor = Color.red;
						var errorStyle = new GUIStyle();
						errorStyle.normal.textColor = Color.red;
						errorStyle.alignment = TextAnchor.MiddleCenter;
						Handles.Label(centroid, issue, errorStyle);
					}

					ForEachLoop(f, l =>
					{
						var nextLoop = GetLoop(l.NextLoopIndex);
						var vStartIndex = l.StartVertexIndex;
						var vEndIndex = nextLoop.StartVertexIndex;
						var vStart = math.transform(t, GetVertex(vStartIndex).Position * scale);
						var vEnd = math.transform(t, GetVertex(vEndIndex).Position * scale);

						var tStart = vStart + (centroid - vStart) * loopBulge;
						var tEnd = vEnd + (centroid - vEnd) * loopBulge;
						Handles.DrawBezier(vStart, vEnd, tStart, tEnd, lineColor, null, lineThickness);

						var loopCenter = CalculateCenter(vStart, vEnd);
						loopCenter += (centroid - loopCenter) * loopBulge;
						if (drawIndices)
						{
							// P/N: <{l.PrevLoopIndex}°{l.NextLoopIndex}>, 
							var relations = drawRelations ? $"  E: {l.EdgeIndex}, V: {l.StartVertexIndex}" : "";
							//var randPos = Random.CreateFromIndex(7).NextFloat3Direction() * 0.2f;
							Handles.Label(loopCenter, $"{l.Index}{relations}", style);
						}

						// mark the start of the loop 
						if (f.FirstLoopIndex == l.Index)
						{
							var toCenter = vStart + (centroid - vStart) * .15f;
							Handles.DrawBezier(toCenter, vStart, toCenter, vStart, lineColor, null, lineThickness + 2f);
						}

						if (drawWinding)
						{
							var toNextLineThickness = 1.5f;
							var vStartIndex2 = nextLoop.StartVertexIndex;
							var vEndIndex2 = GetLoop(nextLoop.NextLoopIndex).StartVertexIndex;
							var vStart2 = math.transform(t, GetVertex(vStartIndex2).Position * scale);
							var vEnd2 = math.transform(t, GetVertex(vEndIndex2).Position * scale);
							var loopCenter2 = CalculateCenter(vStart2, vEnd2);
							loopCenter2 += (centroid - loopCenter2) * loopBulge;
							Handles.DrawBezier(loopCenter, loopCenter2, loopCenter, loopCenter2, lineColor, null, toNextLineThickness);

							// small direction indicator pointing to prev loop's start vertex
							var arrowHeadLength = 0.012f;
							var vPrevStart = math.transform(t, GetVertex(GetLoop(l.PrevLoopIndex).StartVertexIndex).Position * scale);
							var vToPrevStart = loopCenter + math.normalize(vPrevStart - loopCenter) * arrowHeadLength;
							Handles.DrawBezier(loopCenter, vToPrevStart, loopCenter, vToPrevStart, lineColor, null, toNextLineThickness);
							var vToStart = loopCenter + math.normalize(vStart - loopCenter) * arrowHeadLength;
							Handles.DrawBezier(loopCenter, vToStart, loopCenter, vToStart, lineColor, null, toNextLineThickness);

							if (drawIndices)
							{
								style.fontSize += -prevNextFontSizeOffset;
								var windCenter = loopCenter + (loopCenter2 - loopCenter) * .1f;
								Handles.Label(windCenter, $"<{nextLoop.PrevLoopIndex}", style);
								style.fontSize += prevNextFontSizeOffset;
							}
						}
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}

		/// <summary>
		/// Draws radial loop gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawLoopCycleGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false,
			bool highlightErrors = false)
		{
			try
			{
				var lineThickness = 1.5f;
				var scale = (float3)transform.localScale;
				var t = new RigidTransform(transform.rotation, transform.position);
				var txColor = style.normal.textColor;
				var lineDarken = 0.5f;
				var lineColor = new Color(txColor.r * lineDarken, txColor.g * lineDarken, txColor.b * lineDarken);
				var loopBulge = 0.33f;
				var prevNextFontSizeOffset = 3;

				Gizmos.matrix = transform.localToWorldMatrix;
				foreach (var f in Faces)
				{
					if (f.IsValid == false)
						continue;

					var centroid = math.transform(t, CalculateFaceCentroid(f) * scale);

					ForEachLoop(f, l =>
					{
						var nextLoop = GetLoop(l.NextLoopIndex);
						var vStart = math.transform(t, GetVertex(l.StartVertexIndex).Position * scale);
						var vEnd = math.transform(t, GetVertex(nextLoop.StartVertexIndex).Position * scale);

						var loopCenter = CalculateCenter(vStart, vEnd);
						loopCenter += (centroid - loopCenter) * loopBulge;
						if (drawIndices)
						{
							style.fontSize += -prevNextFontSizeOffset;
							Handles.Label(loopCenter, $"<{l.PrevRadialLoopIndex} ° {l.NextRadialLoopIndex}>", style);
							style.fontSize += prevNextFontSizeOffset;
						}

						if (l.NextRadialLoopIndex != l.Index)
						{
							var lOpposite = GetLoop(l.NextRadialLoopIndex);
							var vOppStart = math.transform(t, GetVertex(lOpposite.StartVertexIndex).Position * scale);
							var vOppEnd = math.transform(t, GetVertex(GetLoop(lOpposite.NextLoopIndex).StartVertexIndex).Position * scale);
							//var vOppCenter = math.transform(t, CalculateCenter(vOppStart, vOppEnd) * scale);
							var oppLoopCenter = CalculateCenter(vOppStart, vOppEnd);
							var oppFaceCentroid = math.transform(t, CalculateFaceCentroid(lOpposite.FaceIndex) * scale);
							oppLoopCenter += (oppFaceCentroid - oppLoopCenter) * loopBulge;

							Handles.DrawBezier(loopCenter, oppLoopCenter, loopCenter, oppLoopCenter, lineColor, null, lineThickness);
						}
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}

		/// <summary>
		/// Draws face gizmos. Must be called from OnDrawGizmos().
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="style"></param>
		/// <param name="lineThickness"></param>
		public void DebugDrawFaceGizmos(UnityEngine.Transform transform, GUIStyle style, bool drawIndices = false, bool highlightErrors = false)
		{
			try
			{
				var lineThickness = 3f;
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
					var vertex = math.transform(t, GetVertex(firstLoop.StartVertexIndex).Position * scale);

					Handles.DrawBezier(centroid, vertex, centroid, vertex, lineColor, null, lineThickness);
					if (drawIndices)
						Handles.Label(centroid, $"{f.Index}", style);
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("DrawGizmos: " + e);
			}
		}


	}
}