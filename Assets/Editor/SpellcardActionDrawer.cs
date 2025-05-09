using UnityEngine;
using UnityEditor;
using TouhouWebArena.Spellcards; // Access SpellcardAction, FormationType, BehaviorType

/// <summary>
/// Custom PropertyDrawer for the SpellcardAction class.
/// Dynamically shows/hides fields in the Inspector based on the selected
/// FormationType and BehaviorType to improve usability.
/// </summary>
[CustomPropertyDrawer(typeof(SpellcardAction))]
public class SpellcardActionDrawer : PropertyDrawer
{
    // Store heights for spacing
    private float propertyHeight = EditorGUIUtility.singleLineHeight;
    private float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        float currentY = position.y;

        // Get SerializedProperties (ensure all are fetched here including new ones)
        SerializedProperty startDelayProp = property.FindPropertyRelative("startDelay");
        SerializedProperty bulletPrefabsProp = property.FindPropertyRelative("bulletPrefabs");
        SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SerializedProperty radiusProp = property.FindPropertyRelative("radius");
        SerializedProperty spacingProp = property.FindPropertyRelative("spacing");
        SerializedProperty angleProp = property.FindPropertyRelative("angle");
        SerializedProperty applyRandomSpawnOffsetProp = property.FindPropertyRelative("applyRandomSpawnOffset");
        SerializedProperty randomOffsetMinProp = property.FindPropertyRelative("randomOffsetMin");
        SerializedProperty randomOffsetMaxProp = property.FindPropertyRelative("randomOffsetMax");
        SerializedProperty behaviorProp = property.FindPropertyRelative("behavior");
        SerializedProperty speedProp = property.FindPropertyRelative("speed");
        SerializedProperty speedIncrementProp = property.FindPropertyRelative("speedIncrementPerBullet");
        SerializedProperty homingSpeedProp = property.FindPropertyRelative("homingSpeed");
        SerializedProperty tangentialSpeedProp = property.FindPropertyRelative("tangentialSpeed");
        SerializedProperty useInitialSpeedProp = property.FindPropertyRelative("useInitialSpeed");
        SerializedProperty initialSpeedProp = property.FindPropertyRelative("initialSpeed");
        SerializedProperty transitionDurationProp = property.FindPropertyRelative("speedTransitionDuration");
        SerializedProperty homingDelayProp = property.FindPropertyRelative("homingDelay");
        SerializedProperty secondHomingDelayProp = property.FindPropertyRelative("secondHomingDelay");
        SerializedProperty firstHomingDurationProp = property.FindPropertyRelative("firstHomingDuration");
        SerializedProperty secondHomingLookAheadProp = property.FindPropertyRelative("secondHomingLookAheadDistance");
        SerializedProperty spreadAngleProp = property.FindPropertyRelative("spreadAngle");
        SerializedProperty minTurnSpeedProp = property.FindPropertyRelative("minTurnSpeed");
        SerializedProperty maxTurnSpeedProp = property.FindPropertyRelative("maxTurnSpeed");
        SerializedProperty skipEveryNthProp = property.FindPropertyRelative("skipEveryNth");
        SerializedProperty intraActionDelayProp = property.FindPropertyRelative("intraActionDelay");
        SerializedProperty lifetimeProp = property.FindPropertyRelative("lifetime");

        // Draw fields, passing position.x and position.width
        DrawProperty(ref currentY, position.x, position.width, bulletPrefabsProp);
        DrawProperty(ref currentY, position.x, position.width, positionOffsetProp);
        DrawProperty(ref currentY, position.x, position.width, countProp);
        DrawProperty(ref currentY, position.x, position.width, formationProp);
        FormationType currentFormation = (FormationType)formationProp.enumValueIndex;
        if (currentFormation == FormationType.Circle) DrawProperty(ref currentY, position.x, position.width, radiusProp);
        if (currentFormation == FormationType.Line) DrawProperty(ref currentY, position.x, position.width, spacingProp);
        DrawProperty(ref currentY, position.x, position.width, angleProp);
        
        DrawHeader(ref currentY, position.x, position.width, "Random Spawn Offset");
        DrawProperty(ref currentY, position.x, position.width, applyRandomSpawnOffsetProp);
        if (applyRandomSpawnOffsetProp.boolValue)
        {
            EditorGUI.indentLevel++;
            DrawProperty(ref currentY, position.x, position.width, randomOffsetMinProp);
            DrawProperty(ref currentY, position.x, position.width, randomOffsetMaxProp);
            EditorGUI.indentLevel--;
        }

        DrawProperty(ref currentY, position.x, position.width, behaviorProp);
        BehaviorType currentBehavior = (BehaviorType)behaviorProp.enumValueIndex;
        DrawProperty(ref currentY, position.x, position.width, speedProp);
        if (currentFormation == FormationType.Line) DrawProperty(ref currentY, position.x, position.width, speedIncrementProp);
        if (currentBehavior == BehaviorType.Homing || currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming) DrawProperty(ref currentY, position.x, position.width, homingSpeedProp);
        if (currentBehavior == BehaviorType.Spiral) DrawProperty(ref currentY, position.x, position.width, tangentialSpeedProp);
        DrawProperty(ref currentY, position.x, position.width, useInitialSpeedProp);
        if (useInitialSpeedProp.boolValue)
        {
            DrawProperty(ref currentY, position.x, position.width, initialSpeedProp);
            DrawProperty(ref currentY, position.x, position.width, transitionDurationProp);
        }
        if (currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming || currentBehavior == BehaviorType.DelayedRandomTurn) DrawProperty(ref currentY, position.x, position.width, homingDelayProp);
        if (currentBehavior == BehaviorType.DoubleHoming)
        {
            DrawProperty(ref currentY, position.x, position.width, secondHomingDelayProp);
            DrawProperty(ref currentY, position.x, position.width, firstHomingDurationProp);
            DrawProperty(ref currentY, position.x, position.width, secondHomingLookAheadProp);
        }
        if (currentBehavior == BehaviorType.DelayedRandomTurn)
        {
            DrawProperty(ref currentY, position.x, position.width, spreadAngleProp);
            DrawProperty(ref currentY, position.x, position.width, minTurnSpeedProp);
            DrawProperty(ref currentY, position.x, position.width, maxTurnSpeedProp);
        }
        DrawProperty(ref currentY, position.x, position.width, skipEveryNthProp);
        DrawProperty(ref currentY, position.x, position.width, startDelayProp);
        DrawProperty(ref currentY, position.x, position.width, intraActionDelayProp);
        DrawProperty(ref currentY, position.x, position.width, lifetimeProp);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    private void DrawProperty(ref float currentY, float startX, float totalWidth, SerializedProperty prop)
    {
        float height = EditorGUI.GetPropertyHeight(prop, true);
        Rect propertyRect = new Rect(startX, currentY, totalWidth, height);
        EditorGUI.PropertyField(propertyRect, prop, true);
        currentY += height + verticalSpacing;
    }

    private void DrawHeader(ref float currentY, float startX, float totalWidth, string headerText)
    {
        Rect headerRect = new Rect(startX, currentY, totalWidth, propertyHeight);
        EditorGUI.LabelField(headerRect, headerText, EditorStyles.boldLabel);
        currentY += propertyHeight + verticalSpacing;
    }

    // Calculate the total height needed for the property
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0f;
        // It's good practice to define these constants locally or access them consistently
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float currentVerticalSpacing = EditorGUIUtility.standardVerticalSpacing;

        // Get SerializedProperties (ensure all are fetched here including new ones)
        SerializedProperty bulletPrefabsProp = property.FindPropertyRelative("bulletPrefabs");
        SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SerializedProperty radiusProp = property.FindPropertyRelative("radius");
        SerializedProperty spacingProp = property.FindPropertyRelative("spacing");
        SerializedProperty angleProp = property.FindPropertyRelative("angle");
        SerializedProperty applyRandomSpawnOffsetProp = property.FindPropertyRelative("applyRandomSpawnOffset");
        SerializedProperty randomOffsetMinProp = property.FindPropertyRelative("randomOffsetMin");
        SerializedProperty randomOffsetMaxProp = property.FindPropertyRelative("randomOffsetMax");
        SerializedProperty behaviorProp = property.FindPropertyRelative("behavior");
        SerializedProperty speedProp = property.FindPropertyRelative("speed");
        SerializedProperty speedIncrementProp = property.FindPropertyRelative("speedIncrementPerBullet");
        SerializedProperty homingSpeedProp = property.FindPropertyRelative("homingSpeed");
        SerializedProperty tangentialSpeedProp = property.FindPropertyRelative("tangentialSpeed");
        SerializedProperty useInitialSpeedProp = property.FindPropertyRelative("useInitialSpeed");
        SerializedProperty initialSpeedProp = property.FindPropertyRelative("initialSpeed");
        SerializedProperty transitionDurationProp = property.FindPropertyRelative("speedTransitionDuration");
        SerializedProperty homingDelayProp = property.FindPropertyRelative("homingDelay");
        SerializedProperty secondHomingDelayProp = property.FindPropertyRelative("secondHomingDelay");
        SerializedProperty firstHomingDurationProp = property.FindPropertyRelative("firstHomingDuration");
        SerializedProperty secondHomingLookAheadProp = property.FindPropertyRelative("secondHomingLookAheadDistance");
        SerializedProperty spreadAngleProp = property.FindPropertyRelative("spreadAngle");
        SerializedProperty minTurnSpeedProp = property.FindPropertyRelative("minTurnSpeed");
        SerializedProperty maxTurnSpeedProp = property.FindPropertyRelative("maxTurnSpeed");
        SerializedProperty skipEveryNthProp = property.FindPropertyRelative("skipEveryNth");
        SerializedProperty startDelayProp = property.FindPropertyRelative("startDelay");
        SerializedProperty intraActionDelayProp = property.FindPropertyRelative("intraActionDelay");
        SerializedProperty lifetimeProp = property.FindPropertyRelative("lifetime");

        // Calculate height in the same order as OnGUI drawing if possible for clarity
        totalHeight += EditorGUI.GetPropertyHeight(bulletPrefabsProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(positionOffsetProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(countProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(formationProp, true) + currentVerticalSpacing;
        
        FormationType currentFormation = (FormationType)formationProp.enumValueIndex;
        if (currentFormation == FormationType.Circle) totalHeight += EditorGUI.GetPropertyHeight(radiusProp, true) + currentVerticalSpacing;
        if (currentFormation == FormationType.Line) totalHeight += EditorGUI.GetPropertyHeight(spacingProp, true) + currentVerticalSpacing;
        
        totalHeight += EditorGUI.GetPropertyHeight(angleProp, true) + currentVerticalSpacing;
        
        // Height for "Random Spawn Offset" Header itself
        totalHeight += singleLineHeight + currentVerticalSpacing; 
        totalHeight += EditorGUI.GetPropertyHeight(applyRandomSpawnOffsetProp, true) + currentVerticalSpacing;
        if (applyRandomSpawnOffsetProp.boolValue)
        {
            totalHeight += EditorGUI.GetPropertyHeight(randomOffsetMinProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(randomOffsetMaxProp, true) + currentVerticalSpacing;
        }

        totalHeight += EditorGUI.GetPropertyHeight(behaviorProp, true) + currentVerticalSpacing;
        BehaviorType currentBehavior = (BehaviorType)behaviorProp.enumValueIndex;
        totalHeight += EditorGUI.GetPropertyHeight(speedProp, true) + currentVerticalSpacing;
        if (currentFormation == FormationType.Line) totalHeight += EditorGUI.GetPropertyHeight(speedIncrementProp, true) + currentVerticalSpacing;
        if (currentBehavior == BehaviorType.Homing || currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming) totalHeight += EditorGUI.GetPropertyHeight(homingSpeedProp, true) + currentVerticalSpacing;
        if (currentBehavior == BehaviorType.Spiral) totalHeight += EditorGUI.GetPropertyHeight(tangentialSpeedProp, true) + currentVerticalSpacing;
        
        totalHeight += EditorGUI.GetPropertyHeight(useInitialSpeedProp, true) + currentVerticalSpacing;
        if (useInitialSpeedProp.boolValue)
        {
            totalHeight += EditorGUI.GetPropertyHeight(initialSpeedProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(transitionDurationProp, true) + currentVerticalSpacing;
        }

        if (currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming || currentBehavior == BehaviorType.DelayedRandomTurn) totalHeight += EditorGUI.GetPropertyHeight(homingDelayProp, true) + currentVerticalSpacing;
        if (currentBehavior == BehaviorType.DoubleHoming)
        {
            totalHeight += EditorGUI.GetPropertyHeight(secondHomingDelayProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(firstHomingDurationProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(secondHomingLookAheadProp, true) + currentVerticalSpacing;
        }
        if (currentBehavior == BehaviorType.DelayedRandomTurn)
        {
            totalHeight += EditorGUI.GetPropertyHeight(spreadAngleProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(minTurnSpeedProp, true) + currentVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(maxTurnSpeedProp, true) + currentVerticalSpacing;
        }

        totalHeight += EditorGUI.GetPropertyHeight(skipEveryNthProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(startDelayProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(intraActionDelayProp, true) + currentVerticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(lifetimeProp, true) + currentVerticalSpacing;

        return totalHeight;
    }
} 