using Sandbox;
using System.Collections.Specialized;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public sealed class CameraMovement : Component
{
	//Properties
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public float Distance { get; set; } = 150f;
	[Property] public float AltDistance { get; set; } = 0f;
	[Property] public bool CanSwitchView { get; set; } = true;
	[Property] public float SideDistance { get; set; } = 15f;

	//Variables
	public bool IsFirstPerson => Distance == 0f;
	private Vector3 CurrentOffset = Vector3.Zero;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;
	private float StartingDistance;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
		StartingDistance = Distance;
		Distance = StartingDistance;
	}

	protected override void OnUpdate()
	{
		var eyeAngles = Head.Transform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89f, 89f );
		Head.Transform.Rotation = eyeAngles.ToRotation();

		var targetOffset = Vector3.Zero;
		if ( Player.IsCrouching ) targetOffset += Vector3.Down * 32f;
		CurrentOffset = Vector3.Lerp( CurrentOffset, targetOffset, Time.Delta * 10f );

		if( Input.Pressed( "View" ) && CanSwitchView )
		{
			if ( Distance == StartingDistance )
			{
				Distance = AltDistance;
			}
			else
			{
				Distance = StartingDistance;
			}
		}

		if (Camera is not null)
		{
			var camPos = Head.Transform.Position + CurrentOffset;
			if (!IsFirstPerson)
			{
				var camForward = eyeAngles.ToRotation().Forward;
				var camRight = eyeAngles.ToRotation().Right;
				var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * Distance) - (camRight * -SideDistance)).WithoutTags( "player", "trigger" ).Size(5).Run();

				if (camTrace.Hit)
				{
					camPos = camTrace.HitPosition;
				}
				else
				{
					camPos = camTrace.EndPosition;
				}
				

				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}
