using UnityEngine;

[CreateAssetMenu(menuName = "Mars/Scripted transmission")]
public sealed class ScriptedTransmission : ScriptableObject
{
    public TransmissionDefinition message = new TransmissionDefinition();
}
