#if UNITY_EDITOR
using UnityEditor;
class BuildScript
{
	static void CreateCSProj()
	{
		EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
	}
}
#endif