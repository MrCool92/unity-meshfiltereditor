using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[CustomEditor(typeof(MeshFilter))]
public class MeshFilterEditor : Editor
{
    private static readonly HashSet<(int, int)> edgeCounter = new HashSet<(int, int)>();

    [SerializeField]
    private VisualTreeAsset visualTreeAsset;

    private int previewHeight;
    private int previewWidth;
    private MeshPreview meshPreview;
    private VisualElement previewBox;
    private VisualElement root;
    private MeshFilter meshFilter => (MeshFilter)target;

    public override VisualElement CreateInspectorGUI()
    {
        root = visualTreeAsset.Instantiate();

        var meshObject = root.Q<ObjectField>("Object");
        meshObject.RegisterValueChangedCallback(HandleMeshChanged);

        previewBox = root.Q<VisualElement>("PreviewBox");

        // Mesh preview
        var meshContainer = previewBox.Q<VisualElement>("Mesh");
        var container = new IMGUIContainer();
        container.onGUIHandler += HandleDrawMesshPreview;
        meshContainer.Add(container);
        
        // Settings preview
        var toolbar = root.Q<Toolbar>("Settings");
        var settings = new IMGUIContainer();
        settings.onGUIHandler += HandleDrawSettings;
        meshContainer.Add(settings);
        toolbar.Add(settings);
        
        UpdateAll();

        root.Bind(serializedObject);
        return root;
    }

    private void HandleMeshChanged(ChangeEvent<Object> evt)
    {
        UpdateAll();
    }

    private void UpdateInformation()
    {
        int vertexCount = meshFilter.sharedMesh.vertexCount;
        int faceCount = meshFilter.sharedMesh.triangles.Length / 3;
        int edgeCount = GetEdgeCount(meshFilter.sharedMesh);

        var information = previewBox.Q<VisualElement>("Information");

        var verticesParent = information.Q<VisualElement>("Vertices");
        var label = verticesParent.Q<Label>();
        label.text = $"<b>{vertexCount}</b> Vertices";

        var facesParent = information.Q<VisualElement>("Faces");
        label = facesParent.Q<Label>();
        label.text = $"<b>{faceCount}</b> Faces";

        var edgesParent = information.Q<VisualElement>("Edges");
        label = edgesParent.Q<Label>();
        label.text = $"<b>{edgeCount}</b> Edges";
    }

    private void UpdateAll()
    {
        bool hasMesh = meshFilter.sharedMesh != null;
        previewBox.style.display = hasMesh ? DisplayStyle.Flex : DisplayStyle.None;

        if (hasMesh)
        {
            if (meshPreview == null)
                meshPreview = new MeshPreview(meshFilter.sharedMesh);
            else
                meshPreview.mesh = meshFilter.sharedMesh;

            UpdateInformation();
        }
    }

    private void HandleDrawMesshPreview()
    {
        previewHeight = (int)previewBox.resolvedStyle.height;
        previewWidth = (int)previewBox.resolvedStyle.width;

        DrawMeshPreview();
    }

    private void HandleDrawSettings()
    {
        Rect toolbarRect = GUILayoutUtility.GetRect(previewWidth, 21);

        GUILayout.BeginArea(toolbarRect);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (meshPreview != null)
            meshPreview.OnPreviewSettings();
        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void OnDisable()
    {
        if (meshPreview != null)
        {
            meshPreview.Dispose();
            meshPreview = null;
        }
    }

    private void DrawMeshPreview()
    {
        if (Selection.count != 1)
            return;

        if (meshFilter.sharedMesh == null)
            return;

        if (meshPreview == null)
            meshPreview = new MeshPreview(meshFilter.sharedMesh);

        Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
        meshPreview.OnPreviewGUI(previewRect, new GUIStyle());
    }

    private static int GetEdgeCount(Mesh mesh)
    {
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        int count = edgeCounter.Count;
        edgeCounter.Clear();
        return count;
    }

    private static void AddEdge(int i1, int i2)
    {
        if (i1 < i2)
            edgeCounter.Add((i1, i2));
        else
            edgeCounter.Add((i2, i1));
    }
}