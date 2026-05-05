// ─────────────────────────────────────────────────────────────────────────────
// PromptModelReference_Stub.cs
//
// This stub exists because the real PromptModelReference.cs inside
// com.unity.ai.assistant@... is being skipped by Unity due to a GUID conflict
// with the mcp-unity package.  Without the type definition the compiler cannot
// build MeshGenerator.cs, which blocks ALL scripts in the project.
//
// This minimal Editor-only partial class satisfies the compiler while the real
// package implementation is unavailable.  It will be superseded automatically
// if/when the GUID conflict is resolved or the package is updated.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Unity.AI.Mesh.Components
{
    /// <summary>Stub – mirrors the real PromptModelReference from com.unity.ai.assistant.</summary>
    [UxmlElement]
    partial class PromptModelReference : VisualElement { }
}
#endif
