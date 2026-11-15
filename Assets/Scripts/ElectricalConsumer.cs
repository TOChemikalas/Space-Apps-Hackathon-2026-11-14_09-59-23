using System;
using UnityEngine;

public sealed class ElectricalConsumer : MonoBehaviour
{
    [Min(0)] public double demandKW = 5;
    public bool replacedBySurvivalEconomy;
}
