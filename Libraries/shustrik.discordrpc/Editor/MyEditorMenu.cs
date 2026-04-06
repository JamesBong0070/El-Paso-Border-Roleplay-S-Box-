using Editor;

public static class MyEditorMenu
{
	[Menu( "Editor", "Discord RPC/Info" )]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog( "Info", "It's a simple addon for Discord Rich Presence" );
	}
}
