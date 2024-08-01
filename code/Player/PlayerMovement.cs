using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovement : Component
{
	//Movement Properties
	[Property] public float GroundControl {get; set;} = 4.0f;
	[Property] public float AirControl {get; set;} = 0.1f;
	[Property] public float MaxForce {get; set;} = 50f;
	[Property] public float Speed {get; set;} = 180f;
	[Property] public float RunSpeed {get; set;} = 280f;
	[Property] public float WalkSpeed { get; set; } = 95f;
	[Property] public float CrouchSpeed {get; set;} = 90f;
	[Property] public float JumpForce {get; set;} = 325f;

	//Object References
	[Property] public GameObject Head {get; set;}
	[Property] public GameObject Body {get; set;}
	[Property] public GameObject MoveRot { get; set; }

	//Memeber Variables
	public Vector3 WishVelocity = Vector3.Zero;
	public bool IsCrouching = false;
	public bool IsSprinting = false;
	public bool IsWalking = false;
	private CharacterController characterController;
	private CitizenAnimationHelper animationHelper;


	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
	}

	protected override void OnUpdate()
	{
		UpdateCrouch();
		IsSprinting = Input.Down( "Run" );
		if ( IsSprinting )
		{
			IsWalking = false;
		}
		else
		{
			IsWalking = Input.Down( "Walk" );
		}
		if ( Input.Pressed( "Jump" ) ) Jump();

		RotateBody();
		UpdateAnimations();
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;

		var rot = Head.Transform.Rotation;
		MoveRot.Transform.Rotation = new Angles( 0, rot.Yaw(), 0 ).ToRotation();
		var newrot = MoveRot.Transform.Rotation;

		if ( Input.Down( "Forward" ) ) WishVelocity += newrot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += newrot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += newrot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += newrot.Right;

		if ( ! WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;
		if ( IsCrouching ) WishVelocity *= CrouchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else if ( IsWalking ) WishVelocity *= WalkSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		var gravity = Scene.PhysicsWorld.Gravity;

		if (characterController.IsOnGround)
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength(MaxForce) );
			characterController.ApplyFriction( AirControl );
		}

		characterController.Move();

		if (!characterController.IsOnGround)
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
		float rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

		if ( characterController.Velocity.Length > 10f)
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 10f );
		}
		else if ( rotateDifference > 65f)
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 2f );
		}
	}

	void Jump()
	{
		if ( !characterController.IsOnGround ) return;

		characterController.Punch( Vector3.Up * JumpForce );
		animationHelper.TriggerJump();
	}

	void UpdateAnimations()
	{
		if ( animationHelper is null ) return;

		animationHelper.WithWishVelocity( WishVelocity );
		animationHelper.WithVelocity( characterController.Velocity );
		animationHelper.AimAngle = Head.Transform.Rotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
		if (IsWalking)
		{
			animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Walk;
		}
		else
		{
			animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		}
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	void UpdateCrouch()
	{
		if ( characterController is null ) return;

		if( Input.Pressed( "Duck" ) && !IsCrouching )
		{
			IsCrouching = true;
			characterController.Height /= 1.5f;
		}

		if ( Input.Released( "Duck" ) && IsCrouching )
		{
			IsCrouching = false;
			characterController.Height *= 1.5f;
		}
	}
}
