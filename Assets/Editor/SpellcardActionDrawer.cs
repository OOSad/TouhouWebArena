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
        // Using BeginProperty / EndProperty enables prefab override logic
        EditorGUI.BeginProperty(position, label, property);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        float currentY = position.y;

        // Get SerializedProperties
        SerializedProperty startDelayProp = property.FindPropertyRelative("startDelay");
        SerializedProperty bulletPrefabsProp = property.FindPropertyRelative("bulletPrefabs");
        SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SerializedProperty radiusProp = property.FindPropertyRelative("radius");
        SerializedProperty spacingProp = property.FindPropertyRelative("spacing");
        SerializedProperty angleProp = property.FindPropertyRelative("angle");
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

        // --- Draw Always Visible Fields ---
        // Header: Spawning
        DrawProperty(ref currentY, position.width, bulletPrefabsProp);
        DrawProperty(ref currentY, position.width, positionOffsetProp);
        DrawProperty(ref currentY, position.width, countProp);

        // Header: Formation Shape
        DrawProperty(ref currentY, position.width, formationProp);
        FormationType currentFormation = (FormationType)formationProp.enumValueIndex;
        if (currentFormation == FormationType.Circle)
        {
            DrawProperty(ref currentY, position.width, radiusProp);
        }
        if (currentFormation == FormationType.Line)
        {
            DrawProperty(ref currentY, position.width, spacingProp);
        }
        // Angle applies to Point, Circle (offset), and Line (orientation)
        DrawProperty(ref currentY, position.width, angleProp);

        // Header: Bullet Behavior
        DrawProperty(ref currentY, position.width, behaviorProp);
        BehaviorType currentBehavior = (BehaviorType)behaviorProp.enumValueIndex;

        // Header: Behavior Speeds (Draw Speed always, others conditionally)
        DrawProperty(ref currentY, position.width, speedProp);
        if (currentFormation == FormationType.Line)
        {
            DrawProperty(ref currentY, position.width, speedIncrementProp);
        }
        if (currentBehavior == BehaviorType.Homing || currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming)
        {
            DrawProperty(ref currentY, position.width, homingSpeedProp); 
        }
        if (currentBehavior == BehaviorType.Spiral)
        {
            DrawProperty(ref currentY, position.width, tangentialSpeedProp);
        }

        // Header: Initial Speed Transition
        DrawProperty(ref currentY, position.width, useInitialSpeedProp);
        if (useInitialSpeedProp.boolValue)
        {
            DrawProperty(ref currentY, position.width, initialSpeedProp);
            DrawProperty(ref currentY, position.width, transitionDurationProp);
        }

        // Header: Behavior Timing & Parameters
        if (currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming || currentBehavior == BehaviorType.DelayedRandomTurn)
        {
             DrawProperty(ref currentY, position.width, homingDelayProp);
        }
        if (currentBehavior == BehaviorType.DoubleHoming)
        {
             DrawProperty(ref currentY, position.width, secondHomingDelayProp);
             DrawProperty(ref currentY, position.width, firstHomingDurationProp);
             DrawProperty(ref currentY, position.width, secondHomingLookAheadProp);
        }
         if (currentBehavior == BehaviorType.DelayedRandomTurn)
        {
            DrawProperty(ref currentY, position.width, spreadAngleProp);
            DrawProperty(ref currentY, position.width, minTurnSpeedProp);
            DrawProperty(ref currentY, position.width, maxTurnSpeedProp);
        }

        // Header: Spawning & Formation Modifiers
        DrawProperty(ref currentY, position.width, skipEveryNthProp);
        
        // Header: Timing & Lifetime
        DrawProperty(ref currentY, position.width, startDelayProp);
        DrawProperty(ref currentY, position.width, intraActionDelayProp);
        DrawProperty(ref currentY, position.width, lifetimeProp);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    // Helper to draw a property and advance the Y position
    private void DrawProperty(ref float currentY, float totalWidth, SerializedProperty prop)
    {
        float height = EditorGUI.GetPropertyHeight(prop, true);
        Rect rect = new Rect(EditorGUIUtility.labelWidth, currentY, totalWidth - EditorGUIUtility.labelWidth, height);
        Rect labelRect = new Rect(0, currentY, EditorGUIUtility.labelWidth, height); 
        
        // Draw Label manually to align left
        EditorGUI.LabelField(labelRect, prop.displayName);
        // Draw Property field without label
        EditorGUI.PropertyField(rect, prop, GUIContent.none, true);
        
        currentY += height + verticalSpacing;
    }

    // Calculate the total height needed for the property
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0f;

        // Get SerializedProperties (again, needed for height calculation)
        SerializedProperty bulletPrefabsProp = property.FindPropertyRelative("bulletPrefabs");
        SerializedProperty positionOffsetProp = property.FindPropertyRelative("positionOffset");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SerializedProperty radiusProp = property.FindPropertyRelative("radius");
        SerializedProperty spacingProp = property.FindPropertyRelative("spacing");
        SerializedProperty angleProp = property.FindPropertyRelative("angle");
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

        // Always visible fields
        totalHeight += EditorGUI.GetPropertyHeight(bulletPrefabsProp, true) + verticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(positionOffsetProp, true) + verticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(countProp, true) + verticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(formationProp, true) + verticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(angleProp, true) + verticalSpacing; // Always show angle
        totalHeight += EditorGUI.GetPropertyHeight(behaviorProp, true) + verticalSpacing;
        totalHeight += EditorGUI.GetPropertyHeight(speedProp, true) + verticalSpacing; // Always show base speed
        totalHeight += EditorGUI.GetPropertyHeight(useInitialSpeedProp, true) + verticalSpacing; // Always show checkbox
        totalHeight += EditorGUI.GetPropertyHeight(skipEveryNthProp, true) + verticalSpacing; // Always show skip
        totalHeight += EditorGUI.GetPropertyHeight(startDelayProp, true) + verticalSpacing; // Always show start delay
        totalHeight += EditorGUI.GetPropertyHeight(intraActionDelayProp, true) + verticalSpacing; // Always show intra-action delay
        totalHeight += EditorGUI.GetPropertyHeight(lifetimeProp, true) + verticalSpacing; // Always show lifetime

        // Conditional Fields
        FormationType currentFormation = (FormationType)formationProp.enumValueIndex;
        BehaviorType currentBehavior = (BehaviorType)behaviorProp.enumValueIndex;

        if (currentFormation == FormationType.Circle)
        {
             totalHeight += EditorGUI.GetPropertyHeight(radiusProp, true) + verticalSpacing;
        }
        if (currentFormation == FormationType.Line)
        {
             totalHeight += EditorGUI.GetPropertyHeight(spacingProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(speedIncrementProp, true) + verticalSpacing;
        }
       
        if (currentBehavior == BehaviorType.Homing || currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming)
        {
             totalHeight += EditorGUI.GetPropertyHeight(homingSpeedProp, true) + verticalSpacing;
        }
        if (currentBehavior == BehaviorType.Spiral)
        {
             totalHeight += EditorGUI.GetPropertyHeight(tangentialSpeedProp, true) + verticalSpacing;
        }

        if (useInitialSpeedProp.boolValue)
        {
             totalHeight += EditorGUI.GetPropertyHeight(initialSpeedProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(transitionDurationProp, true) + verticalSpacing;
        }

        if (currentBehavior == BehaviorType.DelayedHoming || currentBehavior == BehaviorType.DoubleHoming || currentBehavior == BehaviorType.DelayedRandomTurn)
        {
              totalHeight += EditorGUI.GetPropertyHeight(homingDelayProp, true) + verticalSpacing;
        }
        if (currentBehavior == BehaviorType.DoubleHoming)
        {
             totalHeight += EditorGUI.GetPropertyHeight(secondHomingDelayProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(firstHomingDurationProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(secondHomingLookAheadProp, true) + verticalSpacing;
        }
        if (currentBehavior == BehaviorType.DelayedRandomTurn)
        {
             totalHeight += EditorGUI.GetPropertyHeight(spreadAngleProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(minTurnSpeedProp, true) + verticalSpacing;
             totalHeight += EditorGUI.GetPropertyHeight(maxTurnSpeedProp, true) + verticalSpacing;
        }

        return totalHeight;
    }
} 