using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;

    // Referencias de componentes
    private CharacterController characterController;
    private Camera playerCamera;
    private PlayerBaseState currentState;
    

    // Variables de c�mara
    private float verticalRotation = 0f;
    private float bobTimer = 0f;

    // Variables de movimiento
    private Vector3 playerVelocity;

    //Estados
    public PlayerIdleState IdleState { get; private set; }
    public PlayerSprintState SprintState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }

    private void Awake()
    {
        // Obtener referencias
        characterController = GetComponent<CharacterController>();

        if (playerData.cameraTransform != null)
        {
            playerCamera = playerData.cameraTransform.GetComponent<Camera>();
        }
        else
        {
            Debug.LogError("Camera Transform no est� asignado en PlayerData!");
        }

        // Configuraci�n inicial de sensibilidad
        UpdateInputType(Gamepad.current != null);

        // Configurar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Inicializar estados
        IdleState = new PlayerIdleState(this, playerData);
        SprintState = new PlayerSprintState(this, playerData);
        MoveState = new PlayerMoveState(this, playerData);

        // Estado inicial
        ChangeState(IdleState);

        // Guardar la posici�n original de la c�mara
        if (playerData.cameraTransform != null)
        {
            playerData.originalCameraPosition = playerData.cameraTransform.localPosition;
        }
    }

    #region UPDATES
    private void Update()
    {
        // Verificar cambios en el tipo de input
        CheckInputTypeChange();

        UpdateGroundCheck();

        // Primero actualizamos la rotaci�n
        UpdateRotation();

        // Luego el headbob
        UpdateHeadBob();

        // Finalmente el movimiento
        UpdateMovement();

        // Actualizar el estado actual
        currentState?.Update();
    }

    private void UpdateGroundCheck()
    {
        playerData.isGrounded = Physics.CheckSphere(
            playerData.groundCheck.position,
            playerData.groundCheckRadius,
            playerData.groundLayer
        );

        if (playerData.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += Physics.gravity.y * Time.deltaTime;
    }

    private void UpdateMovement()
    {
        if (!playerData.canMove) return;

        Vector3 move = transform.right * playerData.moveDirection.x +
                      transform.forward * playerData.moveDirection.z;

        // Usar currentSpeed en lugar de walkSpeed
        characterController.Move(move * playerData.currentSpeed * Time.deltaTime);
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    private void CheckInputTypeChange()
    {
        bool isGamepadNowConnected = Gamepad.current != null;
        if (playerData.isUsingGamepad != isGamepadNowConnected)
        {
            UpdateInputType(isGamepadNowConnected);
        }
    }

    private void UpdateInputType(bool isGamepad)
    {
        playerData.isUsingGamepad = isGamepad;
        playerData.currentSensitivity = isGamepad ?
            playerData.gamepadBaseSensitivity :
            playerData.mouseBaseSensitivity;

        Debug.Log($"Input type changed. Using {(isGamepad ? "Gamepad" : "Mouse")} " +
                  $"with sensitivity: {playerData.currentSensitivity}");
    }

    private void UpdateRotation()
    {
        if (!playerData.canMove) return;

        // Rotaci�n horizontal del cuerpo
        transform.Rotate(Vector3.up * playerData.rotationInput.x *
                        playerData.currentSensitivity);

        // Rotaci�n vertical de la c�mara
        verticalRotation -= playerData.rotationInput.y * playerData.currentSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation,
                                     -playerData.maxLookAngle,
                                     playerData.maxLookAngle);

        if (playerCamera != null)
        {
            // Guardar la posici�n actual antes de modificar la rotaci�n
            Vector3 currentPos = playerData.cameraTransform.localPosition;

            // Aplicar la rotaci�n
            playerData.cameraTransform.localRotation =
                Quaternion.Euler(verticalRotation, 0f, 0f);

            // Restaurar la posici�n
            playerData.cameraTransform.localPosition = currentPos;

#if UNITY_EDITOR
            // Debug para monitorear la posici�n de la c�mara
            Debug.Log($"Camera Position after rotation: {currentPos}");
#endif
        }
    }

    private void UpdateHeadBob()
    {
        if (!playerData.isGrounded || !playerData.canMove) return;

        Vector3 targetPos = playerData.originalCameraPosition;

        // Si el jugador est� en movimiento
        if (playerData.moveDirection.magnitude > 0.1f)
        {
            // Incrementar el timer basado en la velocidad
            float bobSpeed = playerData.isSprinting ?
                playerData.bobFrequency * playerData.sprintBobMultiplier :
                playerData.bobFrequency;

            bobTimer += Time.deltaTime * bobSpeed;

            // Calcular el desplazamiento vertical
            float amplitude = playerData.isSprinting ?
                playerData.bobAmplitude * playerData.sprintBobMultiplier :
                playerData.bobAmplitude;

            float yOffset = Mathf.Sin(bobTimer) * amplitude;

            // Tambi�n podemos a�adir un ligero movimiento horizontal
            float xOffset = Mathf.Cos(bobTimer / 2f) * amplitude * 0.5f;

            // Aplicar los offsets
            targetPos += new Vector3(xOffset, yOffset, 0f);
        }
        else
        {
            // Resetear el timer cuando estamos quietos
            bobTimer = 0f;
        }

        // Suavizar la transici�n a la posici�n objetivo
        playerData.cameraTransform.localPosition = Vector3.Lerp(
            playerData.cameraTransform.localPosition,
            targetPos,
            Time.deltaTime * 12f
        );
    }

    public void RegenerateStamina()
    {
        if (playerData.timeUntilStaminaRegen > 0)
        {
            playerData.timeUntilStaminaRegen -= Time.deltaTime;
            return;
        }

        if (playerData.currentStamina < playerData.maxStamina)
        {
            playerData.currentStamina += playerData.staminaRegenRate * Time.deltaTime;
            playerData.currentStamina = Mathf.Min(playerData.currentStamina, playerData.maxStamina);
        }
    }
    #endregion


    #region Input System Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!playerData.canMove) return;
        Vector2 input = context.ReadValue<Vector2>();
        playerData.moveDirection = new Vector3(input.x, 0, input.y);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!playerData.canMove) return;
        playerData.rotationInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!playerData.canMove) return;

        if (context.started)
        {
            playerData.isSprinting = true;
            if (currentState == MoveState && playerData.canSprint && playerData.currentStamina > 0)
            {
                ChangeState(SprintState);
            }
        }
        else if (context.canceled)
        {
            playerData.isSprinting = false;
            if (currentState == SprintState)
            {
                ChangeState(MoveState);
            }
        }
    }
    #endregion

    #region M�todos de Estado
    public void ChangeState(PlayerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    #endregion

    #region Getters p�blicos
    public bool IsMoving() => playerData.moveDirection.magnitude > 0.1f;
    public CharacterController GetCharacterController() => characterController;
    public float GetStaminaPercentage()
    {
        return playerData.currentStamina / playerData.maxStamina;
    }
    #endregion

    #region Debugging
    private void OnGUI()
    {
        // Mostrar barra de stamina temporal para testing
        GUI.Box(new Rect(20, 20, 200, 20), "");
        GUI.Box(new Rect(20, 20, 200 * GetStaminaPercentage(), 20), "Stamina");
    }
    #endregion
}