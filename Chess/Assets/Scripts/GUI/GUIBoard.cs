using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Board))]
public class GUIBoard : Editor
{
    float height = 25f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Board board = (Board)target;

        GUILayout.Space(10f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Create", GUILayout.Height(height)))
            board.CreateFields(board.Rows, board.Columns);
        if (GUILayout.Button("Destroy", GUILayout.Height(height)))
            board.DestroyFields();
        GUILayout.EndHorizontal();
        /*
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Height(height)))
            board.LoadBoard();
        if (GUILayout.Button("Save", GUILayout.Height(height)))
            board.SaveBoard();
        GUILayout.EndHorizontal();
        */
    }
}
