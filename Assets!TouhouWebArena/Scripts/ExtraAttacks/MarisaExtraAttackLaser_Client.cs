using UnityEngine;

public class MarisaExtraAttackLaser_Client : MonoBehaviour
{
    public float damageAmount = 1f;
    public int _attackerClientId;
    public LayerMask playerHitboxLayer;
    public LayerMask spiritHealthLayer;
    public LayerMask opponentSpiritHealthLayer;
    public LayerMask opponentPlayerHitboxLayer;
    public LayerMask friendlySpiritHealthLayer;
    public LayerMask friendlyPlayerHitboxLayer;
    public LayerMask opponentSpiritPlayerHitboxLayer;
    public LayerMask friendlySpiritPlayerHitboxLayer;
    public LayerMask opponentSpiritPlayerHitboxLayer;
    public LayerMask friendlySpiritPlayerHitboxLayer;

    private LayerMask[] layersToCheck;
    private LayerMask[] layersToDamage;
    private LayerMask[] layersToDamageOpponent;
    private LayerMask[] layersToDamageFriendly;
    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;
    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[] layersToDamageFriendlyPlayer;

    private LayerMask[] layersToDamageOpponentPlayer;
    private LayerMask[]