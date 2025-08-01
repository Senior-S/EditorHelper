using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Commands
{
    public class FlyController : MonoBehaviour
    {
        private Player _player;
        private float _flySpeed;
        private readonly float _scrollSensitivity;
        private readonly float _minSpeed;
        private readonly float _maxSpeed;

        public FlyController()
        {
            _player = GetComponent<Player>();
            
            _flySpeed = 10f;
            _scrollSensitivity = 5f;
            _minSpeed = 1f;
            _maxSpeed = 50f;
        }

        void Start()
        {
            _player = GetComponent<Player>();
            
            if (_player != null)
            {
                // Disable default gravity and movement
                _player.movement.sendPluginGravityMultiplier(0f);
                _player.movement.sendPluginSpeedMultiplier(0f);
            }
        }

        void FixedUpdate()
        {
            HandleFlyMovement();
        }

        private void HandleFlyMovement()
        {
            if (_player == null || _player.life.isDead) return;

            // Adjust fly speed using scroll wheel
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                _flySpeed += scroll * _scrollSensitivity;
                _flySpeed = Mathf.Clamp(_flySpeed, _minSpeed, _maxSpeed);
                _player.ServerShowHint("Fly Speed: " + _flySpeed.ToString("F1"), 2);
            }

            Vector3 move = Vector3.zero;

            // Vertical input
            if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) move += Vector3.down;

            // Directional input (relative to aim)
            if (Input.GetKey(KeyCode.W)) move += _player.look.aim.forward;
            if (Input.GetKey(KeyCode.S)) move -= _player.look.aim.forward;
            if (Input.GetKey(KeyCode.A)) move -= _player.look.aim.right;
            if (Input.GetKey(KeyCode.D)) move += _player.look.aim.right;

            move.Normalize();

            // Apply movement manually
            if (move != Vector3.zero)
            {
                _player.transform.position += move * (_flySpeed * Time.fixedDeltaTime);
            }

            // Reset velocity to avoid sliding or unwanted momentum
            Rigidbody rb = _player.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        void OnDestroy()
        {
            if (_player != null)
            {
                // Restore default gravity and movement when script is removed
                _player.movement.sendPluginGravityMultiplier(1f);
                _player.movement.sendPluginSpeedMultiplier(1f);
            }
        }
    }
}
