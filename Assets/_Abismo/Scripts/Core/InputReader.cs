using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Abismo
{
    /// <summary>
    /// Lee teclado y mando una vez por frame y guarda el resultado para el resto de scripts.
    /// Funciona con el Input System nuevo y con el Input Manager clásico: Unity compila
    /// la parte correcta según Project Settings > Player > Active Input Handling.
    ///
    /// Controles:
    ///   Mover: A/D o flechas · Saltar: Espacio o K · Atacar: J · Esquivar: L o Shift
    ///   Parar (parry): I · Conjuro: U · Curarse: F · Interactuar: E o W/↑ · Pausa: Esc
    ///   Mando: stick/cruceta · A saltar · X atacar · B esquivar · RB parar · Y conjuro · LB curarse · ↑ interactuar · Start pausa
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool ParryPressed { get; private set; }
        public bool SpellPressed { get; private set; }
        public bool HealPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool AnyPressed { get; private set; }

        const float StickThreshold = 0.5f;
        bool stickUpLastFrame;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            Vector2 move = Vector2.zero;
            bool jump = false, jumpHeld = false, attack = false, dash = false, parry = false;
            bool spell = false, heal = false, interact = false, pause = false, any = false;
            float stickY = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;

                jump |= kb.spaceKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
                jumpHeld |= kb.spaceKey.isPressed || kb.kKey.isPressed;
                attack |= kb.jKey.wasPressedThisFrame;
                dash |= kb.lKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame;
                parry |= kb.iKey.wasPressedThisFrame;
                spell |= kb.uKey.wasPressedThisFrame;
                heal |= kb.fKey.wasPressedThisFrame;
                interact |= kb.eKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                pause |= kb.escapeKey.wasPressedThisFrame;
                any |= kb.anyKey.wasPressedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                Vector2 dpad = pad.dpad.ReadValue();
                if (Mathf.Abs(stick.x) > StickThreshold) move.x += Mathf.Sign(stick.x);
                if (Mathf.Abs(stick.y) > StickThreshold) move.y += Mathf.Sign(stick.y);
                move.x += Mathf.Round(dpad.x);
                move.y += Mathf.Round(dpad.y);
                stickY = stick.y;

                jump |= pad.buttonSouth.wasPressedThisFrame;
                jumpHeld |= pad.buttonSouth.isPressed;
                attack |= pad.buttonWest.wasPressedThisFrame;
                dash |= pad.buttonEast.wasPressedThisFrame;
                parry |= pad.rightShoulder.wasPressedThisFrame;
                spell |= pad.buttonNorth.wasPressedThisFrame;
                heal |= pad.leftShoulder.wasPressedThisFrame;
                interact |= pad.dpad.up.wasPressedThisFrame;
                pause |= pad.startButton.wasPressedThisFrame;
                any |= pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                    || pad.startButton.wasPressedThisFrame;
            }
#else
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
            stickY = move.y;

            jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.JoystickButton0);
            jumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.JoystickButton0);
            attack = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.JoystickButton2);
            dash = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.JoystickButton1);
            parry = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.JoystickButton5);
            spell = Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.JoystickButton3);
            heal = Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton4);
            interact = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7);
            any = Input.anyKeyDown;
#endif

            // Empujar el stick hacia arriba también sirve para interactuar.
            bool stickUp = stickY > 0.7f;
            if (stickUp && !stickUpLastFrame) interact = true;
            stickUpLastFrame = stickUp;

            Move = new Vector2(Mathf.Clamp(move.x, -1f, 1f), Mathf.Clamp(move.y, -1f, 1f));
            JumpPressed = jump;
            JumpHeld = jumpHeld;
            AttackPressed = attack;
            DashPressed = dash;
            ParryPressed = parry;
            SpellPressed = spell;
            HealPressed = heal;
            InteractPressed = interact;
            PausePressed = pause;
            AnyPressed = any || jump || attack || interact;
        }
    }
}
