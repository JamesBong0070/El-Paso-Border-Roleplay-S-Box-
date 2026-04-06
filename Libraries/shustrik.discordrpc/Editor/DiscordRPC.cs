using Editor;
using Sandbox;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public static class DiscordRPC
{
	private const string AppId = "1487860863476961310";
	private static NamedPipeClientStream _pipe;

	private static void DefaultStatus()
	{
		UpdateStatus( $"Project: {Project.Current.Config.Title}", "In Editor" );
	}

	[Event( "editor.created", Priority = 100 )]
	public static async void Init( EditorMainWindow mainWindow )
	{
		try
		{
			for ( int i = 0; i <= 9; i++ )
			{
				_pipe = new NamedPipeClientStream( ".", $"discord-ipc-{i}", PipeDirection.InOut );
				try
				{
					_pipe.Connect( 0 );
					break;
				}
				catch
				{
					_pipe.Dispose();
					_pipe = null;
				}
			}

			if ( _pipe == null ) return;

			Send( 0, JsonSerializer.Serialize( new { v = 1, client_id = AppId } ) );

			var res = JsonSerializer.Deserialize<JsonObject>( Read() );

			if ( res["cmd"]?.ToString() == "DISPATCH" )
			{
				Log.Info( "Discord RPC: Connected!" );

				DefaultStatus();
			}
			else
			{
				Log.Error( "Discord RPC: Connected Error" );
			}

		}
		catch ( Exception e ) { Log.Warning( $"Discord RPC Error: {e.Message}" ); }
	}

	[Event( "app.exit" )]
	public static void Exit()
	{
		Log.Info( "Discord RPC: Disconnected!" );
		_pipe.Dispose();
		_pipe = null;
	}

	[Event( "scene.stop" )]
	public static void Stop()
	{
		DefaultStatus();
	}

	[Event( "scene.play" )]
	public static async void Play()
	{
		await Task.Delay( 500 );
		UpdateStatus( $"Project: {Project.Current.Config.Title}", $"Scene: {Game.ActiveScene.Name}" );
	}

	public static void UpdateStatus( string details, string state )
	{
		var payload = new
		{
			cmd = "SET_ACTIVITY",
			args = new
			{
				pid = Environment.ProcessId,
				activity = new
				{
					details = details,
					state = state,
					assets = new
					{
						large_image = "logo"
					}
				}
			},
			nonce = Guid.NewGuid().ToString()
		};
		Send( 1, JsonSerializer.Serialize( payload ) );
	}

	private static void Send( int opcode, string json )
	{
		if ( _pipe?.IsConnected != true ) return;

		try
		{
			byte[] bJson = Encoding.UTF8.GetBytes( json );

			_pipe.Write( BitConverter.GetBytes( opcode ), 0, 4 );
			_pipe.Write( BitConverter.GetBytes( bJson.Length ), 0, 4 );
			_pipe.Write( bJson, 0, bJson.Length );
		}
		catch ( Exception e )
		{
			Log.Error( $"Send failed: {e.Message}" );
		}
	}

	private static string Read()
	{
		if ( _pipe == null || !_pipe.IsConnected ) return null;

		byte[] header = new byte[8];
		int read = _pipe.Read( header, 0, 8 );
		if ( read < 8 ) return null;

		int opcode = BitConverter.ToInt32( header, 0 );
		int length = BitConverter.ToInt32( header, 4 );

		byte[] bJson = new byte[length];
		_pipe.ReadExactly( bJson, 0, length );

		return Encoding.UTF8.GetString( bJson );
	}
}
