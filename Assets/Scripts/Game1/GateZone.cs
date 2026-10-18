using UnityEngine;

// Tall trigger in front of a hub gate wall, so the "Press ENTER" prompt shows even when the
// player arrives above the original (low) entrance trigger.
public class GateZone : MonoBehaviour
{
    public InstructionManager instructionManager;
    public string gate;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) instructionManager.EnterGate(gate);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) instructionManager.ExitGate(gate);
    }
}
