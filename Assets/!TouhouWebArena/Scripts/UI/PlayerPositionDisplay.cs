using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerPositionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _player1PositionText;
    [SerializeField] private TextMeshProUGUI _player2PositionText;
    [SerializeField] private PlayerPositionTracker _positionTracker;

    void Update()
    {
        if (_positionTracker == null)
        {
            Debug.LogError("PlayerPositionTracker reference not set in PlayerPositionDisplay!");
            return;
        }

        if (_player1PositionText != null)
        {
            // Read the NetworkVariable value
            Vector3 p1Pos = _positionTracker.Player1Position.Value;
            _player1PositionText.text = $"P1 Pos: ({p1Pos.x:F1}, {p1Pos.y:F1})"; // Format to 1 decimal place
        }

        if (_player2PositionText != null)
        {
             // Read the NetworkVariable value
            Vector3 p2Pos = _positionTracker.Player2Position.Value;
            _player2PositionText.text = $"P2 Pos: ({p2Pos.x:F1}, {p2Pos.y:F1})"; // Format to 1 decimal place
        }
    }
} 